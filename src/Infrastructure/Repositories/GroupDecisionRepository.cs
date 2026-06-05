using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class GroupDecisionRepository : IGroupDecisionRepository
{
    private readonly AppDbContext _dbContext;

    public GroupDecisionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AgendaItem>> GetAgendaItemsWithDecisionsAsync(
        Guid groupId,
        Guid? meetingId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.AgendaItems
            .Include(x => x.Minute)
            .Include(x => x.Meeting)
            .Include(x => x.OpenMatter)
            .Where(x =>
                x.Meeting.GroupId == groupId &&
                x.Minute != null &&
                x.Minute.DecisionTaken != null &&
                x.Minute.DecisionTaken != "");

        if (meetingId.HasValue)
        {
            query = query.Where(x => x.MeetingId == meetingId.Value);
        }

        return await query
            .OrderByDescending(x => x.Meeting.MeetingDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Resolution>> GetResolutionsAsync(
        Guid groupId,
        Guid? meetingId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Resolutions
            .Include(x => x.Meeting)
            .Include(x => x.AgendaItem)
            .Where(x => x.GroupId == groupId);

        if (meetingId.HasValue)
        {
            query = query.Where(x => x.MeetingId == meetingId.Value);
        }

        return await query
            .OrderByDescending(x => x.ResolutionDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
