using System.Text.Json;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Notifications.Dtos;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;

namespace MySociety.Application.Notifications;

public interface INotificationService
{
    Task NotifyGroupMembersAsync(
        Guid groupId,
        NotificationType type,
        string title,
        string body,
        object? data,
        Guid? excludeUserId,
        CancellationToken cancellationToken);

    Task NotifyGroupAdminsAsync(
        Guid groupId,
        NotificationType type,
        string title,
        string body,
        object? data,
        Guid? excludeUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationResponse>> ListForUserAsync(
        Guid userId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<UnreadNotificationCountResponse> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<NotificationResponse> MarkReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken);

    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken);
}

public class NotificationService : INotificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IMemberRepository _memberRepository;
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(
        IMemberRepository memberRepository,
        INotificationRepository notificationRepository)
    {
        _memberRepository = memberRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task NotifyGroupMembersAsync(
        Guid groupId,
        NotificationType type,
        string title,
        string body,
        object? data,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        var members = await _memberRepository.GetByGroupIdAsync(groupId, cancellationToken);
        if (members.Count == 0)
        {
            return;
        }

        var dataJson = data is null ? null : JsonSerializer.Serialize(data, JsonOptions);
        var now = DateTime.UtcNow;
        var notifications = members
            .Where(m => excludeUserId is null || m.UserId != excludeUserId)
            .Select(m => new Notification
            {
                Id = Guid.NewGuid(),
                UserId = m.UserId,
                GroupId = groupId,
                Type = type,
                Title = title,
                Body = body,
                DataJson = dataJson,
                CreatedAt = now,
            })
            .ToList();

        if (notifications.Count == 0)
        {
            return;
        }

        await _notificationRepository.AddRangeAsync(notifications, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyGroupAdminsAsync(
        Guid groupId,
        NotificationType type,
        string title,
        string body,
        object? data,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        var members = await _memberRepository.GetByGroupIdAsync(groupId, cancellationToken);
        var admins = members.Where(m => m.Role == MemberRole.Admin).ToList();
        if (admins.Count == 0)
        {
            return;
        }

        var dataJson = data is null ? null : JsonSerializer.Serialize(data, JsonOptions);
        var now = DateTime.UtcNow;
        var notifications = admins
            .Where(m => excludeUserId is null || m.UserId != excludeUserId)
            .Select(m => new Notification
            {
                Id = Guid.NewGuid(),
                UserId = m.UserId,
                GroupId = groupId,
                Type = type,
                Title = title,
                Body = body,
                DataJson = dataJson,
                CreatedAt = now,
            })
            .ToList();

        if (notifications.Count == 0)
        {
            return;
        }

        await _notificationRepository.AddRangeAsync(notifications, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationResponse>> ListForUserAsync(
        Guid userId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var notifications = await _notificationRepository.GetByUserIdAsync(
            userId,
            skip,
            take,
            cancellationToken);

        return notifications.Select(MapToResponse).ToList();
    }

    public async Task<UnreadNotificationCountResponse> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var count = await _notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
        return new UnreadNotificationCountResponse(count);
    }

    public async Task<NotificationResponse> MarkReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdForUserAsync(
            notificationId,
            userId,
            cancellationToken)
            ?? throw new NotFoundException("Notification not found.");

        if (notification.ReadAt is null)
        {
            notification.ReadAt = DateTime.UtcNow;
            await _notificationRepository.SaveChangesAsync(cancellationToken);
        }

        return MapToResponse(notification);
    }

    public Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _notificationRepository.MarkAllReadAsync(userId, cancellationToken);
    }

    private static NotificationResponse MapToResponse(Notification notification)
    {
        return new NotificationResponse(
            notification.Id,
            notification.GroupId,
            notification.Group.Name,
            notification.Type,
            notification.Title,
            notification.Body,
            notification.DataJson,
            notification.ReadAt,
            notification.CreatedAt);
    }
}
