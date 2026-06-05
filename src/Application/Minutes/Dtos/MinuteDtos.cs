namespace MySociety.Application.Minutes.Dtos;

public record UpsertMinuteRequest(
    string? DiscussionSummary = null,
    string? DecisionTaken = null,
    decimal? BudgetApproved = null);

public record MinuteResponse(
    Guid Id,
    Guid AgendaItemId,
    string? DiscussionSummary,
    string? DecisionTaken,
    decimal? BudgetApproved,
    DateTime CreatedAt);
