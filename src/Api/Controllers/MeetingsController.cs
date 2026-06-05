using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Meetings;
using MySociety.Application.Meetings.Dtos;
using MySociety.Domain.Enums;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingService _meetingService;
    private readonly IMemberRepository _memberRepository;

    public MeetingsController(IMeetingService meetingService, IMemberRepository memberRepository)
    {
        _meetingService = meetingService;
        _memberRepository = memberRepository;
    }

    [HttpGet("api/groups/{groupId:guid}/meetings")]
    public async Task<ActionResult<IReadOnlyList<MeetingSummaryResponse>>> GetByGroupId(
        Guid groupId,
        [FromQuery] MeetingStatus? status,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _meetingService.GetByGroupIdAsync(groupId, actingMemberId, status, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/groups/{groupId:guid}/meetings/{meetingId:guid}")]
    public async Task<ActionResult<MeetingDetailResponse>> GetById(
        Guid groupId,
        Guid meetingId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _meetingService.GetByIdAsync(groupId, meetingId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/groups/{groupId:guid}/meetings")]
    public async Task<ActionResult<MeetingDetailResponse>> Create(
        Guid groupId,
        [FromBody] CreateMeetingBody body,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var request = new CreateMeetingRequest(
            groupId,
            body.Title,
            body.MeetingDate,
            body.MeetingType,
            body.StartTime,
            body.EndTime,
            body.Location,
            body.Summary,
            body.Status);
        var result = await _meetingService.CreateAsync(request, actingMemberId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { groupId, meetingId = result.Id }, result);
    }

    [HttpPut("api/groups/{groupId:guid}/meetings/{meetingId:guid}")]
    public async Task<ActionResult<MeetingDetailResponse>> Update(
        Guid groupId,
        Guid meetingId,
        [FromBody] UpdateMeetingRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _meetingService.UpdateAsync(groupId, meetingId, request, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("api/groups/{groupId:guid}/meetings/{meetingId:guid}/status")]
    public async Task<ActionResult<MeetingDetailResponse>> UpdateStatus(
        Guid groupId,
        Guid meetingId,
        [FromBody] UpdateMeetingStatusRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _meetingService.UpdateStatusAsync(
            groupId, meetingId, request, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("api/groups/{groupId:guid}/meetings/{meetingId:guid}/attendees")]
    public async Task<ActionResult<MeetingDetailResponse>> SetAttendees(
        Guid groupId,
        Guid meetingId,
        [FromBody] SetMeetingAttendeesRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _meetingService.SetAttendeesAsync(
            groupId, meetingId, request, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/groups/{groupId:guid}/meetings/{meetingId:guid}")]
    public async Task<IActionResult> Delete(
        Guid groupId,
        Guid meetingId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        await _meetingService.DeleteAsync(groupId, meetingId, actingMemberId, cancellationToken);
        return NoContent();
    }
}

public record CreateMeetingBody(
    string Title,
    DateTime MeetingDate,
    MeetingType MeetingType = MeetingType.Regular,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    string? Location = null,
    string? Summary = null,
    MeetingStatus Status = MeetingStatus.Draft);
