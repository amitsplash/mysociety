using MySociety.Domain.Enums;

namespace MySociety.Application.Auth.Dtos;

public record LoginRequest(string Username, string Password);

public record RegisterRequest(string Username, string Email, string Name, string Password);

public record SendActivationOtpRequest(string Phone);

public record SendActivationOtpResponse(
    string Message,
    int ExpiresInSeconds,
    string? Otp);

public record ActivateAccountRequest(string Phone, string InviteCode, string? Otp, string Password);

public record SendPasswordResetCodeRequest(string Email);

public record SendPasswordResetCodeResponse(
    string Message,
    int ExpiresInSeconds,
    string? ResetCode);

public record ResetPasswordRequest(string Email, string ResetCode, string NewPassword);

public record LoginResponse(
    string Token,
    UserSummary User,
    IReadOnlyList<MembershipSummary> Memberships);

public record UserSummary(
    Guid Id,
    string Username,
    string Email,
    string Name,
    string? Phone);

public record MembershipSummary(
    Guid MemberId,
    Guid GroupId,
    string GroupName,
    MemberRole Role);
