using MySociety.Domain.Entities;
using MySociety.Domain.Enums;

namespace MySociety.Application.Common.Interfaces;

public interface IOpenMatterRepository
{
    Task<OpenMatter?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpenMatter>> GetByGroupIdAsync(Guid groupId, OpenMatterStatus? statusFilter, CancellationToken cancellationToken);
    Task AddAsync(OpenMatter openMatter, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
