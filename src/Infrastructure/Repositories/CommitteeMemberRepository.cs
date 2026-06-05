using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class CommitteeMemberRepository : ICommitteeMemberRepository
{
    private readonly AppDbContext _dbContext;

    public CommitteeMemberRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CommitteeMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.CommitteeMembers
            .Include(x => x.Member)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CommitteeMember>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return await _dbContext.CommitteeMembers
            .Include(x => x.Member)
            .ThenInclude(x => x.User)
            .Where(x => x.GroupId == groupId)
            .OrderBy(x => x.Role)
            .ThenBy(x => x.Member.User.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsForMemberAsync(Guid groupId, Guid memberId, CancellationToken cancellationToken)
    {
        return _dbContext.CommitteeMembers
            .AnyAsync(x => x.GroupId == groupId && x.MemberId == memberId, cancellationToken);
    }

    public Task<bool> ExistsForMemberExceptAsync(
        Guid groupId,
        Guid memberId,
        Guid excludeId,
        CancellationToken cancellationToken)
    {
        return _dbContext.CommitteeMembers
            .AnyAsync(
                x => x.GroupId == groupId && x.MemberId == memberId && x.Id != excludeId,
                cancellationToken);
    }

    public Task<bool> ExistsForOfficerRoleAsync(
        Guid groupId,
        CommitteeRole role,
        CancellationToken cancellationToken)
    {
        return _dbContext.CommitteeMembers
            .AnyAsync(x => x.GroupId == groupId && x.Role == role, cancellationToken);
    }

    public Task<bool> ExistsForOfficerRoleExceptAsync(
        Guid groupId,
        CommitteeRole role,
        Guid excludeId,
        CancellationToken cancellationToken)
    {
        return _dbContext.CommitteeMembers
            .AnyAsync(
                x => x.GroupId == groupId && x.Role == role && x.Id != excludeId,
                cancellationToken);
    }

    public Task<bool> IsCommitteeMemberAsync(Guid groupId, Guid memberId, CancellationToken cancellationToken)
    {
        return _dbContext.CommitteeMembers
            .AnyAsync(x => x.GroupId == groupId && x.MemberId == memberId, cancellationToken);
    }

    public async Task AddAsync(CommitteeMember committeeMember, CancellationToken cancellationToken)
    {
        await _dbContext.CommitteeMembers.AddAsync(committeeMember, cancellationToken);
    }

    public Task RemoveAsync(CommitteeMember committeeMember, CancellationToken cancellationToken)
    {
        _dbContext.CommitteeMembers.Remove(committeeMember);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
