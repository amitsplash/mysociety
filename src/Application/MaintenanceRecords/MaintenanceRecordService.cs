using FluentValidation;
using MySociety.Application.Common;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.MaintenanceRecords.Dtos;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.MaintenanceRecords;

public interface IMaintenanceRecordService
{
    Task<MaintenanceRecordResponse> CreateAsync(
        CreateMaintenanceRecordRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MaintenanceRecordResponse>> GetByAssetIdAsync(
        Guid groupId,
        Guid assetId,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public class MaintenanceRecordService : IMaintenanceRecordService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IMaintenanceRecordRepository _maintenanceRecordRepository;
    private readonly IValidator<CreateMaintenanceRecordRequest> _createValidator;

    public MaintenanceRecordService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        IAssetRepository assetRepository,
        IMaintenanceRecordRepository maintenanceRecordRepository,
        IValidator<CreateMaintenanceRecordRequest> createValidator)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _assetRepository = assetRepository;
        _maintenanceRecordRepository = maintenanceRecordRepository;
        _createValidator = createValidator;
    }

    public async Task<MaintenanceRecordResponse> CreateAsync(
        CreateMaintenanceRecordRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);

        _ = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, request.GroupId, cancellationToken);

        var asset = await _assetRepository.GetByIdForGroupAsync(request.AssetId, request.GroupId, cancellationToken)
            ?? throw new NotFoundException("Asset not found.");

        if (asset.Status == AssetStatus.Decommissioned)
        {
            throw new ValidationException(["Cannot log maintenance for a decommissioned asset."]);
        }

        var performedDate = ExpenseDateRules.NormalizeToUtcDate(request.PerformedDate);
        var now = DateTime.UtcNow;

        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(),
            AssetId = asset.Id,
            GroupId = request.GroupId,
            CreatedByMemberId = actingMemberId,
            PerformedDate = performedDate,
            Description = request.Description.Trim(),
            Cost = request.Cost,
            VendorName = NormalizeOptional(request.VendorName),
            Notes = NormalizeOptional(request.Notes),
            CreatedAt = now,
        };

        asset.NextDueDate = MaintenanceScheduleRules.ComputeNextDueAfterMaintenance(
            performedDate,
            asset.MaintenanceIntervalDays);
        asset.LastAlertedForDueDate = null;

        await _maintenanceRecordRepository.AddAsync(record, cancellationToken);
        await _maintenanceRecordRepository.SaveChangesAsync(cancellationToken);

        var created = await _maintenanceRecordRepository.GetByIdAsync(record.Id, cancellationToken)
            ?? throw new NotFoundException("Maintenance record not found after creation.");

        return MapRecord(created);
    }

    public async Task<IReadOnlyList<MaintenanceRecordResponse>> GetByAssetIdAsync(
        Guid groupId,
        Guid assetId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        _ = await _assetRepository.GetByIdForGroupAsync(assetId, groupId, cancellationToken)
            ?? throw new NotFoundException("Asset not found.");

        var records = await _maintenanceRecordRepository.GetByAssetIdAsync(assetId, cancellationToken);
        return records.Select(MapRecord).ToList();
    }

    private static MaintenanceRecordResponse MapRecord(MaintenanceRecord record)
    {
        return new MaintenanceRecordResponse(
            record.Id,
            record.AssetId,
            record.GroupId,
            record.CreatedByMemberId,
            record.CreatedByMember.User.Name,
            record.PerformedDate,
            record.Description,
            record.Cost,
            record.VendorName,
            record.Notes,
            record.CreatedAt);
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static async Task ValidateAsync<T>(
        IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors.Select(x => x.ErrorMessage));
        }
    }
}
