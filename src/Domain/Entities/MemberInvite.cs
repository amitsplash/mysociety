using MySociety.Domain.Common;

namespace MySociety.Domain.Entities;

public class MemberInvite : BaseEntity
{
    public Guid MemberId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public Guid CreatedByMemberId { get; set; }

    public Member Member { get; set; } = null!;
    public Member CreatedByMember { get; set; } = null!;
}
