using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IGroupExpenseRepository
{
    Task<GroupExpense?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<GroupExpense>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken);

    Task<decimal> GetTotalByGroupIdAsync(Guid groupId, CancellationToken cancellationToken);

    Task AddAsync(GroupExpense expense, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
