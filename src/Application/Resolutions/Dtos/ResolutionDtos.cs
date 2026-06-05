using MySociety.Domain.Enums;

namespace MySociety.Application.Resolutions.Dtos;

public record CreateResolutionRequest(
    Guid GroupId,
    Guid MeetingId,
    string Title,
    string? Description = null,
    Guid? AgendaItemId = null,
    Guid? OpenMatterId = null,
    DateTime? ResolutionDate = null,
    decimal? ApprovedBudget = null,
    ResolutionStatus Status = ResolutionStatus.Open);

public record UpdateResolutionRequest(
    string? Title = null,
    string? Description = null,
    DateTime? ResolutionDate = null,
    decimal? ApprovedBudget = null,
    ResolutionStatus? Status = null);

public record ResolutionResponse(
    Guid Id,
    Guid GroupId,
    Guid MeetingId,
    string MeetingTitle,
    MeetingStatus MeetingStatus,
    Guid? AgendaItemId,
    Guid? OpenMatterId,
    string ResolutionNumber,
    string Title,
    string? Description,
    DateTime ResolutionDate,
    decimal? ApprovedBudget,
    ResolutionStatus Status,
    Guid CreatedByMemberId,
    string CreatedByName,
    DateTime CreatedAt);
