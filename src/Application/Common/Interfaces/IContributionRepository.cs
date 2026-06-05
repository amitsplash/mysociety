using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IContributionRepository
{
    Task<Contribution?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Contribution?> GetByMemberAndPeriodAsync(Guid memberId, string period, CancellationToken cancellationToken);
    Task<bool> ExistsForGroupAndPeriodAsync(Guid groupId, string period, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetDistinctPeriodsByGroupIdAsync(Guid groupId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Contribution>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Contribution>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken);
    Task AddAsync(Contribution contribution, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
