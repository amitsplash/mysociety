using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _dbContext;

    public NotificationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Notifications
            .Include(x => x.Group)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.ReadAt == null)
            .ThenByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Notifications.CountAsync(
            x => x.UserId == userId && x.ReadAt == null,
            cancellationToken);
    }

    public Task<Notification?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Notifications
            .Include(x => x.Group)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken)
    {
        await _dbContext.Notifications.AddRangeAsync(notifications, cancellationToken);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await _dbContext.Notifications
            .Where(x => x.UserId == userId && x.ReadAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.ReadAt, now),
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
