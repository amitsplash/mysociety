using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class Contribution : BaseEntity
{
    public Guid MemberId { get; set; }
    public Guid GroupId { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public ContributionStatus Status { get; set; } = ContributionStatus.Pending;

    public Member Member { get; set; } = null!;
    public Group Group { get; set; } = null!;
}
