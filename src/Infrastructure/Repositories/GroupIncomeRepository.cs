using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class GroupIncomeRepository : IGroupIncomeRepository
{
    private readonly AppDbContext _dbContext;

    public GroupIncomeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<GroupIncome?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.GroupIncomes
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<GroupIncome>> GetByGroupIdAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.GroupIncomes
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .Where(x => x.GroupId == groupId)
            .OrderByDescending(x => x.IncomeDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GroupIncome income, CancellationToken cancellationToken)
    {
        await _dbContext.GroupIncomes.AddAsync(income, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
