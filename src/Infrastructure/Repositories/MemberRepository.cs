using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly AppDbContext _dbContext;

    public MemberRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Members.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Member?> GetByIdWithUserAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Members
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Member>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return await _dbContext.Members
            .Include(x => x.User)
            .Where(x => x.GroupId == groupId)
            .OrderBy(x => x.User.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsInGroupAsync(Guid groupId, Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.Members.AnyAsync(x => x.GroupId == groupId && x.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Member member, CancellationToken cancellationToken)
    {
        await _dbContext.Members.AddAsync(member, cancellationToken);
    }

    public Task RemoveAsync(Member member, CancellationToken cancellationToken)
    {
        _dbContext.Members.Remove(member);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
