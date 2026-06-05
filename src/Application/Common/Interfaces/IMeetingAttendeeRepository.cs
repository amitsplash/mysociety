using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IMeetingAttendeeRepository
{
    Task<IReadOnlyList<MeetingAttendee>> GetByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken);
    Task ReplaceForMeetingAsync(Guid meetingId, IReadOnlyList<MeetingAttendee> attendees, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
