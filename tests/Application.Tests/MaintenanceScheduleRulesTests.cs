using MySociety.Application.Common;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;

namespace MySociety.Application.Tests;

public class MaintenanceScheduleRulesTests
{
    private static readonly DateTime Today = new(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ComputeInitialNextDueDate_UsesInstallDatePlusInterval()
    {
        var installDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = MaintenanceScheduleRules.ComputeInitialNextDueDate(installDate, 90, Today);

        Assert.Equal(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ComputeInitialNextDueDate_UsesTodayWhenInstallDateMissing()
    {
        var result = MaintenanceScheduleRules.ComputeInitialNextDueDate(null, 30, Today);

        Assert.Equal(Today.AddDays(30), result);
    }

    [Fact]
    public void ComputeNextDueAfterMaintenance_AddsIntervalToPerformedDate()
    {
        var performedDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var result = MaintenanceScheduleRules.ComputeNextDueAfterMaintenance(performedDate, 60);

        Assert.Equal(new DateTime(2026, 5, 14, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ComputeMaintenanceStatus_ReturnsOverdueWhenPastDue()
    {
        var asset = CreateAsset(nextDueDate: Today.AddDays(-1));

        var status = MaintenanceScheduleRules.ComputeMaintenanceStatus(asset, Today);

        Assert.Equal(AssetMaintenanceStatus.Overdue, status);
    }

    [Fact]
    public void ComputeMaintenanceStatus_ReturnsDueSoonWithinLeadWindow()
    {
        var asset = CreateAsset(nextDueDate: Today.AddDays(5), alertLeadDays: 7);

        var status = MaintenanceScheduleRules.ComputeMaintenanceStatus(asset, Today);

        Assert.Equal(AssetMaintenanceStatus.DueSoon, status);
    }

    [Fact]
    public void ComputeMaintenanceStatus_ReturnsOkWhenOutsideLeadWindow()
    {
        var asset = CreateAsset(nextDueDate: Today.AddDays(20), alertLeadDays: 7);

        var status = MaintenanceScheduleRules.ComputeMaintenanceStatus(asset, Today);

        Assert.Equal(AssetMaintenanceStatus.Ok, status);
    }

    [Fact]
    public void ShouldAlertDueSoon_SkipsWhenAlreadyAlertedForDueDate()
    {
        var dueDate = Today.AddDays(3);
        var asset = CreateAsset(nextDueDate: dueDate, alertLeadDays: 7);
        asset.LastAlertedForDueDate = dueDate;

        Assert.False(MaintenanceScheduleRules.ShouldAlertDueSoon(asset, Today));
    }

    [Fact]
    public void ShouldAlertDueSoon_ReturnsTrueWhenEligible()
    {
        var asset = CreateAsset(nextDueDate: Today.AddDays(3), alertLeadDays: 7);

        Assert.True(MaintenanceScheduleRules.ShouldAlertDueSoon(asset, Today));
    }

    [Fact]
    public void ShouldAlertOverdue_ReturnsTrueWhenPastDueAndNotAlerted()
    {
        var dueDate = Today.AddDays(-2);
        var asset = CreateAsset(nextDueDate: dueDate);

        Assert.True(MaintenanceScheduleRules.ShouldAlertOverdue(asset, Today));
    }

    [Fact]
    public void ShouldAlertOverdue_SkipsWhenAlreadyAlertedForDueDate()
    {
        var dueDate = Today.AddDays(-2);
        var asset = CreateAsset(nextDueDate: dueDate);
        asset.LastAlertedForDueDate = dueDate;

        Assert.False(MaintenanceScheduleRules.ShouldAlertOverdue(asset, Today));
    }

    [Fact]
    public void ShouldAlertDueSoon_SkipsInactiveAssets()
    {
        var asset = CreateAsset(nextDueDate: Today.AddDays(2), alertLeadDays: 7);
        asset.Status = AssetStatus.Inactive;

        Assert.False(MaintenanceScheduleRules.ShouldAlertDueSoon(asset, Today));
    }

    private static Asset CreateAsset(DateTime nextDueDate, int alertLeadDays = 7)
    {
        return new Asset
        {
            Id = Guid.NewGuid(),
            GroupId = Guid.NewGuid(),
            CreatedByMemberId = Guid.NewGuid(),
            Name = "Lift A",
            Status = AssetStatus.Active,
            MaintenanceIntervalDays = 90,
            AlertLeadDays = alertLeadDays,
            NextDueDate = nextDueDate,
            CreatedAt = Today,
        };
    }
}
