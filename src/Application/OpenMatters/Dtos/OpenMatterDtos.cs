using MySociety.Domain.Enums;

namespace MySociety.Application.OpenMatters.Dtos;

public record CreateOpenMatterRequest(
    Guid GroupId,
    string Title,
    string? Description = null);

public record UpdateOpenMatterRequest(
    string? Title = null,
    string? Description = null,
    OpenMatterStatus? Status = null);

public record OpenMatterResponse(
    Guid Id,
    Guid GroupId,
    string Title,
    string? Description,
    OpenMatterStatus Status,
    DateTime RaisedAt,
    Guid? LastDiscussedInMeetingId,
    string CreatedByName,
    DateTime CreatedAt);

public record OpenMatterSummaryResponse(
    int OpenCount,
    int FinalizedCount,
    int CancelledCount);
