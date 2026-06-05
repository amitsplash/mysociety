using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class Meeting : BaseEntity
{
    public Guid GroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public MeetingType MeetingType { get; set; } = MeetingType.Regular;
    public DateTime MeetingDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Location { get; set; }
    public string? Summary { get; set; }
    public MeetingStatus Status { get; set; } = MeetingStatus.Draft;
    public Guid CreatedByMemberId { get; set; }

    public Group Group { get; set; } = null!;
    public Member CreatedByMember { get; set; } = null!;
    public ICollection<AgendaItem> AgendaItems { get; set; } = [];
    public ICollection<MeetingAttendee> Attendees { get; set; } = [];
    public ICollection<Resolution> Resolutions { get; set; } = [];
}
