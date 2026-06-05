using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class OpenMatter : BaseEntity
{
    public Guid GroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public OpenMatterStatus Status { get; set; } = OpenMatterStatus.Open;
    public DateTime RaisedAt { get; set; }
    public Guid? LastDiscussedInMeetingId { get; set; }
    public Guid CreatedByMemberId { get; set; }

    public Group Group { get; set; } = null!;
    public Member CreatedByMember { get; set; } = null!;
    public Meeting? LastDiscussedInMeeting { get; set; }
    public ICollection<AgendaItem> AgendaItems { get; set; } = [];
}
