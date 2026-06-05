using MySociety.Domain.Common;

namespace MySociety.Domain.Entities;

public class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public Guid? CreatedByMemberId { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public User User { get; set; } = null!;
    public Member? CreatedByMember { get; set; }
    public User? CreatedByUser { get; set; }
}
