namespace MySociety.Application.MaintenanceRecords.Dtos;

public record CreateMaintenanceRecordRequest(
    Guid AssetId,
    Guid GroupId,
    DateTime PerformedDate,
    string Description,
    decimal? Cost,
    string? VendorName,
    string? Notes);

public record MaintenanceRecordResponse(
    Guid Id,
    Guid AssetId,
    Guid GroupId,
    Guid CreatedByMemberId,
    string CreatedByName,
    DateTime PerformedDate,
    string Description,
    decimal? Cost,
    string? VendorName,
    string? Notes,
    DateTime CreatedAt);
