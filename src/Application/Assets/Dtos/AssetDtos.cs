using MySociety.Domain.Enums;

namespace MySociety.Application.Assets.Dtos;

public record CreateAssetRequest(
    Guid GroupId,
    string Name,
    AssetCategory Category,
    string? Location,
    string? Description,
    string? SerialNumber,
    string? VendorName,
    DateTime? InstallDate,
    AssetStatus Status,
    int MaintenanceIntervalDays,
    int AlertLeadDays);

public record UpdateAssetRequest(
    string Name,
    AssetCategory Category,
    string? Location,
    string? Description,
    string? SerialNumber,
    string? VendorName,
    DateTime? InstallDate,
    AssetStatus Status,
    int MaintenanceIntervalDays,
    int AlertLeadDays);

public record AssetResponse(
    Guid Id,
    Guid GroupId,
    Guid CreatedByMemberId,
    string CreatedByName,
    string Name,
    AssetCategory Category,
    string? Location,
    string? Description,
    string? SerialNumber,
    string? VendorName,
    DateTime? InstallDate,
    AssetStatus Status,
    int MaintenanceIntervalDays,
    int AlertLeadDays,
    DateTime? NextDueDate,
    AssetMaintenanceStatus MaintenanceStatus,
    DateTime CreatedAt);

public record AssetMaintenanceSummaryResponse(
    int DueSoonCount,
    int OverdueCount,
    int TotalActiveAssets);
