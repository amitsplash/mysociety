using FluentValidation;
using MySociety.Application.Assets.Dtos;
using MySociety.Application.Common;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.Assets;

public interface IAssetService
{
    Task<AssetResponse> CreateAsync(
        CreateAssetRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<AssetResponse> UpdateAsync(
        Guid assetId,
        UpdateAssetRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<AssetResponse> DecommissionAsync(
        Guid assetId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<AssetResponse> GetByIdAsync(
        Guid groupId,
        Guid assetId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AssetResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<AssetMaintenanceSummaryResponse> GetMaintenanceSummaryAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public class AssetService : IAssetService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IMaintenanceRecordRepository _maintenanceRecordRepository;
    private readonly IValidator<CreateAssetRequest> _createValidator;
    private readonly IValidator<UpdateAssetRequest> _updateValidator;

    public AssetService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        IAssetRepository assetRepository,
        IMaintenanceRecordRepository maintenanceRecordRepository,
        IValidator<CreateAssetRequest> createValidator,
        IValidator<UpdateAssetRequest> updateValidator)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _assetRepository = assetRepository;
        _maintenanceRecordRepository = maintenanceRecordRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<AssetResponse> CreateAsync(
        CreateAssetRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);

        _ = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, request.GroupId, cancellationToken);

        var now = DateTime.UtcNow;
        var installDate = request.InstallDate.HasValue
            ? ExpenseDateRules.NormalizeToUtcDate(request.InstallDate.Value)
            : (DateTime?)null;

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            CreatedByMemberId = actingMemberId,
            Name = request.Name.Trim(),
            Category = request.Category,
            Location = NormalizeOptional(request.Location),
            Description = NormalizeOptional(request.Description),
            SerialNumber = NormalizeOptional(request.SerialNumber),
            VendorName = NormalizeOptional(request.VendorName),
            InstallDate = installDate,
            Status = request.Status,
            MaintenanceIntervalDays = request.MaintenanceIntervalDays,
            AlertLeadDays = request.AlertLeadDays,
            NextDueDate = MaintenanceScheduleRules.ComputeInitialNextDueDate(
                installDate,
                request.MaintenanceIntervalDays,
                now),
            CreatedAt = now,
        };

        await _assetRepository.AddAsync(asset, cancellationToken);
        await _assetRepository.SaveChangesAsync(cancellationToken);

        var created = await _assetRepository.GetByIdAsync(asset.Id, cancellationToken)
            ?? throw new NotFoundException("Asset not found after creation.");

        return MapAsset(created, now.Date);
    }

    public async Task<AssetResponse> UpdateAsync(
        Guid assetId,
        UpdateAssetRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);

        var asset = await _assetRepository.GetByIdAsync(assetId, cancellationToken)
            ?? throw new NotFoundException("Asset not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, asset.GroupId, cancellationToken);

        var now = DateTime.UtcNow;
        var installDate = request.InstallDate.HasValue
            ? ExpenseDateRules.NormalizeToUtcDate(request.InstallDate.Value)
            : (DateTime?)null;

        var hasRecords = await _maintenanceRecordRepository.HasRecordsForAssetAsync(asset.Id, cancellationToken);

        asset.Name = request.Name.Trim();
        asset.Category = request.Category;
        asset.Location = NormalizeOptional(request.Location);
        asset.Description = NormalizeOptional(request.Description);
        asset.SerialNumber = NormalizeOptional(request.SerialNumber);
        asset.VendorName = NormalizeOptional(request.VendorName);
        asset.InstallDate = installDate;
        asset.Status = request.Status;
        asset.MaintenanceIntervalDays = request.MaintenanceIntervalDays;
        asset.AlertLeadDays = request.AlertLeadDays;

        if (!hasRecords)
        {
            asset.NextDueDate = MaintenanceScheduleRules.ComputeInitialNextDueDate(
                installDate,
                request.MaintenanceIntervalDays,
                now);
        }
        else if (request.Status != AssetStatus.Active)
        {
            asset.NextDueDate = null;
        }
        else
        {
            var latest = await _maintenanceRecordRepository.GetLatestForAssetAsync(asset.Id, cancellationToken);
            if (latest is not null)
            {
                asset.NextDueDate = MaintenanceScheduleRules.ComputeNextDueAfterMaintenance(
                    latest.PerformedDate,
                    request.MaintenanceIntervalDays);
            }
        }

        await _assetRepository.SaveChangesAsync(cancellationToken);

        var updated = await _assetRepository.GetByIdAsync(asset.Id, cancellationToken)
            ?? throw new NotFoundException("Asset not found after update.");

        return MapAsset(updated, now.Date);
    }

    public async Task<AssetResponse> DecommissionAsync(
        Guid assetId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var asset = await _assetRepository.GetByIdAsync(assetId, cancellationToken)
            ?? throw new NotFoundException("Asset not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, asset.GroupId, cancellationToken);

        asset.Status = AssetStatus.Decommissioned;
        asset.NextDueDate = null;

        await _assetRepository.SaveChangesAsync(cancellationToken);

        var updated = await _assetRepository.GetByIdAsync(asset.Id, cancellationToken)
            ?? throw new NotFoundException("Asset not found after decommission.");

        return MapAsset(updated, DateTime.UtcNow.Date);
    }

    public async Task<AssetResponse> GetByIdAsync(
        Guid groupId,
        Guid assetId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        var asset = await _assetRepository.GetByIdForGroupAsync(assetId, groupId, cancellationToken)
            ?? throw new NotFoundException("Asset not found.");

        return MapAsset(asset, DateTime.UtcNow.Date);
    }

    public async Task<IReadOnlyList<AssetResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        var assets = await _assetRepository.GetByGroupIdAsync(groupId, cancellationToken);
        var today = DateTime.UtcNow.Date;
        return assets.Select(a => MapAsset(a, today)).ToList();
    }

    public async Task<AssetMaintenanceSummaryResponse> GetMaintenanceSummaryAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        var assets = await _assetRepository.GetByGroupIdAsync(groupId, cancellationToken);
        var today = DateTime.UtcNow.Date;
        var activeAssets = assets.Where(a => a.Status == AssetStatus.Active).ToList();

        var dueSoon = activeAssets.Count(a =>
            MaintenanceScheduleRules.ComputeMaintenanceStatus(a, today) == AssetMaintenanceStatus.DueSoon);
        var overdue = activeAssets.Count(a =>
            MaintenanceScheduleRules.ComputeMaintenanceStatus(a, today) == AssetMaintenanceStatus.Overdue);

        return new AssetMaintenanceSummaryResponse(dueSoon, overdue, activeAssets.Count);
    }

    private static AssetResponse MapAsset(Asset asset, DateTime utcToday)
    {
        return new AssetResponse(
            asset.Id,
            asset.GroupId,
            asset.CreatedByMemberId,
            asset.CreatedByMember.User.Name,
            asset.Name,
            asset.Category,
            asset.Location,
            asset.Description,
            asset.SerialNumber,
            asset.VendorName,
            asset.InstallDate,
            asset.Status,
            asset.MaintenanceIntervalDays,
            asset.AlertLeadDays,
            asset.NextDueDate,
            MaintenanceScheduleRules.ComputeMaintenanceStatus(asset, utcToday),
            asset.CreatedAt);
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
