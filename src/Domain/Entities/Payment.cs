using MySociety.Domain.Common;

namespace MySociety.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid MemberId { get; set; }
    public Guid GroupId { get; set; }
    public Guid? ContributionId { get; set; }
    public decimal Amount { get; set; }

    public Member Member { get; set; } = null!;
    public Group Group { get; set; } = null!;
    public Contribution? Contribution { get; set; }
}
