using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IAgendaItemRepository
{
    Task<AgendaItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgendaItem>> GetByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken);
    Task AddAsync(AgendaItem agendaItem, CancellationToken cancellationToken);
    Task RemoveAsync(AgendaItem agendaItem, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
