using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly AppDbContext _dbContext;

    public AssetRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Assets
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Asset?> GetByIdForGroupAsync(Guid id, Guid groupId, CancellationToken cancellationToken)
    {
        return _dbContext.Assets
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id && x.GroupId == groupId, cancellationToken);
    }

    public async Task<IReadOnlyList<Asset>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return await _dbContext.Assets
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .Where(x => x.GroupId == groupId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Asset>> GetActiveScheduledAssetsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Assets
            .Where(x => x.Status == AssetStatus.Active
                && x.MaintenanceIntervalDays > 0
                && x.NextDueDate != null)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Asset asset, CancellationToken cancellationToken)
    {
        await _dbContext.Assets.AddAsync(asset, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
