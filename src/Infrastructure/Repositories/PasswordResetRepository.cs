using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class PasswordResetRepository : IPasswordResetRepository
{
    private readonly AppDbContext _dbContext;

    public PasswordResetRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken)
    {
        await _dbContext.PasswordResetTokens.AddAsync(token, cancellationToken);
    }

    public Task<PasswordResetToken?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
    }

    public Task<PasswordResetToken?> GetLatestByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.PasswordResetTokens
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task InvalidateActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var active = await _dbContext.PasswordResetTokens
            .Where(x => x.UserId == userId && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var item in active)
        {
            item.UsedAt = now;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
