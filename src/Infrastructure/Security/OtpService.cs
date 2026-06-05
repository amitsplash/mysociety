using System.Security.Cryptography;
using System.Text;
using MySociety.Application.Common.Interfaces;

namespace MySociety.Infrastructure.Security;

public class OtpService : IOtpService
{
    public string GenerateCode(int length)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = (char)('0' + (bytes[i] % 10));
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
        new string(code.Where(char.IsDigit).ToArray());
}
