using FluentValidation;
using MySociety.Application.Auth.Dtos;

namespace MySociety.Application.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20).Matches(@"^\d{10,15}$")
            .WithMessage("Phone must be 10–15 digits.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20).Matches(@"^\d{10,15}$")
            .WithMessage("Phone must be 10–15 digits.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class SendActivationOtpRequestValidator : AbstractValidator<SendActivationOtpRequest>
{
    public SendActivationOtpRequestValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
    }
}

public class ActivateAccountRequestValidator : AbstractValidator<ActivateAccountRequest>
{
    public ActivateAccountRequestValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20).Matches(@"^\d{10,15}$")
            .WithMessage("Phone must be 10–15 digits.");
        RuleFor(x => x.InviteCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email).EmailAddress().MaximumLength(256);
        });
        When(x => !string.IsNullOrWhiteSpace(x.Name), () =>
        {
            RuleFor(x => x.Name).MaximumLength(200);
        });
        When(x => !string.IsNullOrWhiteSpace(x.Otp), () =>
        {
            RuleFor(x => x.Otp).Length(6).Matches(@"^\d{6}$").WithMessage("OTP must be a 6-digit code.");
        });
    }
}

public class SendPasswordResetCodeRequestValidator : AbstractValidator<SendPasswordResetCodeRequest>
{
    public SendPasswordResetCodeRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.ResetCode)
            .NotEmpty()
            .Must(code => code.Length == 6 || code.Length == 8)
            .WithMessage("Reset code must be 6 or 8 characters.");
        RuleFor(x => x.ResetCode)
            .Matches(@"^\d{6}$")
            .When(x => x.ResetCode.Length == 6)
            .WithMessage("Email reset codes must be 6 digits.");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
