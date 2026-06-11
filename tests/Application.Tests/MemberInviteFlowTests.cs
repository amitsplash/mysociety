using FluentValidation;
using MySociety.Application.Auth;
using MySociety.Application.Auth.Dtos;
using MySociety.Application.Auth.Validators;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Settings;
using MySociety.Application.Groups;
using MySociety.Application.Groups.Dtos;
using MySociety.Application.Groups.Validators;
using MySociety.Application.Members;
using MySociety.Application.Members.Dtos;
using MySociety.Application.Members.Validators;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;
using MySociety.Infrastructure.Security;
using MySociety.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.Tests;

public class MemberInviteFlowTests
{
    [Fact]
    public async Task CreateMember_without_password_returns_invite()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminMemberId) = await SeedGroupAsync(context);

        var memberService = CreateMemberService(context);
        var result = await memberService.CreateAsync(new CreateMemberRequest(
            groupId,
            "New Member",
            "9555000001",
            MemberRole.Member,
            0m,
            null), adminMemberId, CancellationToken.None);

        Assert.True(result.RequiresActivation);
        Assert.NotNull(result.InviteCode);
        Assert.Equal(8, result.InviteCode!.Length);
        Assert.NotNull(result.InviteExpiresAt);
        Assert.Single(context.MemberInvites);
    }

    [Fact]
    public async Task CreateMember_when_user_has_password_skips_invite()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminMemberId) = await SeedGroupAsync(context);
        await TestData.AddUserAsync(context, "9555000002", "Existing User");

        var memberService = CreateMemberService(context);
        var result = await memberService.CreateAsync(new CreateMemberRequest(
            groupId,
            "Existing User",
            "9555000002",
            MemberRole.Member,
            0m,
            null), adminMemberId, CancellationToken.None);

        Assert.False(result.RequiresActivation);
        Assert.Null(result.InviteCode);
        Assert.Empty(context.MemberInvites);
    }

    [Fact]
    public async Task Register_when_phone_pending_activation_returns_code()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminMemberId) = await SeedGroupAsync(context);
        var memberService = CreateMemberService(context);
        await memberService.CreateAsync(new CreateMemberRequest(
            groupId,
            "Pending User",
            "9555111100",
            MemberRole.Member,
            0m,
            null), adminMemberId, CancellationToken.None);

        var authService = CreateAuthService(context);

        var ex = await Assert.ThrowsAsync<ConflictException>(async () =>
            await authService.RegisterAsync(
                new RegisterRequest("9555111100", "new@example.com", "New User", "Password123!"),
                CancellationToken.None));

        Assert.Equal("PENDING_ACTIVATION", ex.Code);
    }

    [Fact]
    public async Task Activate_with_email_updates_placeholder_email()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminMemberId) = await SeedGroupAsync(context);
        var memberService = CreateMemberService(context);
        var created = await memberService.CreateAsync(new CreateMemberRequest(
            groupId,
            "Email User",
            "9555111101",
            MemberRole.Member,
            0m,
            null), adminMemberId, CancellationToken.None);

        var authService = CreateAuthService(context);
        await authService.ActivateAccountAsync(
            new ActivateAccountRequest(
                "9555111101",
                created.InviteCode!,
                null,
                "NewPass123!",
                "real@example.com",
                "Updated Name"),
            CancellationToken.None);

        var storedUser = context.Users.Single(u => u.Phone == "9555111101");
        Assert.Equal("real@example.com", storedUser.Email);
        Assert.Equal("Updated Name", storedUser.Name);
    }

    [Fact]
    public async Task Activate_with_valid_invite_sets_password_without_otp()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminMemberId) = await SeedGroupAsync(context);
        var memberService = CreateMemberService(context);
        var created = await memberService.CreateAsync(new CreateMemberRequest(
            groupId,
            "Activate Me",
            "9555000003",
            MemberRole.Member,
            0m,
            null), adminMemberId, CancellationToken.None);

        var authService = CreateAuthService(context);
        var login = await authService.ActivateAccountAsync(
            new ActivateAccountRequest("9555000003", created.InviteCode!, null, "NewPass123!"),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        Assert.NotEmpty(login.Memberships);

        var storedUser = context.Users.Single(u => u.Phone == "9555000003");
        Assert.False(string.IsNullOrWhiteSpace(storedUser.PasswordHash));

        var invite = context.MemberInvites.Single();
        Assert.NotNull(invite.UsedAt);
    }

    [Fact]
    public async Task Activate_with_wrong_invite_code_fails()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminMemberId) = await SeedGroupAsync(context);
        var memberService = CreateMemberService(context);
        await memberService.CreateAsync(new CreateMemberRequest(
            groupId,
            "Wrong Code",
            "9555000004",
            MemberRole.Member,
            0m,
            null), adminMemberId, CancellationToken.None);

        var authService = CreateAuthService(context);

        await Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await authService.ActivateAccountAsync(
                new ActivateAccountRequest("9555000004", "WRONGCOD", null, "NewPass123!"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Activate_with_expired_invite_fails()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminMemberId) = await SeedGroupAsync(context);
        var memberService = CreateMemberService(context);
        var created = await memberService.CreateAsync(new CreateMemberRequest(
            groupId,
            "Expired",
            "9555000005",
            MemberRole.Member,
            0m,
            null), adminMemberId, CancellationToken.None);

        var invite = context.MemberInvites.Single();
        invite.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();

        var authService = CreateAuthService(context);

        await Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await authService.ActivateAccountAsync(
                new ActivateAccountRequest("9555000005", created.InviteCode!, null, "NewPass123!"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Activate_when_already_activated_returns_conflict()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var user = await TestData.AddUserAsync(context, "9555000006", "Activated User");
        var authService = CreateAuthService(context);

        await Assert.ThrowsAsync<ConflictException>(async () =>
            await authService.ActivateAccountAsync(
                new ActivateAccountRequest(user.Phone, "ABCDEFGH", null, "NewPass123!"),
                CancellationToken.None));
    }

    [Fact]
    public async Task SendActivationOtp_fails_when_otp_not_required()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminMemberId) = await SeedGroupAsync(context);
        var memberService = CreateMemberService(context);
        await memberService.CreateAsync(new CreateMemberRequest(
            groupId,
            "No Otp User",
            "9555000009",
            MemberRole.Member,
            0m,
            null), adminMemberId, CancellationToken.None);

        var authService = CreateAuthService(context, otpRequired: false);

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await authService.SendActivationOtpAsync(
                new SendActivationOtpRequest("9555000009"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Activate_with_otp_when_required()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminMemberId) = await SeedGroupAsync(context);
        var memberService = CreateMemberService(context);
        var created = await memberService.CreateAsync(new CreateMemberRequest(
            groupId,
            "Otp User",
            "9555000010",
            MemberRole.Member,
            0m,
            null), adminMemberId, CancellationToken.None);

        var (authService, sms) = CreateAuthServiceWithSms(context, otpRequired: true);
        var otp = await SendOtpAsync(authService, sms, "9555000010");

        var login = await authService.ActivateAccountAsync(
            new ActivateAccountRequest("9555000010", created.InviteCode!, otp, "NewPass123!"),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(login.Token));
    }

    private static async Task<string> SendOtpAsync(
        AuthService authService,
        FakeSmsSender sms,
        string phone)
    {
        var response = await authService.SendActivationOtpAsync(
            new SendActivationOtpRequest(phone),
            CancellationToken.None);

        return response.Otp ?? sms.ExtractLastOtp()
            ?? throw new InvalidOperationException("OTP was not captured.");
    }

    private static async Task<(Guid GroupId, Guid AdminMemberId)> SeedGroupAsync(AppDbContext context)
    {
        var owner = await TestData.AddRegisteredUserAsync(context, "invite_owner", "invite@test.com", "Owner");
        var groupService = TestData.CreateGroupService(context);
        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(name: "Invite Test Group"),
            CancellationToken.None);

        return (created.Group.Id, created.CreatorMember.Id);
    }

    private static MemberService CreateMemberService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new UserRepository(context),
            new LedgerService(context),
            new UnitOfWork(context),
            new MemberInviteRepository(context),
            new PasswordResetRepository(context),
            new InviteCodeService(),
            new CreateMemberRequestValidator(),
            new UpdateMemberRequestValidator());

    private static AuthService CreateAuthService(AppDbContext context, bool otpRequired = false) =>
        CreateAuthServiceWithSms(context, otpRequired).Service;

    private static (AuthService Service, FakeSmsSender Sms) CreateAuthServiceWithSms(
        AppDbContext context,
        bool otpRequired)
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
            Required = otpRequired,
            CodeLength = 6,
            ExpiryMinutes = 10,
            ResendCooldownSeconds = 60,
            ExposeCodeInApi = true
        });

        var sms = new FakeSmsSender();

        var service = new AuthService(
            new UserRepository(context),
            new MemberInviteRepository(context),
            new PasswordResetRepository(context),
            new PhoneOtpRepository(context),
            new PasswordHasher(),
            new InviteCodeService(),
            new OtpService(),
            sms,
            new FakeEmailSender(),
            new JwtTokenService(jwtSettings),
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new SendActivationOtpRequestValidator(),
            new ActivateAccountRequestValidator(),
            new SendPasswordResetCodeRequestValidator(),
            new ResetPasswordRequestValidator(),
            otpSettings,
            Options.Create(new EmailSettings()),
            NullLogger<AuthService>.Instance);

        return (service, sms);
    }
}
