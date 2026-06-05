namespace MySociety.Application.Common.Settings;

public class OtpSettings
{
    public const string SectionName = "Otp";

    public int CodeLength { get; set; } = 6;
    public int ExpiryMinutes { get; set; } = 10;
    public int ResendCooldownSeconds { get; set; } = 60;
    /// <summary>When false, activation uses invite code only (no SMS OTP).</summary>
    public bool Required { get; set; }
    public bool ExposeCodeInApi { get; set; }
}
