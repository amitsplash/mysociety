using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class GroupExpense : BaseEntity
{
    public Guid GroupId { get; set; }
    public Guid CreatedByMemberId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public GroupFundType FundType { get; set; } = GroupFundType.Maintenance;

    public Group Group { get; set; } = null!;
    public Member CreatedByMember { get; set; } = null!;
}
