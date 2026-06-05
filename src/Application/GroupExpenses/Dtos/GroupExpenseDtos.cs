using MySociety.Domain.Enums;

namespace MySociety.Application.GroupExpenses.Dtos;

public record CreateGroupExpenseRequest(
    Guid GroupId,
    decimal Amount,
    string Description,
    DateTime ExpenseDate,
    GroupFundType FundType = GroupFundType.Maintenance);

public record GroupExpenseResponse(
    Guid Id,
    Guid GroupId,
    Guid CreatedByMemberId,
    string CreatedByName,
    decimal Amount,
    string Description,
    DateTime ExpenseDate,
    GroupFundType FundType,
    DateTime CreatedAt);
