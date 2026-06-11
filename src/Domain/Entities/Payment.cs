using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid MemberId { get; set; }
    public Guid GroupId { get; set; }
    public Guid? ContributionId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.PendingApproval;
    public Guid SubmissionId { get; set; }
    public Guid RecordedByMemberId { get; set; }
    public Guid? ApprovedByMemberId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public Member Member { get; set; } = null!;
    public Group Group { get; set; } = null!;
    public Contribution? Contribution { get; set; }
    public Member RecordedByMember { get; set; } = null!;
    public Member? ApprovedByMember { get; set; }
}
