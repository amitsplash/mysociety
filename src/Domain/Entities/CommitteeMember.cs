using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class CommitteeMember : BaseEntity
{
    public Guid GroupId { get; set; }
    public Guid MemberId { get; set; }
    public CommitteeRole Role { get; set; }

    public Group Group { get; set; } = null!;
    public Member Member { get; set; } = null!;
}
