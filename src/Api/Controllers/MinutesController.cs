using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MySociety.Api.Extensions;

using MySociety.Application.Common.Interfaces;

using MySociety.Application.Minutes;

using MySociety.Application.Agenda.Dtos;
using MySociety.Application.Minutes.Dtos;



namespace MySociety.Api.Controllers;



[Authorize]

[ApiController]

public class MinutesController : ControllerBase

{

    private readonly IMinuteService _minuteService;

    private readonly IMemberRepository _memberRepository;



    public MinutesController(IMinuteService minuteService, IMemberRepository memberRepository)

    {

        _minuteService = minuteService;

        _memberRepository = memberRepository;

    }



    [HttpPut("api/groups/{groupId:guid}/agenda/{agendaItemId:guid}/minutes")]

    public async Task<ActionResult<AgendaItemResponse>> Upsert(

        Guid groupId,

        Guid agendaItemId,

        [FromBody] UpsertMinuteRequest request,

        CancellationToken cancellationToken)

    {

        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);

        return Ok(await _minuteService.UpsertAsync(groupId, agendaItemId, request, actingMemberId, cancellationToken));

    }

}

