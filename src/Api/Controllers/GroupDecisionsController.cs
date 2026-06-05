using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.GroupDecisions;
using MySociety.Application.GroupDecisions.Dtos;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class GroupDecisionsController : ControllerBase
{
    private readonly IGroupDecisionService _groupDecisionService;
    private readonly IMemberRepository _memberRepository;

    public GroupDecisionsController(
        IGroupDecisionService groupDecisionService,
        IMemberRepository memberRepository)
    {
        _groupDecisionService = groupDecisionService;
        _memberRepository = memberRepository;
    }

    [HttpGet("api/groups/{groupId:guid}/group-decisions")]
    public async Task<ActionResult<IReadOnlyList<GroupDecisionResponse>>> GetByGroupId(
        Guid groupId,
        [FromQuery] GroupDecisionFilter? filter,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        return Ok(await _groupDecisionService.GetByGroupIdAsync(
            groupId, actingMemberId, filter, cancellationToken));
    }

    [HttpGet("api/groups/{groupId:guid}/meetings/{meetingId:guid}/group-decisions")]
    public async Task<ActionResult<IReadOnlyList<GroupDecisionResponse>>> GetByMeetingId(
        Guid groupId,
        Guid meetingId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        return Ok(await _groupDecisionService.GetByMeetingIdAsync(
            groupId, meetingId, actingMemberId, cancellationToken));
    }
}
