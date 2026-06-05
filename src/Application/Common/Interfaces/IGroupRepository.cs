using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Group>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Group>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(Group group, CancellationToken cancellationToken);
    Task DeleteByIdAsync(Guid groupId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
