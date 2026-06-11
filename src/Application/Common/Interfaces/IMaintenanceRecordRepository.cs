using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IMaintenanceRecordRepository
{
    Task<MaintenanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaintenanceRecord>> GetByAssetIdAsync(Guid assetId, CancellationToken cancellationToken);
    Task<bool> HasRecordsForAssetAsync(Guid assetId, CancellationToken cancellationToken);
    Task<MaintenanceRecord?> GetLatestForAssetAsync(Guid assetId, CancellationToken cancellationToken);
    Task AddAsync(MaintenanceRecord record, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
