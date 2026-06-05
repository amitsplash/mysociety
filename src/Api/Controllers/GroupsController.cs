using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Groups;
using MySociety.Application.Groups.Dtos;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/groups")]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly IMemberRepository _memberRepository;

    public GroupsController(IGroupService groupService, IMemberRepository memberRepository)
    {
        _groupService = groupService;
        _memberRepository = memberRepository;
    }

    [HttpPost]
    public async Task<ActionResult<CreateGroupResponse>> Create(
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetRequiredUserId();
        var result = await _groupService.CreateAsync(userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Group.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GroupResponse>>> List(CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetRequiredUserId();
        var result = await _groupService.ListMineAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GroupResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetRequiredUserId();
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _groupService.GetByIdAsync(id, userId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GroupResponse>> Update(
        Guid id,
        [FromBody] UpdateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetRequiredUserId();
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _groupService.UpdateAsync(id, request, userId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetRequiredUserId();
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        await _groupService.DeleteAsync(id, userId, actingMemberId, cancellationToken);
        return NoContent();
    }
}
