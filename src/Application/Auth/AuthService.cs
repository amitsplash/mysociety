using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySociety.Application.Auth.Dtos;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Common.Settings;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.Auth;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<SendActivationOtpResponse> SendActivationOtpAsync(
        SendActivationOtpRequest request,
        CancellationToken cancellationToken);
    Task<LoginResponse> ActivateAccountAsync(ActivateAccountRequest request, CancellationToken cancellationToken);
    Task<SendPasswordResetCodeResponse> SendPasswordResetCodeAsync(
        SendPasswordResetCodeRequest request,
        CancellationToken cancellationToken);
    Task<LoginResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IMemberInviteRepository _memberInviteRepository;
    private readonly IPasswordResetRepository _passwordResetRepository;
    private readonly IPhoneOtpRepository _phoneOtpRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IInviteCodeService _inviteCodeService;
    private readonly IOtpService _otpService;
    private readonly ISmsSender _smsSender;
    private readonly IEmailSender _emailSender;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<SendActivationOtpRequest> _sendOtpValidator;
    private readonly IValidator<ActivateAccountRequest> _activateValidator;
    private readonly IValidator<SendPasswordResetCodeRequest> _sendPasswordResetValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
    private readonly OtpSettings _otpSettings;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IMemberInviteRepository memberInviteRepository,
        IPasswordResetRepository passwordResetRepository,
        IPhoneOtpRepository phoneOtpRepository,
        IPasswordHasher passwordHasher,
        IInviteCodeService inviteCodeService,
        IOtpService otpService,
        ISmsSender smsSender,
        IEmailSender emailSender,
        IJwtTokenService jwtTokenService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<SendActivationOtpRequest> sendOtpValidator,
        IValidator<ActivateAccountRequest> activateValidator,
        IValidator<SendPasswordResetCodeRequest> sendPasswordResetValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator,
        IOptions<OtpSettings> otpSettings,
        IOptions<EmailSettings> emailSettings,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _memberInviteRepository = memberInviteRepository;
        _passwordResetRepository = passwordResetRepository;
        _phoneOtpRepository = phoneOtpRepository;
        _passwordHasher = passwordHasher;
        _inviteCodeService = inviteCodeService;
        _otpService = otpService;
        _smsSender = smsSender;
        _emailSender = emailSender;
        _jwtTokenService = jwtTokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _sendOtpValidator = sendOtpValidator;
        _activateValidator = activateValidator;
        _sendPasswordResetValidator = sendPasswordResetValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _otpSettings = otpSettings.Value;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task<LoginResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors.Select(x => x.ErrorMessage));
        }

        var username = request.Username.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.ExistsByUsernameAsync(username, cancellationToken))
        {
            throw new ConflictException("This username is already taken.");
        }

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new ConflictException("This email is already registered.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            Name = request.Name.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User registered {UserId} ({Username})", user.Id, user.Username);

        return BuildLoginResponse(user);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors.Select(x => x.ErrorMessage));
        }

        var key = request.Username.Trim();
        var user = await _userRepository.GetByUsernameOrPhoneWithMembershipsAsync(key, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid username or password.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid username or password.");
        }

        return BuildLoginResponse(user);
    }

    public async Task<SendActivationOtpResponse> SendActivationOtpAsync(
        SendActivationOtpRequest request,
        CancellationToken cancellationToken)
    {
        if (!_otpSettings.Required)
        {
            throw new ValidationException("OTP verification is not enabled.");
        }

        var validation = await _sendOtpValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors.Select(x => x.ErrorMessage));
        }

        var phone = request.Phone.Trim();
        await EnsurePendingActivationAsync(phone, cancellationToken);

        var latest = await _phoneOtpRepository.GetLatestAsync(phone, OtpPurpose.AccountActivation, cancellationToken);
        if (latest is not null)
        {
            var elapsed = DateTime.UtcNow - latest.CreatedAt;
            if (elapsed.TotalSeconds < _otpSettings.ResendCooldownSeconds)
            {
                var waitSeconds = _otpSettings.ResendCooldownSeconds - (int)elapsed.TotalSeconds;
                throw new ValidationException(
                    $"Please wait {waitSeconds} seconds before requesting another code.");
            }
        }

        await _phoneOtpRepository.InvalidateActiveAsync(phone, OtpPurpose.AccountActivation, cancellationToken);

        var code = _otpService.GenerateCode(_otpSettings.CodeLength);
        var verification = new PhoneOtpVerification
        {
            Id = Guid.NewGuid(),
            Phone = phone,
            Purpose = OtpPurpose.AccountActivation,
            CodeHash = _otpService.HashCode(code),
            ExpiresAt = DateTime.UtcNow.AddMinutes(_otpSettings.ExpiryMinutes),
            CreatedAt = DateTime.UtcNow
        };

        await _phoneOtpRepository.AddAsync(verification, cancellationToken);
        await _phoneOtpRepository.SaveChangesAsync(cancellationToken);

        var message =
            $"Your MySociety verification code is {code}. Valid for {_otpSettings.ExpiryMinutes} minutes.";
        await _smsSender.SendAsync(phone, message, cancellationToken);

        _logger.LogInformation("Activation OTP sent for phone {Phone}", phone);

        return new SendActivationOtpResponse(
            "Verification code sent.",
            _otpSettings.ExpiryMinutes * 60,
            _otpSettings.ExposeCodeInApi ? code : null);
    }

    public async Task<LoginResponse> ActivateAccountAsync(
        ActivateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _activateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors.Select(x => x.ErrorMessage));
        }

        var phone = request.Phone.Trim();
        var user = await _userRepository.GetByPhoneWithMembershipsAsync(phone, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedException(InvalidActivationMessage);
        }

        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new ConflictException("Account is already activated. Sign in instead.");
        }

        PhoneOtpVerification? otpRecord = null;
        if (_otpSettings.Required)
        {
            if (string.IsNullOrWhiteSpace(request.Otp))
            {
                throw new ValidationException("OTP is required.");
            }

            otpRecord = await _phoneOtpRepository.GetActiveAsync(phone, OtpPurpose.AccountActivation, cancellationToken);
            if (otpRecord is null || !_otpService.Verify(request.Otp, otpRecord.CodeHash))
            {
                throw new UnauthorizedException("Invalid or expired OTP.");
            }
        }

        var invites = await _memberInviteRepository.GetUnusedByUserPhoneAsync(phone, cancellationToken);
        var matchedInvite = invites.FirstOrDefault(x => _inviteCodeService.Verify(request.InviteCode, x.CodeHash));
        if (matchedInvite is null)
        {
            throw new UnauthorizedException(InvalidActivationMessage);
        }

        user.PasswordHash = _passwordHasher.Hash(request.Password);
        matchedInvite.UsedAt = DateTime.UtcNow;
        if (otpRecord is not null)
        {
            otpRecord.UsedAt = DateTime.UtcNow;
        }

        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Account activated for user {UserId}", user.Id);

        return BuildLoginResponse(user);
    }

    public async Task<SendPasswordResetCodeResponse> SendPasswordResetCodeAsync(
        SendPasswordResetCodeRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _sendPasswordResetValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors.Select(x => x.ErrorMessage));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var resetSettings = _emailSettings.PasswordReset;

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return new SendPasswordResetCodeResponse(
                "If an account exists for this email, a reset code has been sent.",
                resetSettings.ExpiryMinutes * 60,
                null);
        }

        var latest = await _passwordResetRepository.GetLatestByUserIdAsync(user.Id, cancellationToken);
        if (latest is not null)
        {
            var elapsed = DateTime.UtcNow - latest.CreatedAt;
            if (elapsed.TotalSeconds < resetSettings.ResendCooldownSeconds)
            {
                var waitSeconds = resetSettings.ResendCooldownSeconds - (int)elapsed.TotalSeconds;
                throw new ValidationException(
                    $"Please wait {waitSeconds} seconds before requesting another code.");
            }
        }

        await _passwordResetRepository.InvalidateActiveByUserIdAsync(user.Id, cancellationToken);

        var code = _otpService.GenerateCode(resetSettings.CodeLength);
        var expiresAt = DateTime.UtcNow.AddMinutes(resetSettings.ExpiryMinutes);
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = _otpService.HashCode(code),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        await _passwordResetRepository.AddAsync(token, cancellationToken);
        await _passwordResetRepository.SaveChangesAsync(cancellationToken);

        var body =
            $"Your MySociety password reset code is {code}. It expires in {resetSettings.ExpiryMinutes} minutes. " +
            "If you did not request this, you can ignore this email.";
        await _emailSender.SendAsync(
            user.Email,
            "MySociety password reset",
            body,
            cancellationToken);

        _logger.LogInformation("Password reset code emailed for user {UserId}", user.Id);

        return new SendPasswordResetCodeResponse(
            "If an account exists for this email, a reset code has been sent.",
            resetSettings.ExpiryMinutes * 60,
            resetSettings.ExposeCodeInApi ? code : null);
    }

    public async Task<LoginResponse> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _resetPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors.Select(x => x.ErrorMessage));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailWithMembershipsAsync(email, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or reset code.");
        }

        var resetToken = await _passwordResetRepository.GetActiveByUserIdAsync(user.Id, cancellationToken);
        if (resetToken is null || !VerifyResetCode(request.ResetCode, resetToken))
        {
            throw new UnauthorizedException("Invalid email or reset code.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        resetToken.UsedAt = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);

        return BuildLoginResponse(user);
    }

    private bool VerifyResetCode(string code, PasswordResetToken token)
    {
        if (_otpService.Verify(code, token.CodeHash))
        {
            return true;
        }

        return token.CreatedByMemberId.HasValue && _inviteCodeService.Verify(code, token.CodeHash);
    }

    private LoginResponse BuildLoginResponse(User user)
    {
        var jwt = _jwtTokenService.CreateToken(user);
        var memberships = user.Memberships
            .Select(m => new MembershipSummary(
                m.Id,
                m.GroupId,
                m.Group.Name,
                m.Role))
            .ToList();

        return new LoginResponse(
            jwt,
            new UserSummary(user.Id, user.Username, user.Email, user.Name, user.Phone),
            memberships);
    }

    private static string InvalidActivationMessage =>
        "Invalid phone or invite code.";

    private async Task EnsurePendingActivationAsync(string phone, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByPhoneAsync(phone, cancellationToken);
        if (user is null || !string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new NotFoundException("No account is pending activation for this phone number.");
        }

        var invites = await _memberInviteRepository.GetUnusedByUserPhoneAsync(phone, cancellationToken);
        if (invites.Count == 0)
        {
            throw new NotFoundException("No account is pending activation for this phone number.");
        }
    }
}
