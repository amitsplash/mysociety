using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class Expense : BaseEntity
{
    public Guid GroupId { get; set; }
    public Guid CreatedByMemberId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;
    public Guid? ApprovedByMemberId { get; set; }

    public Group Group { get; set; } = null!;
    public Member CreatedByMember { get; set; } = null!;
    public Member? ApprovedByMember { get; set; }
}
