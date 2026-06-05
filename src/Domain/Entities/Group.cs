using MySociety.Domain.Common;

using MySociety.Domain.Enums;



namespace MySociety.Domain.Entities;



public class Group : BaseEntity

{

    public string Name { get; set; } = string.Empty;

    public GroupType Type { get; set; }

    public ContributionModel ContributionModel { get; set; }

    public decimal ContributionAmount { get; set; }

    public ContributionFrequency ContributionFrequency { get; set; }

    public decimal OpeningMaintenanceBalance { get; set; }

    public decimal OpeningCorpusBalance { get; set; }

    public Guid CreatedByUserId { get; set; }



    public User CreatedByUser { get; set; } = null!;

    public ICollection<Member> Members { get; set; } = [];

    public ICollection<Contribution> Contributions { get; set; } = [];

    public ICollection<Expense> Expenses { get; set; } = [];

    public ICollection<GroupExpense> GroupExpenses { get; set; } = [];

    public ICollection<LedgerEntry> LedgerEntries { get; set; } = [];

    public ICollection<CommitteeMember> CommitteeMembers { get; set; } = [];

    public ICollection<Meeting> Meetings { get; set; } = [];

    public ICollection<OpenMatter> OpenMatters { get; set; } = [];

    public ICollection<Resolution> Resolutions { get; set; } = [];

}

