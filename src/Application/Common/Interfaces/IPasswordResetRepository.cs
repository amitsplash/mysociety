using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IPasswordResetRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken);
    Task<PasswordResetToken?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<PasswordResetToken?> GetLatestByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task InvalidateActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
