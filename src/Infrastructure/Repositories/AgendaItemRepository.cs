using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class AgendaItemRepository : IAgendaItemRepository
{
    private readonly AppDbContext _dbContext;

    public AgendaItemRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AgendaItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.AgendaItems
            .Include(x => x.Meeting)
            .Include(x => x.OpenMatter)
            .Include(x => x.Minute)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AgendaItem>> GetByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        return await _dbContext.AgendaItems
            .Include(x => x.OpenMatter)
            .Include(x => x.Minute)
            .Where(x => x.MeetingId == meetingId)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.AgendaNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AgendaItem agendaItem, CancellationToken cancellationToken)
    {
        await _dbContext.AgendaItems.AddAsync(agendaItem, cancellationToken);
    }

    public Task RemoveAsync(AgendaItem agendaItem, CancellationToken cancellationToken)
    {
        _dbContext.AgendaItems.Remove(agendaItem);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
