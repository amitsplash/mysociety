using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class ContributionRepository : IContributionRepository
{
    private readonly AppDbContext _dbContext;

    public ContributionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Contribution?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Contributions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Contribution?> GetByMemberAndPeriodAsync(Guid memberId, string period, CancellationToken cancellationToken)
    {
        return _dbContext.Contributions.FirstOrDefaultAsync(
            x => x.MemberId == memberId && x.Period == period,
            cancellationToken);
    }

    public Task<bool> ExistsForGroupAndPeriodAsync(Guid groupId, string period, CancellationToken cancellationToken)
    {
        return _dbContext.Contributions.AnyAsync(
            x => x.GroupId == groupId && x.Period == period,
            cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctPeriodsByGroupIdAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Contributions
            .Where(x => x.GroupId == groupId)
            .Select(x => x.Period)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Contribution>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken)
    {
        return await _dbContext.Contributions
            .Where(x => x.MemberId == memberId)
            .OrderByDescending(x => x.Period)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Contribution>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return await _dbContext.Contributions
            .AsNoTracking()
            .Include(x => x.Member)
            .ThenInclude(m => m.User)
            .Where(x => x.GroupId == groupId)
            .OrderByDescending(x => x.Period)
            .ThenBy(x => x.Member.User.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Contribution contribution, CancellationToken cancellationToken)
    {
        await _dbContext.Contributions.AddAsync(contribution, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
