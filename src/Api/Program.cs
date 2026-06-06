using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MySociety.Api.Hosting;
using MySociety.Api.Middleware;
using MySociety.Application;
using MySociety.Infrastructure;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Security;
using Serilog;
using Serilog.Events;

EmergencyStartupLog.Mark("Process started — before Serilog bootstrap");

// Bootstrap logger — writes to stdout before full Serilog config (visible in Azure Log stream / stdout).
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("MySociety API starting");
    EmergencyStartupLog.Mark("Serilog bootstrap logger created");

    var builder = WebApplication.CreateBuilder(args);
    var isProduction = !builder.Environment.IsDevelopment();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Log.Information("Database connection: {Connection}", HostingConfiguration.SummarizeForLog(connectionString));

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is required. " +
            "Set ConnectionStrings__DefaultConnection to your Neon or Supabase PostgreSQL connection string.");
    }

    var logDirectory = HostingConfiguration.ResolveLogDirectory(builder.Environment.ContentRootPath);
    Log.Information("Log directory: {LogDirectory}", logDirectory);

    builder.Host.UseSerilog((context, _, configuration) =>
    {
        configuration
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "MySociety.Api")
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
            .WriteTo.Console();

        try
        {
            configuration.WriteTo.File(
                Path.Combine(logDirectory, "mysociety-api-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "File logging disabled; using console only");
        }

        configuration.ReadFrom.Configuration(context.Configuration);
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "MySociety API", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header. Example: Bearer {token}"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
    builder.Services.Configure<MySociety.Application.Common.Settings.OtpSettings>(
        builder.Configuration.GetSection(MySociety.Application.Common.Settings.OtpSettings.SectionName));

    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
    if (jwtSettings is null || string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
    {
        throw new InvalidOperationException(
            "Jwt:Key must be at least 32 characters. Set Jwt__Key environment variable or appsettings.");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("MobileDev", policy =>
        {
            policy
                .SetIsOriginAllowed(origin =>
                {
                    if (string.IsNullOrWhiteSpace(origin) || !Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    {
                        return false;
                    }

                    // Local web clients (Expo web, Swagger, etc.) — allowed in every environment.
                    if (uri.Host is "localhost" or "127.0.0.1")
                    {
                        return true;
                    }

                    if (!builder.Environment.IsDevelopment())
                    {
                        return false;
                    }

                    return uri.Host is "10.0.2.2"
                        || uri.Host.StartsWith("192.168.", StringComparison.Ordinal)
                        || uri.Host.StartsWith("10.", StringComparison.Ordinal);
                })
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();
    Log.Information(
        "Bootstrapped. Environment={Environment}; ContentRoot={ContentRoot}; Connection={Connection}",
        app.Environment.EnvironmentName,
        app.Environment.ContentRootPath,
        HostingConfiguration.SummarizeConnectionString(connectionString));

    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            Log.Information("Database migrations applied");

            if (app.Environment.IsDevelopment())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedAsync();
                Log.Information("Development seed data applied");
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex,
                "Database setup failed. Check ConnectionStrings__DefaultConnection points to a reachable PostgreSQL instance.");

            if (isProduction)
            {
                throw;
            }
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} => {StatusCode} in {Elapsed:0.0000} ms (User={UserName}, MemberId={MemberId}, RemoteIp={RemoteIp})";

        options.GetLevel = (httpContext, _, ex) =>
        {
            if (ex is null)
            {
                return LogEventLevel.Information;
            }

            if (ex is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                return LogEventLevel.Debug;
            }

            return LogEventLevel.Error;
        };

        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RemoteIp", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            diagnosticContext.Set("UserName", httpContext.User?.Identity?.Name ?? "anonymous");
            diagnosticContext.Set("MemberId", httpContext.Request.Headers["X-Member-Id"].ToString());
            diagnosticContext.Set("Host", httpContext.Request.Host.Value);
            diagnosticContext.Set("Scheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        };
    });

    app.UseRouting();
    app.UseCors("MobileDev");
    app.UseMiddleware<GlobalExceptionHandler>();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/health", () => Results.Ok(new { status = "alive" }))
        .AllowAnonymous()
        .ExcludeFromDescription();

    app.MapGet("/", () => Results.Ok(new
    {
        service = "MySociety API",
        health = "/health",
        healthWithDb = "/api/health"
    }))
        .AllowAnonymous()
        .ExcludeFromDescription();

    app.MapControllers();

    Log.Information("Listening for requests");
    app.Run();
}
catch (Exception ex)
{
    EmergencyStartupLog.Mark($"FATAL: {ex.GetType().Name}: {ex.Message}");
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
