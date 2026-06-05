using FluentValidation;
using MySociety.Application.Common;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Expenses.Dtos;
using MySociety.Application.Financial;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.Expenses;

public interface IExpenseService
{
    Task<ExpenseResponse> CreateAsync(
        CreateExpenseRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<ExpenseResponse> ApproveAsync(
        Guid expenseId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<ExpenseResponse> RejectAsync(
        Guid expenseId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExpenseResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public class ExpenseService : IExpenseService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly ILedgerService _ledgerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateExpenseRequest> _createValidator;

    public ExpenseService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        IExpenseRepository expenseRepository,
        ILedgerService ledgerService,
        IUnitOfWork unitOfWork,
        IValidator<CreateExpenseRequest> createValidator)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _expenseRepository = expenseRepository;
        _ledgerService = ledgerService;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
    }

    public async Task<ExpenseResponse> CreateAsync(
        CreateExpenseRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);

        _ = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(
            _memberRepository, actingMemberId, request.GroupId, cancellationToken);

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            CreatedByMemberId = actingMemberId,
            Amount = request.Amount,
            Description = request.Description.Trim(),
            ExpenseDate = ExpenseDateRules.NormalizeToUtcDate(request.ExpenseDate),
            Status = ExpenseStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _expenseRepository.AddAsync(expense, cancellationToken);
        await _expenseRepository.SaveChangesAsync(cancellationToken);

        var created = await _expenseRepository.GetByIdAsync(expense.Id, cancellationToken)
            ?? throw new NotFoundException("Expense not found after creation.");

        return MapExpense(created);
    }

    public async Task<ExpenseResponse> ApproveAsync(
        Guid expenseId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new NotFoundException("Expense not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, expense.GroupId, cancellationToken);

        EnsurePending(expense);

        ExpenseResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            expense.Status = ExpenseStatus.Approved;
            expense.ApprovedByMemberId = actingMemberId;

            await _expenseRepository.SaveChangesAsync(ct);

            await _ledgerService.RecordExpenseCreditAsync(
                expense.CreatedByMemberId,
                expense.GroupId,
                expense.Id,
                expense.Amount,
                ct);

            response = MapExpense(expense);
        }, cancellationToken);

        return response!;
    }

    public async Task<ExpenseResponse> RejectAsync(
        Guid expenseId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new NotFoundException("Expense not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, expense.GroupId, cancellationToken);

        EnsurePending(expense);

        expense.Status = ExpenseStatus.Rejected;
        expense.ApprovedByMemberId = actingMemberId;

        await _expenseRepository.SaveChangesAsync(cancellationToken);
        return MapExpense(expense);
    }

    public async Task<IReadOnlyList<ExpenseResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        var expenses = await _expenseRepository.GetByGroupIdAsync(groupId, cancellationToken);
        return expenses.Select(MapExpense).ToList();
    }

    private static void EnsurePending(Expense expense)
    {
        if (expense.Status == ExpenseStatus.Approved)
        {
            throw new ConflictException("Expense has already been approved.");
        }

        if (expense.Status == ExpenseStatus.Rejected)
        {
            throw new ConflictException("Expense has already been rejected.");
        }
    }

    private static ExpenseResponse MapExpense(Expense expense)
    {
        return new ExpenseResponse(
            expense.Id,
            expense.GroupId,
            expense.CreatedByMemberId,
            expense.CreatedByMember.User.Name,
            expense.Amount,
            expense.Description,
            expense.ExpenseDate,
            expense.Status,
            expense.ApprovedByMemberId,
            expense.CreatedAt);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors.Select(x => x.ErrorMessage));
        }
    }
}
