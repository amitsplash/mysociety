using MySociety.Domain.Entities;
using MySociety.Domain.Enums;

namespace MySociety.Application.Common.Interfaces;

public interface ICommitteeMemberRepository
{
    Task<CommitteeMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommitteeMember>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken);
    Task<bool> ExistsForMemberAsync(Guid groupId, Guid memberId, CancellationToken cancellationToken);
    Task<bool> ExistsForMemberExceptAsync(Guid groupId, Guid memberId, Guid excludeId, CancellationToken cancellationToken);
    Task<bool> ExistsForOfficerRoleAsync(Guid groupId, CommitteeRole role, CancellationToken cancellationToken);
    Task<bool> ExistsForOfficerRoleExceptAsync(Guid groupId, CommitteeRole role, Guid excludeId, CancellationToken cancellationToken);
    Task<bool> IsCommitteeMemberAsync(Guid groupId, Guid memberId, CancellationToken cancellationToken);
    Task AddAsync(CommitteeMember committeeMember, CancellationToken cancellationToken);
    Task RemoveAsync(CommitteeMember committeeMember, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
