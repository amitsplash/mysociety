using MySociety.Domain.Common;

namespace MySociety.Domain.Entities;

public class GroupIncome : BaseEntity
{
    public Guid GroupId { get; set; }
    public Guid CreatedByMemberId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime IncomeDate { get; set; }

    public Group Group { get; set; } = null!;
    public Member CreatedByMember { get; set; } = null!;
}
