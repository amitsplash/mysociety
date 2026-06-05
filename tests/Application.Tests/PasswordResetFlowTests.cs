using MySociety.Application.Auth;
using MySociety.Application.Auth.Dtos;
using MySociety.Application.Auth.Validators;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Settings;
using MySociety.Application.Members.Dtos;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;
using MySociety.Infrastructure.Security;
using MySociety.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.Tests;

public class PasswordResetFlowTests
{
    [Fact]
    public async Task Email_reset_code_allows_password_change_and_login()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (authService, emailSender) = CreateAuthService(context);

        await TestData.AddRegisteredUserAsync(
            context,
            "reset_user",
            "reset_user@example.com",
            "Reset User",
            passwordHash: new PasswordHasher().Hash("InitialPass1!"));

        var sent = await authService.SendPasswordResetCodeAsync(
            new SendPasswordResetCodeRequest("reset_user@example.com"),
            CancellationToken.None);

        var code = sent.ResetCode ?? emailSender.ExtractResetCode()
            ?? throw new InvalidOperationException("Reset code was not captured.");

        var login = await authService.ResetPasswordAsync(
            new ResetPasswordRequest("reset_user@example.com", code, "NewSecure99!"),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(login.Token));

        var canLogin = await authService.LoginAsync(
            new LoginRequest("reset_user", "NewSecure99!"),
            CancellationToken.None);
        Assert.False(string.IsNullOrWhiteSpace(canLogin.Token));
    }

    [Fact]
    public async Task Reset_password_rejects_wrong_code()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (authService, _) = CreateAuthService(context);

        await TestData.AddRegisteredUserAsync(
            context,
            "reset_user2",
            "reset_user2@example.com",
            "Reset User 2",
            passwordHash: new PasswordHasher().Hash("InitialPass1!"));

        await authService.SendPasswordResetCodeAsync(
            new SendPasswordResetCodeRequest("reset_user2@example.com"),
            CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await authService.ResetPasswordAsync(
                new ResetPasswordRequest("reset_user2@example.com", "000000", "NewSecure99!"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Admin_issued_reset_code_still_works_with_email()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminMemberId, memberId, _) = await SeedActivatedMemberAsync(context);
        var (authService, _) = CreateAuthService(context);

        var memberService = TestData.CreateMemberService(context);
        var issued = await memberService.IssuePasswordResetAsync(memberId, adminMemberId, CancellationToken.None);

        var user = await context.Users.FindAsync(
            (await context.Members.FindAsync(memberId))!.UserId);
        Assert.NotNull(user);

        var login = await authService.ResetPasswordAsync(
            new ResetPasswordRequest(user!.Email, issued.ResetCode, "AdminReset99!"),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(login.Token));
    }

    [Fact]
    public async Task Issue_reset_fails_for_unactivated_member()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "owner_reset", "owner_reset@test.com", "Owner");

        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(name: "Reset Test"),
            CancellationToken.None);

        var pending = await memberService.CreateAsync(new CreateMemberRequest(
            created.Group.Id,
            "Pending User",
            "9333000002",
            MemberRole.Member,
            0m,
            null), created.CreatorMember.Id, CancellationToken.None);

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await memberService.IssuePasswordResetAsync(
                pending.Member.Id,
                created.CreatorMember.Id,
                CancellationToken.None));
    }

    private static async Task<(Guid GroupId, Guid AdminMemberId, Guid MemberId, string Phone)> SeedActivatedMemberAsync(
        AppDbContext context)
    {
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var (authService, _) = CreateAuthService(context);

        var owner = await TestData.AddRegisteredUserAsync(context, "owner_pr", "owner_pr@test.com", "Owner");
        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(name: "Password Reset Group"),
            CancellationToken.None);

        var added = await memberService.CreateAsync(new CreateMemberRequest(
            created.Group.Id,
            "Member User",
            "9222000002",
            MemberRole.Member,
            0m,
            null), created.CreatorMember.Id, CancellationToken.None);

        await authService.ActivateAccountAsync(
            new ActivateAccountRequest("9222000002", added.InviteCode!, null, "InitialPass1!"),
            CancellationToken.None);

        return (created.Group.Id, created.CreatorMember.Id, added.Member.Id, "9222000002");
    }

    private static (AuthService Service, FakeEmailSender Email) CreateAuthService(AppDbContext context)
    {
        var jwtSettings = Options.Create(new JwtSettings
        {
            Key = "MySociety_Test_Signing_Key_At_Least_32_Chars!",
            Issuer = "MySociety",
            Audience = "MySociety",
            ExpiryMinutes = 60
        });

        var otpSettings = Options.Create(new OtpSettings { Required = false, ExposeCodeInApi = true });
        var emailSettings = Options.Create(new EmailSettings
        {
            PasswordReset = new PasswordResetEmailSettings
            {
                CodeLength = 6,
                ExpiryMinutes = 15,
                ResendCooldownSeconds = 0,
                ExposeCodeInApi = true
            }
        });

        var emailSender = new FakeEmailSender();

        var service = new AuthService(
            new UserRepository(context),
            new MemberInviteRepository(context),
            new PasswordResetRepository(context),
            new PhoneOtpRepository(context),
            new PasswordHasher(),
            new InviteCodeService(),
            new OtpService(),
            new FakeSmsSender(),
            emailSender,
            new JwtTokenService(jwtSettings),
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new SendActivationOtpRequestValidator(),
            new ActivateAccountRequestValidator(),
            new SendPasswordResetCodeRequestValidator(),
            new ResetPasswordRequestValidator(),
            otpSettings,
            emailSettings,
            NullLogger<AuthService>.Instance);

        return (service, emailSender);
    }
}
