using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.GroupIncomes;
using MySociety.Application.GroupIncomes.Dtos;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class GroupIncomesController : ControllerBase
{
    private readonly IGroupIncomeService _groupIncomeService;
    private readonly IMemberRepository _memberRepository;

    public GroupIncomesController(
        IGroupIncomeService groupIncomeService,
        IMemberRepository memberRepository)
    {
        _groupIncomeService = groupIncomeService;
        _memberRepository = memberRepository;
    }

    [HttpGet("api/groups/{groupId:guid}/group-incomes")]
    public async Task<ActionResult<IReadOnlyList<GroupIncomeResponse>>> GetByGroupId(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _groupIncomeService.GetByGroupIdAsync(groupId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/group-incomes")]
    public async Task<ActionResult<GroupIncomeResponse>> Create(
        [FromBody] CreateGroupIncomeRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _groupIncomeService.CreateAsync(request, actingMemberId, cancellationToken);
        return CreatedAtAction(nameof(GetByGroupId), new { groupId = result.GroupId }, result);
    }
}
