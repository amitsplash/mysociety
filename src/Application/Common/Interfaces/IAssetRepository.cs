using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Asset?> GetByIdForGroupAsync(Guid id, Guid groupId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Asset>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Asset>> GetActiveScheduledAssetsAsync(CancellationToken cancellationToken);
    Task AddAsync(Asset asset, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
