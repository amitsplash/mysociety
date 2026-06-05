using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _dbContext;

    public PaymentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        await _dbContext.Payments.AddAsync(payment, cancellationToken);
    }

    public async Task<decimal> GetTotalPaidForContributionAsync(
        Guid contributionId,
        CancellationToken cancellationToken)
    {
        var amounts = await _dbContext.Payments
            .Where(p => p.ContributionId == contributionId)
            .Select(p => p.Amount)
            .ToListAsync(cancellationToken);

        return amounts.Sum();
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetTotalsPaidByContributionIdsAsync(
        IReadOnlyCollection<Guid> contributionIds,
        CancellationToken cancellationToken)
    {
        if (contributionIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var rows = await _dbContext.Payments
            .Where(p => p.ContributionId != null && contributionIds.Contains(p.ContributionId.Value))
            .Select(p => new { ContributionId = p.ContributionId!.Value, p.Amount })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.ContributionId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
