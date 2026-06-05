using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class MemberInviteRepository : IMemberInviteRepository
{
    private readonly AppDbContext _dbContext;

    public MemberInviteRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(MemberInvite invite, CancellationToken cancellationToken)
    {
        await _dbContext.MemberInvites.AddAsync(invite, cancellationToken);
    }

    public Task<MemberInvite?> GetUnusedByMemberIdAsync(Guid memberId, CancellationToken cancellationToken)
    {
        return _dbContext.MemberInvites
            .FirstOrDefaultAsync(
                x => x.MemberId == memberId && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
    }

    public async Task<IReadOnlyList<MemberInvite>> GetUnusedByUserPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        return await _dbContext.MemberInvites
            .Include(x => x.Member)
            .ThenInclude(x => x.User)
            .Where(x =>
                x.Member.User.Phone == phone
                && x.UsedAt == null
                && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
