using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly AppDbContext _dbContext;

    public GroupRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Groups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Group>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Groups
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Group>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Groups
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Group group, CancellationToken cancellationToken)
    {
        await _dbContext.Groups.AddAsync(group, cancellationToken);
    }

    public async Task DeleteByIdAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var memberIds = await _dbContext.Members
            .Where(m => m.GroupId == groupId)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        if (memberIds.Count > 0)
        {
            var invites = await _dbContext.MemberInvites
                .Where(i => memberIds.Contains(i.MemberId) || memberIds.Contains(i.CreatedByMemberId))
                .ToListAsync(cancellationToken);
            _dbContext.MemberInvites.RemoveRange(invites);

            var resetTokens = await _dbContext.PasswordResetTokens
                .Where(t => t.CreatedByMemberId.HasValue && memberIds.Contains(t.CreatedByMemberId.Value))
                .ToListAsync(cancellationToken);
            _dbContext.PasswordResetTokens.RemoveRange(resetTokens);
        }

        var group = await _dbContext.Groups.FirstOrDefaultAsync(x => x.Id == groupId, cancellationToken);
        if (group is not null)
        {
            _dbContext.Groups.Remove(group);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
