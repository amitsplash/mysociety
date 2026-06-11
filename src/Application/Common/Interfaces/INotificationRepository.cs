using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken);

    Task<Notification?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken);

    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
