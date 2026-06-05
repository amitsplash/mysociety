namespace MySociety.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "MySociety";
    public string Audience { get; set; } = "MySociety";
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 1440;
}
