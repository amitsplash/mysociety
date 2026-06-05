using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
    Task<decimal> GetTotalPaidForContributionAsync(Guid contributionId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, decimal>> GetTotalsPaidByContributionIdsAsync(
        IReadOnlyCollection<Guid> contributionIds,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
