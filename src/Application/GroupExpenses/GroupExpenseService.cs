using FluentValidation;
using MySociety.Application.Common;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Financial;
using MySociety.Application.GroupExpenses.Dtos;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.GroupExpenses;

public interface IGroupExpenseService
{
    Task<GroupFundsResponse> GetFundsAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<GroupExpenseResponse> CreateAsync(
        CreateGroupExpenseRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GroupExpenseResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public class GroupExpenseService : IGroupExpenseService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IGroupExpenseRepository _groupExpenseRepository;
    private readonly ILedgerService _ledgerService;
    private readonly IValidator<CreateGroupExpenseRequest> _createValidator;

    public GroupExpenseService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        IGroupExpenseRepository groupExpenseRepository,
        ILedgerService ledgerService,
        IValidator<CreateGroupExpenseRequest> createValidator)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _groupExpenseRepository = groupExpenseRepository;
        _ledgerService = ledgerService;
        _createValidator = createValidator;
    }

    public async Task<GroupFundsResponse> GetFundsAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        return await _ledgerService.GetGroupFundsAsync(groupId, cancellationToken);
    }

    public async Task<GroupExpenseResponse> CreateAsync(
        CreateGroupExpenseRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);

        _ = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, request.GroupId, cancellationToken);

        var fundBalance = request.FundType == GroupFundType.Corpus
            ? await _ledgerService.GetCorpusFundBalanceAsync(request.GroupId, cancellationToken)
            : await _ledgerService.GetMaintenanceFundBalanceAsync(request.GroupId, cancellationToken);

        var fundLabel = request.FundType == GroupFundType.Corpus ? "corpus" : "maintenance";
        if (request.Amount > fundBalance.Balance)
        {
            throw new ValidationException(
                $"Insufficient {fundLabel} fund balance. Available: {fundBalance.Balance:0.##}");
        }

        var expense = new GroupExpense
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            CreatedByMemberId = actingMemberId,
            Amount = request.Amount,
            Description = request.Description.Trim(),
            ExpenseDate = ExpenseDateRules.NormalizeToUtcDate(request.ExpenseDate),
            FundType = request.FundType,
            CreatedAt = DateTime.UtcNow
        };

        await _groupExpenseRepository.AddAsync(expense, cancellationToken);
        await _groupExpenseRepository.SaveChangesAsync(cancellationToken);

        var created = await _groupExpenseRepository.GetByIdAsync(expense.Id, cancellationToken)
            ?? throw new NotFoundException("Group expense not found after creation.");

        return MapExpense(created);
    }

    public async Task<IReadOnlyList<GroupExpenseResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        var expenses = await _groupExpenseRepository.GetByGroupIdAsync(groupId, cancellationToken);
        return expenses.Select(MapExpense).ToList();
    }

    private static GroupExpenseResponse MapExpense(GroupExpense expense)
    {
        return new GroupExpenseResponse(
            expense.Id,
            expense.GroupId,
            expense.CreatedByMemberId,
            expense.CreatedByMember.User.Name,
            expense.Amount,
            expense.Description,
            expense.ExpenseDate,
            expense.FundType,
            expense.CreatedAt);
    }

    private static async Task ValidateAsync<T>(
        IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors.Select(x => x.ErrorMessage));
        }
    }
}
