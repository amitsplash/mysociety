using MySociety.Application.Minutes.Dtos;
using MySociety.Domain.Enums;

namespace MySociety.Application.Agenda.Dtos;

public record CreateAgendaItemRequest(
    string Title,
    string? Description = null,
    Guid? OpenMatterId = null,
    AgendaItemSource Source = AgendaItemSource.AdHoc);

public record UpdateAgendaItemRequest(
    string? Title = null,
    string? Description = null,
    int? DisplayOrder = null);

public record UpdateAgendaOutcomeRequest(
    MeetingItemOutcome Outcome,
    string? DiscussionSummary = null);

public record AgendaItemResponse(
    Guid Id,
    Guid MeetingId,
    Guid? OpenMatterId,
    string? OpenMatterTitle,
    int AgendaNumber,
    string Title,
    string? Description,
    int DisplayOrder,
    AgendaItemSource Source,
    MeetingItemOutcome Outcome,
    string? DiscussionSummary,
    MinuteResponse? Minute,
    DateTime CreatedAt);
