using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Financial;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;
using MySociety.Application.Common.Settings;
using MySociety.Infrastructure.Security;
using MySociety.Infrastructure.Services;

namespace MySociety.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=mysociety.db";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<OtpSettings>(configuration.GetSection(OtpSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        var emailSection = configuration.GetSection(EmailSettings.SectionName);
        var smtpHost = emailSection.GetSection("Smtp").GetValue<string>("Host");
        if (!string.IsNullOrWhiteSpace(smtpHost))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }

        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IMemberInviteRepository, MemberInviteRepository>();
        services.AddScoped<IPhoneOtpRepository, PhoneOtpRepository>();
        services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IContributionRepository, ContributionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IGroupExpenseRepository, GroupExpenseRepository>();
        services.AddScoped<ICommitteeMemberRepository, CommitteeMemberRepository>();
        services.AddScoped<IMeetingRepository, MeetingRepository>();
        services.AddScoped<IOpenMatterRepository, OpenMatterRepository>();
        services.AddScoped<IAgendaItemRepository, AgendaItemRepository>();
        services.AddScoped<IMeetingAttendeeRepository, MeetingAttendeeRepository>();
        services.AddScoped<IMinuteRepository, MinuteRepository>();
        services.AddScoped<IResolutionRepository, ResolutionRepository>();
        services.AddScoped<IGroupDecisionRepository, GroupDecisionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ILedgerService, LedgerService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IInviteCodeService, InviteCodeService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ISmsSender, LoggingSmsSender>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
