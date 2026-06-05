using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class MeetingAttendeeRepository : IMeetingAttendeeRepository
{
    private readonly AppDbContext _dbContext;

    public MeetingAttendeeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MeetingAttendee>> GetByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        return await _dbContext.MeetingAttendees
            .Include(x => x.Member)
            .ThenInclude(x => x.User)
            .Where(x => x.MeetingId == meetingId)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceForMeetingAsync(
        Guid meetingId,
        IReadOnlyList<MeetingAttendee> attendees,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.MeetingAttendees
            .Where(x => x.MeetingId == meetingId)
            .ToListAsync(cancellationToken);

        _dbContext.MeetingAttendees.RemoveRange(existing);
        await _dbContext.MeetingAttendees.AddRangeAsync(attendees, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
