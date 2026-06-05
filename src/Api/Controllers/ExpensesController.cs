using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Expenses;
using MySociety.Application.Expenses.Dtos;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly IMemberRepository _memberRepository;

    public ExpensesController(IExpenseService expenseService, IMemberRepository memberRepository)
    {
        _expenseService = expenseService;
        _memberRepository = memberRepository;
    }

    [HttpPost("api/expenses")]
    public async Task<ActionResult<ExpenseResponse>> Create(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _expenseService.CreateAsync(request, actingMemberId, cancellationToken);
        return CreatedAtAction(nameof(GetByGroupId), new { groupId = result.GroupId }, result);
    }

    [HttpPatch("api/expenses/{id:guid}/approve")]
    public async Task<ActionResult<ExpenseResponse>> Approve(Guid id, CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _expenseService.ApproveAsync(id, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("api/expenses/{id:guid}/reject")]
    public async Task<ActionResult<ExpenseResponse>> Reject(Guid id, CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _expenseService.RejectAsync(id, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/groups/{groupId:guid}/expenses")]
    public async Task<ActionResult<IReadOnlyList<ExpenseResponse>>> GetByGroupId(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _expenseService.GetByGroupIdAsync(groupId, actingMemberId, cancellationToken);
        return Ok(result);
    }
}
