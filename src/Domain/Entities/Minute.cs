using MySociety.Domain.Common;

namespace MySociety.Domain.Entities;

public class Minute : BaseEntity
{
    public Guid AgendaItemId { get; set; }
    public string? DiscussionSummary { get; set; }
    public string? DecisionTaken { get; set; }
    public decimal? BudgetApproved { get; set; }

    public AgendaItem AgendaItem { get; set; } = null!;
}
