using MySociety.Application.Common;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Notifications;
using MySociety.Domain.Enums;

namespace MySociety.Application.MaintenanceAlerts;

public interface IMaintenanceAlertService
{
    Task ProcessDueAlertsAsync(CancellationToken cancellationToken);
}

public class MaintenanceAlertService : IMaintenanceAlertService
{
    private readonly IAssetRepository _assetRepository;
    private readonly INotificationService _notificationService;

    public MaintenanceAlertService(
        IAssetRepository assetRepository,
        INotificationService notificationService)
    {
        _assetRepository = assetRepository;
        _notificationService = notificationService;
    }

    public async Task ProcessDueAlertsAsync(CancellationToken cancellationToken)
    {
        var assets = await _assetRepository.GetActiveScheduledAssetsAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;

        foreach (var asset in assets)
        {
            if (MaintenanceScheduleRules.ShouldAlertOverdue(asset, today))
            {
                await NotifyAsync(asset, NotificationType.MaintenanceOverdue, "overdue", cancellationToken);
                asset.LastAlertedForDueDate = asset.NextDueDate;
                continue;
            }

            if (MaintenanceScheduleRules.ShouldAlertDueSoon(asset, today))
            {
                await NotifyAsync(asset, NotificationType.MaintenanceDueSoon, "dueSoon", cancellationToken);
                asset.LastAlertedForDueDate = asset.NextDueDate;
            }
        }

        await _assetRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyAsync(
        Domain.Entities.Asset asset,
        NotificationType type,
        string status,
        CancellationToken cancellationToken)
    {
        var dueDate = asset.NextDueDate!.Value;
        var dueLabel = dueDate.ToString("dd MMM yyyy");
        var title = type == NotificationType.MaintenanceOverdue
            ? $"Maintenance overdue: {asset.Name}"
            : $"Maintenance due soon: {asset.Name}";
        var body = type == NotificationType.MaintenanceOverdue
            ? $"{asset.Name} was due on {dueLabel}. Please schedule preventive maintenance."
            : $"{asset.Name} is due on {dueLabel}.";

        await _notificationService.NotifyGroupAdminsAsync(
            asset.GroupId,
            type,
            title,
            body,
            new
            {
                assetId = asset.Id,
                assetName = asset.Name,
                nextDueDate = dueDate,
                status,
            },
            excludeUserId: null,
            cancellationToken);
    }
}
