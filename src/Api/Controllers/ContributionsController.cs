using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Contributions;
using MySociety.Application.Contributions.Dtos;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class ContributionsController : ControllerBase
{
    private readonly IContributionService _contributionService;
    private readonly IMemberRepository _memberRepository;

    public ContributionsController(
        IContributionService contributionService,
        IMemberRepository memberRepository)
    {
        _contributionService = contributionService;
        _memberRepository = memberRepository;
    }

    [HttpPost("api/contributions/generate")]
    public async Task<ActionResult<GenerateContributionsResponse>> Generate(
        [FromBody] GenerateContributionsRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _contributionService.GenerateAsync(request, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/payments")]
    public async Task<ActionResult<PaymentResponse>> RecordPayment(
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _contributionService.RecordPaymentAsync(request, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/members/{memberId:guid}/contributions")]
    public async Task<ActionResult<IReadOnlyList<ContributionResponse>>> GetByMemberId(
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _contributionService.GetByMemberIdAsync(memberId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/groups/{groupId:guid}/contributions")]
    public async Task<ActionResult<IReadOnlyList<ContributionResponse>>> GetByGroupId(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _contributionService.GetByGroupIdAsync(groupId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/groups/{groupId:guid}/contributions/pending-summary")]
    public async Task<ActionResult<GroupPendingContributionsResponse>> GetPendingSummary(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _contributionService.GetPendingSummaryAsync(groupId, actingMemberId, cancellationToken);
        return Ok(result);
    }
}
