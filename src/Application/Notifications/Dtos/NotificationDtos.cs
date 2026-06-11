using MySociety.Domain.Enums;

namespace MySociety.Application.Notifications.Dtos;

public record NotificationResponse(
    Guid Id,
    Guid GroupId,
    string GroupName,
    NotificationType Type,
    string Title,
    string Body,
    string? DataJson,
    DateTime? ReadAt,
    DateTime CreatedAt);

public record UnreadNotificationCountResponse(int Count);
