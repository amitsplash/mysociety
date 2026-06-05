namespace MySociety.Application.Common.Settings;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = "noreply@mysociety.local";
    public string FromName { get; set; } = "MySociety";
    public SmtpSettings Smtp { get; set; } = new();
    public PasswordResetEmailSettings PasswordReset { get; set; } = new();
}

public class SmtpSettings
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; } = true;
}

public class PasswordResetEmailSettings
{
    public int CodeLength { get; set; } = 6;
    public int ExpiryMinutes { get; set; } = 15;
    public int ResendCooldownSeconds { get; set; } = 60;
    /// <summary>When true, return the code in the API response (development only).</summary>
    public bool ExposeCodeInApi { get; set; }
}
