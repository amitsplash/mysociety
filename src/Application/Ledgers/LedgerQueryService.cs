using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Financial;
using MySociety.Domain.Enums;

namespace MySociety.Application.Ledgers;

public class LedgerQueryService : ILedgerQueryService
{
    private readonly IMemberRepository _memberRepository;
    private readonly ILedgerService _ledgerService;

    public LedgerQueryService(IMemberRepository memberRepository, ILedgerService ledgerService)
    {
        _memberRepository = memberRepository;
        _ledgerService = ledgerService;
    }

    public async Task<MemberLedgerResponse> GetMemberLedgerAsync(
        Guid memberId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdWithUserAsync(memberId, cancellationToken)
            ?? throw new NotFoundException("Member not found.");

        var actingMember = await _memberRepository.GetByIdAsync(actingMemberId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid acting member.");

        if (actingMember.GroupId != member.GroupId)
        {
            throw new ForbiddenException("You are not a member of this group.");
        }

        var isAdmin = actingMember.Role == MemberRole.Admin;
        if (!isAdmin && actingMemberId != memberId)
        {
            throw new ForbiddenException("You can only view your own ledger.");
        }

        var entries = await _ledgerService.GetEntriesAsync(memberId, member.GroupId, cancellationToken);
        var balance = await _ledgerService.GetBalanceAsync(memberId, member.GroupId, cancellationToken);

        return new MemberLedgerResponse(memberId, member.GroupId, balance, entries);
    }

    public async Task<IReadOnlyList<MemberBalanceDto>> GetGroupBalancesAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        return await _ledgerService.GetGroupBalancesAsync(groupId, cancellationToken);
    }

    public async Task<GroupLedgerOverviewResponse> GetGroupLedgerOverviewAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        var members = await _memberRepository.GetByGroupIdAsync(groupId, cancellationToken);
        var summaries = new List<MemberLedgerSummary>();

        foreach (var member in members)
        {
            var entries = await _ledgerService.GetEntriesAsync(member.Id, groupId, cancellationToken);
            var balance = await _ledgerService.GetBalanceAsync(member.Id, groupId, cancellationToken);
            summaries.Add(new MemberLedgerSummary(
                member.Id,
                member.User.Name,
                balance,
                entries));
        }

        return new GroupLedgerOverviewResponse(groupId, summaries);
    }

    public async Task<FundLedgerResponse> GetFundLedgerAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        return await _ledgerService.GetFundLedgerAsync(groupId, cancellationToken);
    }
}
