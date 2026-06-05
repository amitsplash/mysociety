using FluentValidation;
using MySociety.Application.Agenda;
using MySociety.Application.Agenda.Dtos;
using MySociety.Application.Resolutions;
using MySociety.Application.Resolutions.Dtos;
using MySociety.Application.GroupDecisions;
using MySociety.Application.GroupDecisions.Dtos;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Meetings.Dtos;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.Meetings;

public interface IMeetingService
{
    Task<IReadOnlyList<MeetingSummaryResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        MeetingStatus? statusFilter,
        CancellationToken cancellationToken);

    Task<MeetingDetailResponse> GetByIdAsync(
        Guid groupId,
        Guid meetingId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<MeetingDetailResponse> CreateAsync(
        CreateMeetingRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<MeetingDetailResponse> UpdateAsync(
        Guid groupId,
        Guid meetingId,
        UpdateMeetingRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<MeetingDetailResponse> UpdateStatusAsync(
        Guid groupId,
        Guid meetingId,
        UpdateMeetingStatusRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<MeetingDetailResponse> SetAttendeesAsync(
        Guid groupId,
        Guid meetingId,
        SetMeetingAttendeesRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid groupId,
        Guid meetingId,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public class MeetingService : IMeetingService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ICommitteeMemberRepository _committeeMemberRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMeetingAttendeeRepository _meetingAttendeeRepository;
    private readonly IGroupDecisionService _societyDecisionService;
    private readonly IValidator<CreateMeetingRequest> _createValidator;
    private readonly IValidator<UpdateMeetingRequest> _updateValidator;
    private readonly IValidator<UpdateMeetingStatusRequest> _statusValidator;

    public MeetingService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        ICommitteeMemberRepository committeeMemberRepository,
        IMeetingRepository meetingRepository,
        IMeetingAttendeeRepository meetingAttendeeRepository,
        IGroupDecisionService societyDecisionService,
        IValidator<CreateMeetingRequest> createValidator,
        IValidator<UpdateMeetingRequest> updateValidator,
        IValidator<UpdateMeetingStatusRequest> statusValidator)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _committeeMemberRepository = committeeMemberRepository;
        _meetingRepository = meetingRepository;
        _meetingAttendeeRepository = meetingAttendeeRepository;
        _societyDecisionService = societyDecisionService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    public async Task<IReadOnlyList<MeetingSummaryResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        MeetingStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        var canManage = await MeetingVisibility.CanManageMeetingsAsync(
            _memberRepository, _committeeMemberRepository, actingMemberId, groupId, cancellationToken);

        var meetings = await _meetingRepository.GetByGroupIdAsync(groupId, statusFilter, cancellationToken);
        var visible = MeetingVisibility.FilterForViewer(meetings, canManage);
        return visible.Select(MapToSummary).ToList();
    }

    public async Task<MeetingDetailResponse> GetByIdAsync(
        Guid groupId,
        Guid meetingId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await EnsureGroupMemberForGroupAsync(groupId, actingMemberId, cancellationToken);

        var meeting = await _meetingRepository.GetByIdWithDetailsAsync(meetingId, cancellationToken)
            ?? throw new NotFoundException("Meeting not found.");

        if (meeting.GroupId != groupId)
        {
            throw new NotFoundException("Meeting not found.");
        }

        var canManage = await MeetingVisibility.CanManageMeetingsAsync(
            _memberRepository, _committeeMemberRepository, actingMemberId, groupId, cancellationToken);

        if (!MeetingVisibility.CanMemberView(meeting, canManage))
        {
            throw new ForbiddenException("This meeting is not published yet.");
        }

        var decisions = await _societyDecisionService.GetByMeetingIdAsync(
            groupId, meetingId, actingMemberId, cancellationToken);

        return MapToDetail(meeting, decisions);
    }

    public async Task<MeetingDetailResponse> CreateAsync(
        CreateMeetingRequest request,
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

        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            Title = request.Title.Trim(),
            MeetingType = request.MeetingType,
            MeetingDate = request.MeetingDate.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
            Summary = string.IsNullOrWhiteSpace(request.Summary) ? null : request.Summary.Trim(),
            Status = request.Status,
            CreatedByMemberId = actingMemberId,
            CreatedAt = DateTime.UtcNow,
        };

        await _meetingRepository.AddAsync(meeting, cancellationToken);
        await _meetingRepository.SaveChangesAsync(cancellationToken);

        var saved = await _meetingRepository.GetByIdWithDetailsAsync(meeting.Id, cancellationToken)
            ?? throw new NotFoundException("Meeting not found.");

        return MapToDetail(saved, []);
    }

    public async Task<MeetingDetailResponse> UpdateAsync(
        Guid groupId,
        Guid meetingId,
        UpdateMeetingRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);
        var meeting = await GetEditableMeetingAsync(groupId, meetingId, actingMemberId, cancellationToken);

        if (request.Title is not null)
        {
            meeting.Title = request.Title.Trim();
        }

        if (request.MeetingDate.HasValue)
        {
            meeting.MeetingDate = request.MeetingDate.Value.Date;
        }

        if (request.MeetingType.HasValue)
        {
            meeting.MeetingType = request.MeetingType.Value;
        }

        if (request.StartTime.HasValue)
        {
            meeting.StartTime = request.StartTime;
        }

        if (request.EndTime.HasValue)
        {
            meeting.EndTime = request.EndTime;
        }

        if (request.Location is not null)
        {
            meeting.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
        }

        if (request.Summary is not null)
        {
            meeting.Summary = string.IsNullOrWhiteSpace(request.Summary) ? null : request.Summary.Trim();
        }

        await _meetingRepository.SaveChangesAsync(cancellationToken);

        var updated = await _meetingRepository.GetByIdWithDetailsAsync(meeting.Id, cancellationToken)
            ?? throw new NotFoundException("Meeting not found.");

        return MapToDetail(updated, []);
    }

    public async Task<MeetingDetailResponse> UpdateStatusAsync(
        Guid groupId,
        Guid meetingId,
        UpdateMeetingStatusRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_statusValidator, request, cancellationToken);
        var meeting = await GetEditableMeetingAsync(groupId, meetingId, actingMemberId, cancellationToken);
        meeting.Status = request.Status;
        await _meetingRepository.SaveChangesAsync(cancellationToken);

        var updated = await _meetingRepository.GetByIdWithDetailsAsync(meeting.Id, cancellationToken)
            ?? throw new NotFoundException("Meeting not found.");

        return MapToDetail(updated, []);
    }

    public async Task<MeetingDetailResponse> SetAttendeesAsync(
        Guid groupId,
        Guid meetingId,
        SetMeetingAttendeesRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var meeting = await GetEditableMeetingAsync(groupId, meetingId, actingMemberId, cancellationToken);

        var attendees = new List<MeetingAttendee>();
        foreach (var memberId in request.MemberIds.Distinct())
        {
            var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken)
                ?? throw new ValidationException($"Member {memberId} not found.");

            if (member.GroupId != groupId)
            {
                throw new ValidationException("All attendees must be group members.");
            }

            attendees.Add(new MeetingAttendee
            {
                Id = Guid.NewGuid(),
                MeetingId = meetingId,
                MemberId = memberId,
                AttendanceStatus = AttendanceStatus.Present,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _meetingAttendeeRepository.ReplaceForMeetingAsync(meetingId, attendees, cancellationToken);
        await _meetingAttendeeRepository.SaveChangesAsync(cancellationToken);

        var updated = await _meetingRepository.GetByIdWithDetailsAsync(meeting.Id, cancellationToken)
            ?? throw new NotFoundException("Meeting not found.");

        return MapToDetail(updated, []);
    }

    public async Task DeleteAsync(
        Guid groupId,
        Guid meetingId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await EnsureCanManageForGroupAsync(groupId, actingMemberId, cancellationToken);

        var meeting = await GetMeetingInGroupAsync(groupId, meetingId, cancellationToken);
        await _meetingRepository.RemoveAsync(meeting, cancellationToken);
        await _meetingRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Meeting> GetEditableMeetingAsync(
        Guid groupId,
        Guid meetingId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await EnsureCanManageForGroupAsync(groupId, actingMemberId, cancellationToken);

        var meeting = await GetMeetingInGroupAsync(groupId, meetingId, cancellationToken);

        if (meeting.Status is MeetingStatus.Published or MeetingStatus.Archived)
        {
            throw new ConflictException("Cannot edit a published or archived meeting.");
        }

        return meeting;
    }

    private async Task EnsureGroupMemberForGroupAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);
    }

    private async Task EnsureCanManageForGroupAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureCanManageMeetingsAsync(
            _memberRepository,
            _committeeMemberRepository,
            actingMemberId,
            groupId,
            cancellationToken);
    }

    private async Task<Meeting> GetMeetingInGroupAsync(
        Guid groupId,
        Guid meetingId,
        CancellationToken cancellationToken)
    {
        var meeting = await _meetingRepository.GetByIdAsync(meetingId, cancellationToken)
            ?? throw new NotFoundException("Meeting not found.");

        if (meeting.GroupId != groupId)
        {
            throw new NotFoundException("Meeting not found.");
        }

        return meeting;
    }

    private static MeetingSummaryResponse MapToSummary(Meeting meeting)
    {
        var createdByName = meeting.CreatedByMember?.User?.Name ?? string.Empty;
        return new MeetingSummaryResponse(
            meeting.Id,
            meeting.GroupId,
            meeting.Title,
            meeting.MeetingType,
            meeting.MeetingDate,
            meeting.StartTime,
            meeting.EndTime,
            meeting.Location,
            meeting.Summary,
            meeting.Status,
            meeting.CreatedByMemberId,
            createdByName,
            meeting.AgendaItems.Count,
            meeting.CreatedAt);
    }

    private static MeetingDetailResponse MapToDetail(
        Meeting meeting,
        IReadOnlyList<GroupDecisionResponse> decisions)
    {
        var createdByName = meeting.CreatedByMember?.User?.Name ?? string.Empty;
        var agenda = meeting.AgendaItems
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.AgendaNumber)
            .Select(AgendaMapping.MapToResponse)
            .ToList();

        var resolutions = meeting.Resolutions
            .OrderByDescending(x => x.ResolutionDate)
            .Select(r => MapResolution(r, meeting))
            .ToList();

        var attendees = meeting.Attendees
            .Select(x => new MeetingAttendeeResponse(
                x.Id,
                x.MemberId,
                x.Member.User.Name,
                x.AttendanceStatus))
            .ToList();

        return new MeetingDetailResponse(
            meeting.Id,
            meeting.GroupId,
            meeting.Title,
            meeting.MeetingType,
            meeting.MeetingDate,
            meeting.StartTime,
            meeting.EndTime,
            meeting.Location,
            meeting.Summary,
            meeting.Status,
            meeting.CreatedByMemberId,
            createdByName,
            agenda,
            attendees,
            resolutions,
            decisions,
            meeting.CreatedAt);
    }

    private static ResolutionResponse MapResolution(Resolution resolution, Meeting meeting)
    {
        var createdByName = resolution.CreatedByMember?.User?.Name ?? string.Empty;
        return new ResolutionResponse(
            resolution.Id,
            resolution.GroupId,
            resolution.MeetingId,
            meeting.Title,
            meeting.Status,
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
