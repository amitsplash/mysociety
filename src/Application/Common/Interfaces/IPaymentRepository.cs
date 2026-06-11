using MySociety.Domain.Entities;
using MySociety.Domain.Enums;

namespace MySociety.Application.Common.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Payment>> GetBySubmissionIdAsync(Guid submissionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Payment>> GetPendingApprovalByGroupIdAsync(Guid groupId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Payment>> GetPendingApprovalByMemberIdAsync(Guid memberId, CancellationToken cancellationToken);
    Task<decimal> GetTotalPaidForContributionAsync(Guid contributionId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, decimal>> GetTotalsPaidByContributionIdsAsync(
        IReadOnlyCollection<Guid> contributionIds,
        CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, decimal>> GetPendingApprovalTotalsByContributionIdsAsync(
        IReadOnlyCollection<Guid> contributionIds,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
