using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Resolutions;
using MySociety.Application.Resolutions.Dtos;
using MySociety.Domain.Enums;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class ResolutionsController : ControllerBase
{
    private readonly IResolutionService _resolutionService;
    private readonly IMemberRepository _memberRepository;

    public ResolutionsController(IResolutionService resolutionService, IMemberRepository memberRepository)
    {
        _resolutionService = resolutionService;
        _memberRepository = memberRepository;
    }

    [HttpGet("api/groups/{groupId:guid}/resolutions")]
    public async Task<ActionResult<IReadOnlyList<ResolutionResponse>>> GetByGroupId(
        Guid groupId,
        [FromQuery] ResolutionStatus? status,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        return Ok(await _resolutionService.GetByGroupIdAsync(groupId, actingMemberId, status, cancellationToken));
    }

    [HttpGet("api/groups/{groupId:guid}/resolutions/{id:guid}")]
    public async Task<ActionResult<ResolutionResponse>> GetById(
        Guid groupId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        return Ok(await _resolutionService.GetByIdAsync(groupId, id, actingMemberId, cancellationToken));
    }

    [HttpPost("api/groups/{groupId:guid}/resolutions")]
    public async Task<ActionResult<ResolutionResponse>> Create(
        Guid groupId,
        [FromBody] CreateResolutionBody body,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var request = new CreateResolutionRequest(
            groupId,
            body.MeetingId,
            body.Title,
            body.Description,
            body.AgendaItemId,
            body.OpenMatterId,
            body.ResolutionDate,
            body.ApprovedBudget,
            body.Status);
        var result = await _resolutionService.CreateAsync(request, actingMemberId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { groupId, id = result.Id }, result);
    }

    [HttpPut("api/groups/{groupId:guid}/resolutions/{id:guid}")]
    public async Task<ActionResult<ResolutionResponse>> Update(
        Guid groupId,
        Guid id,
        [FromBody] UpdateResolutionRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        return Ok(await _resolutionService.UpdateAsync(groupId, id, request, actingMemberId, cancellationToken));
    }
}

public record CreateResolutionBody(
    Guid MeetingId,
    string Title,
    string? Description = null,
    Guid? AgendaItemId = null,
    Guid? OpenMatterId = null,
    DateTime? ResolutionDate = null,
    decimal? ApprovedBudget = null,
    ResolutionStatus Status = ResolutionStatus.Open);
