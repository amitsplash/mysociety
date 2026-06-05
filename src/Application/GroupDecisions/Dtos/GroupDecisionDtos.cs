using MySociety.Domain.Enums;

namespace MySociety.Application.GroupDecisions.Dtos;

public record GroupDecisionResponse(
    Guid Id,
    GroupDecisionSource Source,
    string DecisionText,
    string? ResolutionNumber,
    Guid? ResolutionId,
    Guid MeetingId,
    string MeetingTitle,
    MeetingStatus MeetingStatus,
    DateTime MeetingDate,
    bool IsDraft,
    Guid? AgendaItemId,
    string? TopicTitle,
    Guid? OpenMatterId,
    decimal? ApprovedBudget,
    MeetingItemOutcome? Outcome,
    ResolutionStatus? ResolutionStatus,
    DateTime DecidedAt);

public enum GroupDecisionFilter
{
    All = 0,
    HasBudget = 1
}
