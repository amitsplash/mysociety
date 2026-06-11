using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Assets;
using MySociety.Application.Assets.Dtos;
using MySociety.Application.Common.Interfaces;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly IMemberRepository _memberRepository;

    public AssetsController(IAssetService assetService, IMemberRepository memberRepository)
    {
        _assetService = assetService;
        _memberRepository = memberRepository;
    }

    [HttpGet("api/groups/{groupId:guid}/assets")]
    public async Task<ActionResult<IReadOnlyList<AssetResponse>>> GetByGroupId(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _assetService.GetByGroupIdAsync(groupId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/groups/{groupId:guid}/assets/maintenance-summary")]
    public async Task<ActionResult<AssetMaintenanceSummaryResponse>> GetMaintenanceSummary(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _assetService.GetMaintenanceSummaryAsync(groupId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/groups/{groupId:guid}/assets/{assetId:guid}")]
    public async Task<ActionResult<AssetResponse>> GetById(
        Guid groupId,
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _assetService.GetByIdAsync(groupId, assetId, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/assets")]
    public async Task<ActionResult<AssetResponse>> Create(
        [FromBody] CreateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _assetService.CreateAsync(request, actingMemberId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { groupId = result.GroupId, assetId = result.Id }, result);
    }

    [HttpPut("api/assets/{assetId:guid}")]
    public async Task<ActionResult<AssetResponse>> Update(
        Guid assetId,
        [FromBody] UpdateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _assetService.UpdateAsync(assetId, request, actingMemberId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/assets/{assetId:guid}")]
    public async Task<ActionResult<AssetResponse>> Decommission(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _assetService.DecommissionAsync(assetId, actingMemberId, cancellationToken);
        return Ok(result);
    }
}
