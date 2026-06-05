using System.Security.Cryptography;
using System.Text;
using MySociety.Application.Common.Interfaces;

namespace MySociety.Infrastructure.Security;

public class InviteCodeService : IInviteCodeService
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;

    public string GenerateCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(CodeLength);
        var chars = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(chars);
    }

    public string HashCode(string code)
    {
        var normalized = NormalizeCode(code);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToBase64String(hash);
    }

    public bool Verify(string code, string codeHash)
    {
        if (string.IsNullOrWhiteSpace(codeHash))
        {
            return false;
        }

        var normalized = NormalizeCode(code);
        var expected = Convert.FromBase64String(codeHash);
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string NormalizeCode(string code) =>
        code.Trim().ToUpperInvariant();
}
