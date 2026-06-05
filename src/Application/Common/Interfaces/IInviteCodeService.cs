namespace MySociety.Application.Common.Interfaces;

public interface IInviteCodeService
{
    string GenerateCode();
    string HashCode(string code);
    bool Verify(string code, string codeHash);
}
