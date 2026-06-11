using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Notifications;
using MySociety.Application.Notifications.Dtos;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 100;

    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var userId = HttpContext.GetRequiredUserId();
        var pageSize = Math.Clamp(take, 1, MaxPageSize);
        var result = await _notificationService.ListForUserAsync(
            userId,
            Math.Max(skip, 0),
            pageSize,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadNotificationCountResponse>> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetRequiredUserId();
        var result = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<ActionResult<NotificationResponse>> MarkRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetRequiredUserId();
        var result = await _notificationService.MarkReadAsync(id, userId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetRequiredUserId();
        await _notificationService.MarkAllReadAsync(userId, cancellationToken);
        return NoContent();
    }
}
