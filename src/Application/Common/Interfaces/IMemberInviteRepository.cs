using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IMemberInviteRepository
{
    Task AddAsync(MemberInvite invite, CancellationToken cancellationToken);
    Task<MemberInvite?> GetUnusedByMemberIdAsync(Guid memberId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MemberInvite>> GetUnusedByUserPhoneAsync(string phone, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
