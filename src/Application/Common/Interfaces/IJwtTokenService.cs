using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(User user);
}
