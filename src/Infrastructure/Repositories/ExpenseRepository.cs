using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _dbContext;

    public ExpenseRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Expenses
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Expense>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return await _dbContext.Expenses
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .Where(x => x.GroupId == groupId)
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Expense expense, CancellationToken cancellationToken)
    {
        await _dbContext.Expenses.AddAsync(expense, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
