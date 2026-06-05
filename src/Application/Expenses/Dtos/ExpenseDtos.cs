using MySociety.Domain.Enums;

namespace MySociety.Application.Expenses.Dtos;

public record CreateExpenseRequest(
    Guid GroupId,
    decimal Amount,
    string Description,
    DateTime ExpenseDate);

public record ExpenseResponse(
    Guid Id,
    Guid GroupId,
    Guid CreatedByMemberId,
    string CreatedByName,
    decimal Amount,
    string Description,
    DateTime ExpenseDate,
    ExpenseStatus Status,
    Guid? ApprovedByMemberId,
    DateTime CreatedAt);
