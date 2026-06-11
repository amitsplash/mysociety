using MySociety.Domain.Common;

namespace MySociety.Domain.Entities;

public class MaintenanceRecord : BaseEntity
{
    public Guid AssetId { get; set; }
    public Guid GroupId { get; set; }
    public Guid CreatedByMemberId { get; set; }
    public DateTime PerformedDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? Cost { get; set; }
    public string? VendorName { get; set; }
    public string? Notes { get; set; }

    public Asset Asset { get; set; } = null!;
    public Group Group { get; set; } = null!;
    public Member CreatedByMember { get; set; } = null!;
}
