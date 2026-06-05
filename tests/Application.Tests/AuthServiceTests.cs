using FluentValidation;
using MySociety.Application.Auth;
using MySociety.Application.Auth.Dtos;
using MySociety.Application.Auth.Validators;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Settings;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;
using MySociety.Infrastructure.Security;
using MySociety.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace MySociety.Application.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task Login_returns_token_for_seeded_user()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var passwordHasher = new PasswordHasher();
        var ledger = new LedgerService(context);
        var seeder = new DatabaseSeeder(context, passwordHasher, ledger, Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseSeeder>.Instance);
        await seeder.SeedAsync();

        var sut = CreateAuthService(context);
        var result = await sut.LoginAsync(
            new LoginRequest("demo", "Password123!"),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.NotEmpty(result.Memberships);
        Assert.Equal("demo", result.User.Username);
    }

    [Fact]
    public async Task Register_creates_user_and_returns_token()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var sut = CreateAuthService(context);

        var result = await sut.RegisterAsync(
            new RegisterRequest("newuser", "new@example.com", "New User", "Password123!"),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("newuser", result.User.Username);
        Assert.Empty(result.Memberships);
    }

    [Fact]
    public async Task Login_rejects_invalid_password()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var passwordHasher = new PasswordHasher();
        var ledger = new LedgerService(context);
        var seeder = new DatabaseSeeder(context, passwordHasher, ledger, Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseSeeder>.Instance);
        await seeder.SeedAsync();

        var sut = CreateAuthService(context);

        await Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await sut.LoginAsync(new LoginRequest("demo", "Wrong12!"), CancellationToken.None));
    }

    private static AuthService CreateAuthService(AppDbContext context)
    {
        var jwtSettings = Options.Create(new JwtSettings
        {
            Key = "MySociety_Test_Signing_Key_At_Least_32_Chars!",
            Issuer = "MySociety",
            Audience = "MySociety",
            ExpiryMinutes = 60
        });

        var otpSettings = Options.Create(new OtpSettings
        {
            Required = false,
            CodeLength = 6,
            ExpiryMinutes = 10,
            ResendCooldownSeconds = 60,
            ExposeCodeInApi = false
        });

        var emailSettings = Options.Create(new EmailSettings());

        return new AuthService(
            new UserRepository(context),
            new MemberInviteRepository(context),
            new PasswordResetRepository(context),
            new PhoneOtpRepository(context),
            new PasswordHasher(),
            new InviteCodeService(),
            new OtpService(),
            new FakeSmsSender(),
            new FakeEmailSender(),
            new JwtTokenService(jwtSettings),
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new SendActivationOtpRequestValidator(),
            new ActivateAccountRequestValidator(),
            new SendPasswordResetCodeRequestValidator(),
            new ResetPasswordRequestValidator(),
            otpSettings,
            emailSettings,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthService>.Instance);
    }
}
