namespace MySociety.Application.Common;

public static class InviteUserCredentials
{
    public static string UsernameForPhone(string phone) => $"user_{phone}";

    public static string EmailForPhone(string phone) => $"{phone}@invite.local";

    public static bool IsPlaceholderEmail(string email) =>
        email.EndsWith("@invite.local", StringComparison.OrdinalIgnoreCase);
}
