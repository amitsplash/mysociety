using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class MaintenanceRecordRepository : IMaintenanceRecordRepository
{
    private readonly AppDbContext _dbContext;

    public MaintenanceRecordRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<MaintenanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.MaintenanceRecords
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceRecord>> GetByAssetIdAsync(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.MaintenanceRecords
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .Where(x => x.AssetId == assetId)
            .OrderByDescending(x => x.PerformedDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasRecordsForAssetAsync(Guid assetId, CancellationToken cancellationToken)
    {
        return _dbContext.MaintenanceRecords.AnyAsync(x => x.AssetId == assetId, cancellationToken);
    }

    public Task<MaintenanceRecord?> GetLatestForAssetAsync(Guid assetId, CancellationToken cancellationToken)
    {
        return _dbContext.MaintenanceRecords
            .Where(x => x.AssetId == assetId)
            .OrderByDescending(x => x.PerformedDate)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(MaintenanceRecord record, CancellationToken cancellationToken)
    {
        await _dbContext.MaintenanceRecords.AddAsync(record, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
