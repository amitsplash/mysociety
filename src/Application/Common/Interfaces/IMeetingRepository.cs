using MySociety.Domain.Entities;
using MySociety.Domain.Enums;

namespace MySociety.Application.Common.Interfaces;

public interface IMeetingRepository
{
    Task<Meeting?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Meeting?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Meeting>> GetByGroupIdAsync(Guid groupId, MeetingStatus? statusFilter, CancellationToken cancellationToken);
    Task AddAsync(Meeting meeting, CancellationToken cancellationToken);
    Task RemoveAsync(Meeting meeting, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
