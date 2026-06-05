using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class MeetingRepository : IMeetingRepository
{
    private readonly AppDbContext _dbContext;

    public MeetingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Meeting?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Meetings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Meeting?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Meetings
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .Include(x => x.AgendaItems)
            .ThenInclude(x => x.OpenMatter)
            .Include(x => x.AgendaItems)
            .ThenInclude(x => x.Minute)
            .Include(x => x.Resolutions)
            .ThenInclude(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .Include(x => x.Attendees)
            .ThenInclude(x => x.Member)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Meeting>> GetByGroupIdAsync(
        Guid groupId,
        MeetingStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Meetings
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .Include(x => x.AgendaItems)
            .Where(x => x.GroupId == groupId);

        if (statusFilter.HasValue)
        {
            query = query.Where(x => x.Status == statusFilter.Value);
        }

        return await query
            .OrderByDescending(x => x.MeetingDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Meeting meeting, CancellationToken cancellationToken)
    {
        await _dbContext.Meetings.AddAsync(meeting, cancellationToken);
    }

    public Task RemoveAsync(Meeting meeting, CancellationToken cancellationToken)
    {
        _dbContext.Meetings.Remove(meeting);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
