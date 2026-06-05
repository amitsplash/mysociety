using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Committee;
using MySociety.Application.Committee.Dtos;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Enums;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class CommitteeMembersController : ControllerBase
{
    private readonly ICommitteeMemberService _committeeMemberService;
    private readonly IMemberRepository _memberRepository;

    public CommitteeMembersController(
        ICommitteeMemberService committeeMemberService,
        IMemberRepository memberRepository)
    {
        _committeeMemberService = committeeMemberService;
        _memberRepository = memberRepository;
    }

    [HttpGet("api/groups/{groupId:guid}/committee-members")]
    public async Task<ActionResult<IReadOnlyList<CommitteeMemberResponse>>> GetByGroupId(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _committeeMemberService.GetByGroupIdAsync(groupId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/groups/{groupId:guid}/committee-members")]
    public async Task<ActionResult<CommitteeMemberResponse>> Create(
        Guid groupId,
        [FromBody] CreateCommitteeMemberBody body,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var request = new CreateCommitteeMemberRequest(groupId, body.MemberId, body.Role);
        var result = await _committeeMemberService.CreateAsync(request, actingMemberId, cancellationToken);
        return CreatedAtAction(nameof(GetByGroupId), new { groupId }, result);
    }

    [HttpPut("api/groups/{groupId:guid}/committee-members/{id:guid}")]
    public async Task<ActionResult<CommitteeMemberResponse>> Update(
        Guid groupId,
        Guid id,
        [FromBody] UpdateCommitteeMemberRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _committeeMemberService.UpdateAsync(id, request, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/groups/{groupId:guid}/committee-members/{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid groupId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        await _committeeMemberService.DeleteAsync(id, actingMemberId, cancellationToken);
        return NoContent();
    }
}

public record CreateCommitteeMemberBody(Guid MemberId, CommitteeRole Role);
