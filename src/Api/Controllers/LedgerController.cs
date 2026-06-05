using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Financial;
using MySociety.Application.Ledgers;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class LedgerController : ControllerBase
{
    private readonly ILedgerQueryService _ledgerQueryService;
    private readonly IMemberRepository _memberRepository;

    public LedgerController(ILedgerQueryService ledgerQueryService, IMemberRepository memberRepository)
    {
        _ledgerQueryService = ledgerQueryService;
        _memberRepository = memberRepository;
    }

    [HttpGet("api/ledger/{memberId:guid}")]
    public async Task<ActionResult<MemberLedgerResponse>> GetMemberLedger(
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _ledgerQueryService.GetMemberLedgerAsync(memberId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/groups/{groupId:guid}/balances")]
    public async Task<ActionResult<IReadOnlyList<MemberBalanceDto>>> GetGroupBalances(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _ledgerQueryService.GetGroupBalancesAsync(groupId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/groups/{groupId:guid}/ledger-overview")]
    public async Task<ActionResult<GroupLedgerOverviewResponse>> GetGroupLedgerOverview(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _ledgerQueryService.GetGroupLedgerOverviewAsync(groupId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/groups/{groupId:guid}/fund-ledger")]
    public async Task<ActionResult<FundLedgerResponse>> GetFundLedger(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _ledgerQueryService.GetFundLedgerAsync(groupId, actingMemberId, cancellationToken);
        return Ok(result);
    }
}
