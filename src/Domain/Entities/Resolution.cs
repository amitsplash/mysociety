using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class Resolution : BaseEntity
{
    public Guid GroupId { get; set; }
    public Guid MeetingId { get; set; }
    public Guid? AgendaItemId { get; set; }
    public Guid? OpenMatterId { get; set; }
    public string ResolutionNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ResolutionDate { get; set; }
    public decimal? ApprovedBudget { get; set; }
    public ResolutionStatus Status { get; set; } = ResolutionStatus.Open;
    public Guid CreatedByMemberId { get; set; }

    public Group Group { get; set; } = null!;
    public Meeting Meeting { get; set; } = null!;
    public AgendaItem? AgendaItem { get; set; }
    public OpenMatter? OpenMatter { get; set; }
    public Member CreatedByMember { get; set; } = null!;
}
