using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class MinuteRepository : IMinuteRepository
{
    private readonly AppDbContext _dbContext;

    public MinuteRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Minute?> GetByAgendaItemIdAsync(Guid agendaItemId, CancellationToken cancellationToken) =>
        _dbContext.Minutes.FirstOrDefaultAsync(x => x.AgendaItemId == agendaItemId, cancellationToken);

    public async Task AddAsync(Minute minute, CancellationToken cancellationToken) =>
        await _dbContext.Minutes.AddAsync(minute, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
