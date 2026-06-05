using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Agenda;
using MySociety.Application.Agenda.Dtos;
using MySociety.Application.Common.Interfaces;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class AgendaController : ControllerBase
{
    private readonly IAgendaService _agendaService;
    private readonly IMemberRepository _memberRepository;

    public AgendaController(IAgendaService agendaService, IMemberRepository memberRepository)
    {
        _agendaService = agendaService;
        _memberRepository = memberRepository;
    }

    [HttpPost("api/groups/{groupId:guid}/meetings/{meetingId:guid}/agenda")]
    public async Task<ActionResult<AgendaItemResponse>> Add(
        Guid groupId,
        Guid meetingId,
        [FromBody] CreateAgendaItemRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        return Ok(await _agendaService.AddAsync(groupId, meetingId, request, actingMemberId, cancellationToken));
    }

    [HttpPost("api/groups/{groupId:guid}/meetings/{meetingId:guid}/agenda/from-open-matter/{openMatterId:guid}")]
    public async Task<ActionResult<AgendaItemResponse>> AddFromOpenMatter(
        Guid groupId,
        Guid meetingId,
        Guid openMatterId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        return Ok(await _agendaService.AddFromOpenMatterAsync(
            groupId, meetingId, openMatterId, actingMemberId, cancellationToken));
    }

    [HttpPut("api/groups/{groupId:guid}/agenda/{agendaItemId:guid}")]
    public async Task<ActionResult<AgendaItemResponse>> Update(
        Guid groupId,
        Guid agendaItemId,
        [FromBody] UpdateAgendaItemRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        return Ok(await _agendaService.UpdateAsync(groupId, agendaItemId, request, actingMemberId, cancellationToken));
    }

    [HttpPatch("api/groups/{groupId:guid}/agenda/{agendaItemId:guid}/outcome")]
    public async Task<ActionResult<AgendaItemResponse>> UpdateOutcome(
        Guid groupId,
        Guid agendaItemId,
        [FromBody] UpdateAgendaOutcomeRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        return Ok(await _agendaService.UpdateOutcomeAsync(
            groupId, agendaItemId, request, actingMemberId, cancellationToken));
    }

    [HttpDelete("api/groups/{groupId:guid}/agenda/{agendaItemId:guid}")]
    public async Task<IActionResult> Delete(
        Guid groupId,
        Guid agendaItemId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        await _agendaService.DeleteAsync(groupId, agendaItemId, actingMemberId, cancellationToken);
        return NoContent();
    }
}
