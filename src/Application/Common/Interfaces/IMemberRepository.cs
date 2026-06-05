using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Member?> GetByIdWithUserAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Member>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken);
    Task<bool> ExistsInGroupAsync(Guid groupId, Guid userId, CancellationToken cancellationToken);
    Task AddAsync(Member member, CancellationToken cancellationToken);
    Task RemoveAsync(Member member, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
