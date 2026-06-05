using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class OpenMatterRepository : IOpenMatterRepository
{
    private readonly AppDbContext _dbContext;

    public OpenMatterRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<OpenMatter?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.OpenMatters
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<OpenMatter>> GetByGroupIdAsync(
        Guid groupId,
        OpenMatterStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.OpenMatters
            .Include(x => x.CreatedByMember)
            .ThenInclude(x => x.User)
            .Where(x => x.GroupId == groupId);

        if (statusFilter.HasValue)
        {
            query = query.Where(x => x.Status == statusFilter.Value);
        }

        return await query
            .OrderByDescending(x => x.RaisedAt)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(OpenMatter openMatter, CancellationToken cancellationToken)
    {
        await _dbContext.OpenMatters.AddAsync(openMatter, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
