using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Members;
using MySociety.Application.Members.Dtos;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly IMemberRepository _memberRepository;

    public MembersController(IMemberService memberService, IMemberRepository memberRepository)
    {
        _memberService = memberService;
        _memberRepository = memberRepository;
    }

    [HttpPost("api/members")]
    public async Task<ActionResult<CreateMemberResponse>> Create(
        [FromBody] CreateMemberRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _memberService.CreateAsync(request, actingMemberId, cancellationToken);
        return CreatedAtAction(nameof(GetByGroupId), new { groupId = result.Member.GroupId }, result);
    }

    [HttpGet("api/groups/{groupId:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<MemberResponse>>> GetByGroupId(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _memberService.GetByGroupIdAsync(groupId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("api/members/{id:guid}")]
    public async Task<ActionResult<MemberResponse>> Update(
        Guid id,
        [FromBody] UpdateMemberRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _memberService.UpdateAsync(id, request, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/members/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        await _memberService.DeleteAsync(id, actingMemberId, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/members/{id:guid}/password-reset")]
    public async Task<ActionResult<IssuePasswordResetResponse>> IssuePasswordReset(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _memberService.IssuePasswordResetAsync(id, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/members/{id:guid}/corpus/receive")]
    public async Task<ActionResult<MarkCorpusReceivedResponse>> MarkCorpusReceived(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _memberService.MarkCorpusReceivedAsync(id, actingMemberId, cancellationToken);
        return Ok(result);
    }
}
