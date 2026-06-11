using MySociety.Domain.Entities;
using MySociety.Domain.Enums;

namespace MySociety.Application.Common;

public static class MaintenanceScheduleRules
{
    public static DateTime? ComputeInitialNextDueDate(DateTime? installDate, int maintenanceIntervalDays, DateTime utcNow)
    {
        if (maintenanceIntervalDays <= 0)
        {
            return null;
        }

        var anchor = ExpenseDateRules.NormalizeToUtcDate(installDate ?? utcNow);
        return anchor.AddDays(maintenanceIntervalDays);
    }

    public static DateTime ComputeNextDueAfterMaintenance(DateTime performedDate, int maintenanceIntervalDays)
    {
        return ExpenseDateRules.NormalizeToUtcDate(performedDate).AddDays(maintenanceIntervalDays);
    }

    public static AssetMaintenanceStatus ComputeMaintenanceStatus(Asset asset, DateTime utcToday)
    {
        if (asset.Status != AssetStatus.Active || asset.NextDueDate is null || asset.MaintenanceIntervalDays <= 0)
        {
            return AssetMaintenanceStatus.NotScheduled;
        }

        var dueDate = ExpenseDateRules.NormalizeToUtcDate(asset.NextDueDate.Value);
        if (dueDate < utcToday)
        {
            return AssetMaintenanceStatus.Overdue;
        }

        var leadEnd = utcToday.AddDays(asset.AlertLeadDays);
        if (dueDate <= leadEnd)
        {
            return AssetMaintenanceStatus.DueSoon;
        }

        return AssetMaintenanceStatus.Ok;
    }

    public static bool ShouldAlertDueSoon(Asset asset, DateTime utcToday)
    {
        if (asset.Status != AssetStatus.Active || asset.NextDueDate is null || asset.MaintenanceIntervalDays <= 0)
        {
            return false;
        }

        var dueDate = ExpenseDateRules.NormalizeToUtcDate(asset.NextDueDate.Value);
        if (dueDate <= utcToday)
        {
            return false;
        }

        var leadEnd = utcToday.AddDays(asset.AlertLeadDays);
        if (dueDate > leadEnd)
        {
            return false;
        }

        return !DatesEqual(asset.LastAlertedForDueDate, dueDate);
    }

    public static bool ShouldAlertOverdue(Asset asset, DateTime utcToday)
    {
        if (asset.Status != AssetStatus.Active || asset.NextDueDate is null || asset.MaintenanceIntervalDays <= 0)
        {
            return false;
        }

        var dueDate = ExpenseDateRules.NormalizeToUtcDate(asset.NextDueDate.Value);
        if (dueDate >= utcToday)
        {
            return false;
        }

        return !DatesEqual(asset.LastAlertedForDueDate, dueDate);
    }

    private static bool DatesEqual(DateTime? left, DateTime right)
    {
        return left.HasValue
            && ExpenseDateRules.NormalizeToUtcDate(left.Value) == ExpenseDateRules.NormalizeToUtcDate(right);
    }
}
