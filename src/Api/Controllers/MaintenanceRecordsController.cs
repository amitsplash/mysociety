using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Api.Extensions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.MaintenanceRecords;
using MySociety.Application.MaintenanceRecords.Dtos;

namespace MySociety.Api.Controllers;

[Authorize]
[ApiController]
public class MaintenanceRecordsController : ControllerBase
{
    private readonly IMaintenanceRecordService _maintenanceRecordService;
    private readonly IMemberRepository _memberRepository;

    public MaintenanceRecordsController(
        IMaintenanceRecordService maintenanceRecordService,
        IMemberRepository memberRepository)
    {
        _maintenanceRecordService = maintenanceRecordService;
        _memberRepository = memberRepository;
    }

    [HttpGet("api/groups/{groupId:guid}/assets/{assetId:guid}/maintenance-records")]
    public async Task<ActionResult<IReadOnlyList<MaintenanceRecordResponse>>> GetByAssetId(
        Guid groupId,
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _maintenanceRecordService.GetByAssetIdAsync(
            groupId,
            assetId,
            actingMemberId,
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/maintenance-records")]
    public async Task<ActionResult<MaintenanceRecordResponse>> Create(
        [FromBody] CreateMaintenanceRecordRequest request,
        CancellationToken cancellationToken)
    {
        var actingMemberId = await HttpContext.GetRequiredActingMemberIdAsync(_memberRepository, cancellationToken);
        var result = await _maintenanceRecordService.CreateAsync(request, actingMemberId, cancellationToken);
        return CreatedAtAction(
            nameof(GetByAssetId),
            new { groupId = result.GroupId, assetId = result.AssetId },
            result);
    }
}
