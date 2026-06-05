using MySociety.Application.Agenda.Dtos;
using MySociety.Application.Resolutions.Dtos;
using MySociety.Application.GroupDecisions.Dtos;
using MySociety.Domain.Enums;

namespace MySociety.Application.Meetings.Dtos;

public record CreateMeetingRequest(
    Guid GroupId,
    string Title,
    DateTime MeetingDate,
    MeetingType MeetingType = MeetingType.Regular,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    string? Location = null,
    string? Summary = null,
    MeetingStatus Status = MeetingStatus.Draft);

public record UpdateMeetingRequest(
    string? Title = null,
    DateTime? MeetingDate = null,
    MeetingType? MeetingType = null,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    string? Location = null,
    string? Summary = null);

public record UpdateMeetingStatusRequest(MeetingStatus Status);

public record SetMeetingAttendeesRequest(IReadOnlyList<Guid> MemberIds);

public record MeetingSummaryResponse(
    Guid Id,
    Guid GroupId,
    string Title,
    MeetingType MeetingType,
    DateTime MeetingDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Location,
    string? Summary,
    MeetingStatus Status,
    Guid CreatedByMemberId,
    string CreatedByName,
    int AgendaItemCount,
    DateTime CreatedAt);

public record MeetingDetailResponse(
    Guid Id,
    Guid GroupId,
    string Title,
    MeetingType MeetingType,
    DateTime MeetingDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Location,
    string? Summary,
    MeetingStatus Status,
    Guid CreatedByMemberId,
    string CreatedByName,
    IReadOnlyList<AgendaItemResponse> AgendaItems,
    IReadOnlyList<MeetingAttendeeResponse> Attendees,
    IReadOnlyList<ResolutionResponse> Resolutions,
    IReadOnlyList<GroupDecisionResponse> Decisions,
    DateTime CreatedAt);

public record MeetingAttendeeResponse(
    Guid Id,
    Guid MemberId,
    string MemberName,
    AttendanceStatus AttendanceStatus);
