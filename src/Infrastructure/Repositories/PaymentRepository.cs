using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
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

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Payments
            .Include(x => x.Member)
            .ThenInclude(x => x.User)
            .Include(x => x.Contribution)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetBySubmissionIdAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        return await _dbContext.Payments
            .Include(x => x.Member)
            .ThenInclude(x => x.User)
            .Include(x => x.Contribution)
            .Where(x => x.SubmissionId == submissionId)
            .OrderBy(x => x.ContributionId == null ? 1 : 0)
            .ThenBy(x => x.Contribution!.Period)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetPendingApprovalByGroupIdAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return await _dbContext.Payments
            .Include(x => x.Member)
            .ThenInclude(x => x.User)
            .Include(x => x.Contribution)
            .Where(x => x.GroupId == groupId && x.Status == PaymentStatus.PendingApproval)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetPendingApprovalByMemberIdAsync(Guid memberId, CancellationToken cancellationToken)
    {
        return await _dbContext.Payments
            .Include(x => x.Contribution)
            .Where(x => x.MemberId == memberId && x.Status == PaymentStatus.PendingApproval)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalPaidForContributionAsync(
        Guid contributionId,
        CancellationToken cancellationToken)
    {
        var amounts = await _dbContext.Payments
            .Where(p => p.ContributionId == contributionId && p.Status == PaymentStatus.Approved)
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
            .Where(p =>
                p.ContributionId != null &&
                contributionIds.Contains(p.ContributionId.Value) &&
                p.Status == PaymentStatus.Approved)
            .Select(p => new { ContributionId = p.ContributionId!.Value, p.Amount })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.ContributionId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetPendingApprovalTotalsByContributionIdsAsync(
        IReadOnlyCollection<Guid> contributionIds,
        CancellationToken cancellationToken)
    {
        if (contributionIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var rows = await _dbContext.Payments
            .Where(p =>
                p.ContributionId != null &&
                contributionIds.Contains(p.ContributionId.Value) &&
                p.Status == PaymentStatus.PendingApproval)
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
