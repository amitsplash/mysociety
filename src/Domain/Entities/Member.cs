using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class Member : BaseEntity
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public MemberRole Role { get; set; } = MemberRole.Member;
    public decimal? SquareFeet { get; set; }

    public decimal CorpusAmount { get; set; }

    public DateTime? CorpusPaidAt { get; set; }

    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<Contribution> Contributions { get; set; } = [];
    public ICollection<Expense> ExpensesCreated { get; set; } = [];
    public ICollection<Expense> ExpensesApproved { get; set; } = [];
    public ICollection<GroupExpense> GroupExpensesCreated { get; set; } = [];
    public ICollection<LedgerEntry> LedgerEntries { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
