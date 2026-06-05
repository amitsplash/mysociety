using FluentValidation;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Meetings;
using MySociety.Application.Resolutions.Dtos;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.Resolutions;

public interface IResolutionService
{
    Task<IReadOnlyList<ResolutionResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        ResolutionStatus? statusFilter,
        CancellationToken cancellationToken);

    Task<ResolutionResponse> GetByIdAsync(
        Guid groupId,
        Guid id,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<ResolutionResponse> CreateAsync(
        CreateResolutionRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<ResolutionResponse> UpdateAsync(
        Guid groupId,
        Guid id,
        UpdateResolutionRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public class ResolutionService : IResolutionService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ICommitteeMemberRepository _committeeMemberRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly IAgendaItemRepository _agendaItemRepository;
    private readonly IResolutionRepository _resolutionRepository;
    private readonly IValidator<CreateResolutionRequest> _createValidator;
    private readonly IValidator<UpdateResolutionRequest> _updateValidator;

    public ResolutionService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        ICommitteeMemberRepository committeeMemberRepository,
        IMeetingRepository meetingRepository,
        IAgendaItemRepository agendaItemRepository,
        IResolutionRepository resolutionRepository,
        IValidator<CreateResolutionRequest> createValidator,
        IValidator<UpdateResolutionRequest> updateValidator)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _committeeMemberRepository = committeeMemberRepository;
        _meetingRepository = meetingRepository;
        _agendaItemRepository = agendaItemRepository;
        _resolutionRepository = resolutionRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<ResolutionResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        ResolutionStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        await EnsureGroupMemberAsync(groupId, actingMemberId, cancellationToken);

        var canManage = await MeetingVisibility.CanManageMeetingsAsync(
            _memberRepository, _committeeMemberRepository, actingMemberId, groupId, cancellationToken);

        var resolutions = await _resolutionRepository.GetByGroupIdAsync(groupId, statusFilter, cancellationToken);
        var visible = ResolutionVisibility.FilterForViewer(resolutions, canManage);
        return visible.Select(MapToResponse).ToList();
    }

    public async Task<ResolutionResponse> GetByIdAsync(
        Guid groupId,
        Guid id,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await EnsureGroupMemberAsync(groupId, actingMemberId, cancellationToken);

        var resolution = await _resolutionRepository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException("Resolution not found.");

        if (resolution.GroupId != groupId)
        {
            throw new NotFoundException("Resolution not found.");
        }

        var canManage = await MeetingVisibility.CanManageMeetingsAsync(
            _memberRepository, _committeeMemberRepository, actingMemberId, groupId, cancellationToken);

        if (!ResolutionVisibility.CanMemberView(resolution, canManage))
        {
            throw new ForbiddenException("This resolution is not visible yet.");
        }

        return MapToResponse(resolution);
    }

    public async Task<ResolutionResponse> CreateAsync(
        CreateResolutionRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);

        _ = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureCanManageMeetingsAsync(
            _memberRepository,
            _committeeMemberRepository,
            actingMemberId,
            request.GroupId,
            cancellationToken);

        var meeting = await _meetingRepository.GetByIdAsync(request.MeetingId, cancellationToken)
            ?? throw new NotFoundException("Meeting not found.");

        if (meeting.GroupId != request.GroupId)
        {
            throw new NotFoundException("Meeting not found.");
        }

        if (meeting.Status is MeetingStatus.Published or MeetingStatus.Archived)
        {
            throw new ConflictException("Cannot add resolutions to a published or archived meeting.");
        }

        Guid? agendaItemId = null;
        Guid? openMatterId = request.OpenMatterId;

        if (request.AgendaItemId.HasValue)
        {
            var agenda = await _agendaItemRepository.GetByIdAsync(request.AgendaItemId.Value, cancellationToken)
                ?? throw new ValidationException("Agenda item not found.");

            if (agenda.MeetingId != meeting.Id)
            {
                throw new ValidationException("Agenda item must belong to this meeting.");
            }

            agendaItemId = agenda.Id;
            openMatterId ??= agenda.OpenMatterId;
        }

        var resolutionDate = (request.ResolutionDate ?? meeting.MeetingDate).Date;
        var resolutionNumber = await GenerateResolutionNumberAsync(request.GroupId, resolutionDate.Year, cancellationToken);

        var resolution = new Resolution
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            MeetingId = request.MeetingId,
            AgendaItemId = agendaItemId,
            OpenMatterId = openMatterId,
            ResolutionNumber = resolutionNumber,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ResolutionDate = resolutionDate,
            ApprovedBudget = request.ApprovedBudget,
            Status = request.Status,
            CreatedByMemberId = actingMemberId,
            CreatedAt = DateTime.UtcNow,
        };

        await _resolutionRepository.AddAsync(resolution, cancellationToken);
        await _resolutionRepository.SaveChangesAsync(cancellationToken);

        var saved = await _resolutionRepository.GetByIdWithDetailsAsync(resolution.Id, cancellationToken)
            ?? throw new NotFoundException("Resolution not found.");

        return MapToResponse(saved);
    }

    public async Task<ResolutionResponse> UpdateAsync(
        Guid groupId,
        Guid id,
        UpdateResolutionRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);

        await MemberAuthorization.EnsureCanManageMeetingsAsync(
            _memberRepository,
            _committeeMemberRepository,
            actingMemberId,
            groupId,
            cancellationToken);

        var resolution = await _resolutionRepository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException("Resolution not found.");

        if (resolution.GroupId != groupId)
        {
            throw new NotFoundException("Resolution not found.");
        }

        if (resolution.Meeting.Status is MeetingStatus.Published or MeetingStatus.Archived)
        {
            throw new ConflictException("Cannot edit resolutions on a published or archived meeting.");
        }

        if (request.Title is not null)
        {
            resolution.Title = request.Title.Trim();
        }

        if (request.Description is not null)
        {
            resolution.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();
        }

        if (request.ResolutionDate.HasValue)
        {
            resolution.ResolutionDate = request.ResolutionDate.Value.Date;
        }

        if (request.ApprovedBudget.HasValue)
        {
            resolution.ApprovedBudget = request.ApprovedBudget;
        }

        if (request.Status.HasValue)
        {
            resolution.Status = request.Status.Value;
        }

        await _resolutionRepository.SaveChangesAsync(cancellationToken);

        var updated = await _resolutionRepository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException("Resolution not found.");

        return MapToResponse(updated);
    }

    private async Task<string> GenerateResolutionNumberAsync(
        Guid groupId,
        int year,
        CancellationToken cancellationToken)
    {
        var count = await _resolutionRepository.CountForGroupYearAsync(groupId, year, cancellationToken);
        return $"RES-{year}-{(count + 1):D3}";
    }

    private async Task EnsureGroupMemberAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);
    }

    private static ResolutionResponse MapToResponse(Resolution resolution)
    {
        var createdByName = resolution.CreatedByMember?.User?.Name ?? string.Empty;
        return new ResolutionResponse(
            resolution.Id,
            resolution.GroupId,
            resolution.MeetingId,
            resolution.Meeting.Title,
            resolution.Meeting.Status,
            resolution.AgendaItemId,
            resolution.OpenMatterId,
            resolution.ResolutionNumber,
            resolution.Title,
            resolution.Description,
            resolution.ResolutionDate,
            resolution.ApprovedBudget,
            resolution.Status,
            resolution.CreatedByMemberId,
            createdByName,
            resolution.CreatedAt);
    }

    private static async Task ValidateAsync<T>(
        IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors.Select(x => x.ErrorMessage).ToArray());
        }
    }
}
