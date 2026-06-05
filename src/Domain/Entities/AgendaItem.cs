using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class AgendaItem : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Guid? OpenMatterId { get; set; }
    public int AgendaNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public AgendaItemSource Source { get; set; }
    public MeetingItemOutcome Outcome { get; set; } = MeetingItemOutcome.NotDiscussed;
    public string? DiscussionSummary { get; set; }

    public Meeting Meeting { get; set; } = null!;
    public OpenMatter? OpenMatter { get; set; }
    public Minute? Minute { get; set; }
}
