namespace MySociety.Application.Common.Interfaces;

public interface IOtpService
{
    string GenerateCode(int length);
    string HashCode(string code);
    bool Verify(string code, string codeHash);
}
