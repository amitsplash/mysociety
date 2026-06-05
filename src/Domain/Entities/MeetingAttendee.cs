using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class MeetingAttendee : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Guid MemberId { get; set; }
    public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Present;

    public Meeting Meeting { get; set; } = null!;
    public Member Member { get; set; } = null!;
}
