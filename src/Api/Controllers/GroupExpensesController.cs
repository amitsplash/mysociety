using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Financial;
using MySociety.Application.GroupExpenses;
using MySociety.Application.GroupExpenses.Dtos;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class GroupExpensesController : ControllerBase
{
    private readonly IGroupExpenseService _groupExpenseService;
    private readonly IMemberRepository _memberRepository;

    public GroupExpensesController(
        IGroupExpenseService groupExpenseService,
        IMemberRepository memberRepository)
    {
        _groupExpenseService = groupExpenseService;
        _memberRepository = memberRepository;
    }

    [HttpGet("api/groups/{groupId:guid}/group-funds")]
    public async Task<ActionResult<GroupFundsResponse>> GetFunds(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _groupExpenseService.GetFundsAsync(groupId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/groups/{groupId:guid}/group-expenses")]
    public async Task<ActionResult<IReadOnlyList<GroupExpenseResponse>>> GetByGroupId(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _groupExpenseService.GetByGroupIdAsync(groupId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/group-expenses")]
    public async Task<ActionResult<GroupExpenseResponse>> Create(
        [FromBody] CreateGroupExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _groupExpenseService.CreateAsync(request, actingMemberId, cancellationToken);
        return CreatedAtAction(nameof(GetByGroupId), new { groupId = result.GroupId }, result);
    }
}
