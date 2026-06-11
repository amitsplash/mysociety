using MySociety.Application.Notifications;
using MySociety.Application.Notifications.Dtos;
using MySociety.Domain.Enums;

namespace MySociety.Application.Tests;

internal sealed class FakeNotificationService : INotificationService
{
    public Task NotifyGroupMembersAsync(
        Guid groupId,
        NotificationType type,
        string title,
        string body,
        object? data,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task NotifyGroupAdminsAsync(
        Guid groupId,
        NotificationType type,
        string title,
        string body,
        object? data,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NotificationResponse>> ListForUserAsync(
        Guid userId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<NotificationResponse>>([]);
    }

    public Task<UnreadNotificationCountResponse> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new UnreadNotificationCountResponse(0));
    }

    public Task<NotificationResponse> MarkReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
