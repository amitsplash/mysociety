using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class GroupExpenseRepository : IGroupExpenseRepository
{
    private readonly AppDbContext _dbContext;

    public GroupExpenseRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<GroupExpense?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.GroupExpenses
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<GroupExpense>> GetByGroupIdAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.GroupExpenses
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .Where(x => x.GroupId == groupId)
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<decimal> GetTotalByGroupIdAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return _dbContext.GroupExpenses
            .Where(x => x.GroupId == groupId)
            .SumAsync(x => x.Amount, cancellationToken);
    }

    public async Task AddAsync(GroupExpense expense, CancellationToken cancellationToken)
    {
        await _dbContext.GroupExpenses.AddAsync(expense, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
