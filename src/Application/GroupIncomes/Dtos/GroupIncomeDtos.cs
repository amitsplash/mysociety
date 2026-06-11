namespace MySociety.Application.GroupIncomes.Dtos;

public record CreateGroupIncomeRequest(
    Guid GroupId,
    decimal Amount,
    string Description,
    DateTime IncomeDate);

public record GroupIncomeResponse(
    Guid Id,
    Guid GroupId,
    Guid CreatedByMemberId,
    string CreatedByName,
    decimal Amount,
    string Description,
    DateTime IncomeDate,
    DateTime CreatedAt);
