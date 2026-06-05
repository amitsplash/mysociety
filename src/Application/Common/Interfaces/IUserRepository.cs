using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByUsernameOrPhoneWithMembershipsAsync(string usernameOrPhone, CancellationToken cancellationToken);
    Task<User?> GetByPhoneWithMembershipsAsync(string phone, CancellationToken cancellationToken);
    Task<User?> GetByUsernameWithMembershipsAsync(string username, CancellationToken cancellationToken);
    Task<User?> GetByEmailWithMembershipsAsync(string email, CancellationToken cancellationToken);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
