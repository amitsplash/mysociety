using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class Asset : BaseEntity
{
    public Guid GroupId { get; set; }
    public Guid CreatedByMemberId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AssetCategory Category { get; set; } = AssetCategory.Other;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public string? VendorName { get; set; }
    public DateTime? InstallDate { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Active;
    public int MaintenanceIntervalDays { get; set; }
    public int AlertLeadDays { get; set; } = 7;
    public DateTime? NextDueDate { get; set; }
    public DateTime? LastAlertedForDueDate { get; set; }

    public Group Group { get; set; } = null!;
    public Member CreatedByMember { get; set; } = null!;
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = [];
}
