using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Expense>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken);
    Task AddAsync(Expense expense, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
