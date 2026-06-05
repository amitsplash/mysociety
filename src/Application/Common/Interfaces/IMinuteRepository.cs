using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IMinuteRepository
{
    Task<Minute?> GetByAgendaItemIdAsync(Guid agendaItemId, CancellationToken cancellationToken);
    Task AddAsync(Minute minute, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
