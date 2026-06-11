using FluentValidation;
using MySociety.Application.Common;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.GroupIncomes.Dtos;
using MySociety.Domain.Entities;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.GroupIncomes;

public interface IGroupIncomeService
{
    Task<GroupIncomeResponse> CreateAsync(
        CreateGroupIncomeRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GroupIncomeResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public class GroupIncomeService : IGroupIncomeService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IGroupIncomeRepository _groupIncomeRepository;
    private readonly IValidator<CreateGroupIncomeRequest> _createValidator;

    public GroupIncomeService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        IGroupIncomeRepository groupIncomeRepository,
        IValidator<CreateGroupIncomeRequest> createValidator)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _groupIncomeRepository = groupIncomeRepository;
        _createValidator = createValidator;
    }

    public async Task<GroupIncomeResponse> CreateAsync(
        CreateGroupIncomeRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);

        _ = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, request.GroupId, cancellationToken);

        var income = new GroupIncome
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            CreatedByMemberId = actingMemberId,
            Amount = request.Amount,
            Description = request.Description.Trim(),
            IncomeDate = ExpenseDateRules.NormalizeToUtcDate(request.IncomeDate),
            CreatedAt = DateTime.UtcNow
        };

        await _groupIncomeRepository.AddAsync(income, cancellationToken);
        await _groupIncomeRepository.SaveChangesAsync(cancellationToken);

        var created = await _groupIncomeRepository.GetByIdAsync(income.Id, cancellationToken)
            ?? throw new NotFoundException("Group income not found after creation.");

        return MapIncome(created);
    }

    public async Task<IReadOnlyList<GroupIncomeResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        var incomes = await _groupIncomeRepository.GetByGroupIdAsync(groupId, cancellationToken);
        return incomes.Select(MapIncome).ToList();
    }

    private static GroupIncomeResponse MapIncome(GroupIncome income)
    {
        return new GroupIncomeResponse(
            income.Id,
            income.GroupId,
            income.CreatedByMemberId,
            income.CreatedByMember.User.Name,
            income.Amount,
            income.Description,
            income.IncomeDate,
            income.CreatedAt);
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
