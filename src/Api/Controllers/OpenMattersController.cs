using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.OpenMatters;
using MySociety.Application.OpenMatters.Dtos;
using MySociety.Domain.Enums;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class OpenMattersController : ControllerBase
{
    private readonly IOpenMatterService _openMatterService;
    private readonly IMemberRepository _memberRepository;

    public OpenMattersController(IOpenMatterService openMatterService, IMemberRepository memberRepository)
    {
        _openMatterService = openMatterService;
        _memberRepository = memberRepository;
    }

    [HttpGet("api/groups/{groupId:guid}/open-matters/summary")]
    public async Task<ActionResult<OpenMatterSummaryResponse>> GetSummary(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        return Ok(await _openMatterService.GetSummaryAsync(groupId, actingMemberId, cancellationToken));
    }

    [HttpGet("api/groups/{groupId:guid}/open-matters")]
    public async Task<ActionResult<IReadOnlyList<OpenMatterResponse>>> GetByGroupId(
        Guid groupId,
        [FromQuery] OpenMatterStatus? status,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        return Ok(await _openMatterService.GetByGroupIdAsync(groupId, actingMemberId, status, cancellationToken));
    }

    [HttpPost("api/groups/{groupId:guid}/open-matters")]
    public async Task<ActionResult<OpenMatterResponse>> Create(
        Guid groupId,
        [FromBody] CreateOpenMatterBody body,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var request = new CreateOpenMatterRequest(groupId, body.Title, body.Description);
        var result = await _openMatterService.CreateAsync(request, actingMemberId, cancellationToken);
        return CreatedAtAction(nameof(GetByGroupId), new { groupId }, result);
    }

    [HttpPut("api/groups/{groupId:guid}/open-matters/{id:guid}")]
    public async Task<ActionResult<OpenMatterResponse>> Update(
        Guid groupId,
        Guid id,
        [FromBody] UpdateOpenMatterRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        return Ok(await _openMatterService.UpdateAsync(id, request, actingMemberId, cancellationToken));
    }

    [HttpPost("api/groups/{groupId:guid}/open-matters/from-agenda/{agendaItemId:guid}")]
    public async Task<ActionResult<OpenMatterResponse>> PromoteFromAgenda(
        Guid groupId,
        Guid agendaItemId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        return Ok(await _openMatterService.PromoteFromAgendaAsync(groupId, agendaItemId, actingMemberId, cancellationToken));
    }
}

public record CreateOpenMatterBody(string Title, string? Description);
