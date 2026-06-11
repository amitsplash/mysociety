using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IGroupIncomeRepository
{
    Task<GroupIncome?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<GroupIncome>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken);

    Task AddAsync(GroupIncome income, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
