using FluentValidation;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Contributions.Dtos;
using MySociety.Application.Financial;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;

namespace MySociety.Application.Contributions;

public interface IContributionService
{
    Task<GenerateContributionsResponse> GenerateAsync(
        GenerateContributionsRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<PaymentResponse> RecordPaymentAsync(
        RecordPaymentRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ContributionResponse>> GetByMemberIdAsync(
        Guid memberId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ContributionResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<GroupPendingContributionsResponse> GetPendingSummaryAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public class ContributionService : IContributionService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IContributionRepository _contributionRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILedgerService _ledgerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<GenerateContributionsRequest> _generateValidator;
    private readonly IValidator<RecordPaymentRequest> _paymentValidator;

    public ContributionService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        IContributionRepository contributionRepository,
        IPaymentRepository paymentRepository,
        ILedgerService ledgerService,
        IUnitOfWork unitOfWork,
        IValidator<GenerateContributionsRequest> generateValidator,
        IValidator<RecordPaymentRequest> paymentValidator)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _contributionRepository = contributionRepository;
        _paymentRepository = paymentRepository;
        _ledgerService = ledgerService;
        _unitOfWork = unitOfWork;
        _generateValidator = generateValidator;
        _paymentValidator = paymentValidator;
    }

    public async Task<GenerateContributionsResponse> GenerateAsync(
        GenerateContributionsRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_generateValidator, request, cancellationToken);

        var group = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, request.GroupId, cancellationToken);

        if (!ContributionMonthRange.TryValidate(
                request.FromMonth,
                request.ToMonth,
                out var periodKey,
                out var monthCount,
                out var rangeError))
        {
            throw new ValidationException(rangeError);
        }

        var existingPeriods = await _contributionRepository.GetDistinctPeriodsByGroupIdAsync(
            request.GroupId,
            cancellationToken);
        if (ContributionMonthRange.TryFindOverlappingPeriod(
                request.FromMonth,
                request.ToMonth,
                existingPeriods,
                out var overlappingPeriod))
        {
            throw new ConflictException(
                $"The requested range overlaps with contributions already generated for {ContributionMonthRange.FormatDisplayLabel(overlappingPeriod)}.");
        }

        var members = await _memberRepository.GetByGroupIdAsync(request.GroupId, cancellationToken);
        if (members.Count == 0)
        {
            throw new ValidationException("Group has no members to generate contributions for.");
        }

        var created = new List<ContributionResponse>();
        var skipped = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var member in members)
            {
                var existing = await _contributionRepository.GetByMemberAndPeriodAsync(
                    member.Id, periodKey, ct);

                if (existing is not null)
                {
                    skipped++;
                    continue;
                }

                var baseAmount = ContributionAmountCalculator.CalculateBaseAmount(group, member, monthCount);

                var balanceBeforeGeneration = await _ledgerService.GetBalanceAsync(
                    member.Id,
                    group.Id,
                    ct);
                var isCoveredByExistingCredit = baseAmount > 0 && balanceBeforeGeneration >= baseAmount;

                var contribution = new Contribution
                {
                    Id = Guid.NewGuid(),
                    MemberId = member.Id,
                    GroupId = group.Id,
                    Period = periodKey,
                    Amount = baseAmount,
                    Status = isCoveredByExistingCredit ? ContributionStatus.Paid : ContributionStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                await _contributionRepository.AddAsync(contribution, ct);

                if (baseAmount > 0)
                {
                    await _ledgerService.RecordContributionDebitAsync(
                        member.Id, group.Id, contribution.Id, baseAmount, ct);
                }

                created.Add(MapContribution(contribution, 0));
            }

            await _contributionRepository.SaveChangesAsync(ct);
        }, cancellationToken);

        return new GenerateContributionsResponse(
            request.GroupId,
            periodKey,
            request.FromMonth.Trim(),
            request.ToMonth.Trim(),
            monthCount,
            created.Count,
            skipped,
            created);
    }

    public async Task<PaymentResponse> RecordPaymentAsync(
        RecordPaymentRequest request,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_paymentValidator, request, cancellationToken);

        var member = await _memberRepository.GetByIdAsync(request.MemberId, cancellationToken)
            ?? throw new NotFoundException("Member not found.");

        var actingMember = await _memberRepository.GetByIdAsync(actingMemberId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid acting member.");

        if (actingMember.GroupId != member.GroupId)
        {
            throw new ForbiddenException("You are not a member of this group.");
        }

        var isAdmin = actingMember.Role == MemberRole.Admin;
        if (!isAdmin && actingMemberId != request.MemberId)
        {
            throw new ForbiddenException("You can only record payments for yourself.");
        }

        PaymentResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            Contribution? contribution = null;

            if (request.ContributionId.HasValue)
            {
                contribution = await _contributionRepository.GetByIdAsync(request.ContributionId.Value, ct)
                    ?? throw new NotFoundException("Contribution not found.");

                if (contribution.MemberId != member.Id)
                {
                    throw new ValidationException("Contribution does not belong to this member.");
                }

                if (contribution.Status == ContributionStatus.Paid)
                {
                    throw new ConflictException("Contribution is already paid.");
                }

                var paidSoFar = await _paymentRepository.GetTotalPaidForContributionAsync(contribution.Id, ct);
                var remaining = contribution.Amount - paidSoFar;
                if (remaining <= 0)
                {
                    contribution.Status = ContributionStatus.Paid;
                    throw new ConflictException("Contribution is already paid.");
                }

                if (request.Amount > remaining)
                {
                    throw new ValidationException(
                        $"Payment amount cannot exceed remaining balance ({remaining}).");
                }

                if (paidSoFar + request.Amount >= contribution.Amount)
                {
                    contribution.Status = ContributionStatus.Paid;
                }
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                MemberId = member.Id,
                GroupId = member.GroupId,
                ContributionId = contribution?.Id,
                Amount = request.Amount,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment, ct);
            await _paymentRepository.SaveChangesAsync(ct);

            await _ledgerService.RecordPaymentCreditAsync(
                member.Id, member.GroupId, payment.Id, request.Amount, ct);

            response = new PaymentResponse(
                payment.Id,
                payment.MemberId,
                payment.GroupId,
                payment.ContributionId,
                payment.Amount,
                payment.CreatedAt);
        }, cancellationToken);

        return response!;
    }

    public async Task<IReadOnlyList<ContributionResponse>> GetByMemberIdAsync(
        Guid memberId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken)
            ?? throw new NotFoundException("Member not found.");

        var actingMember = await _memberRepository.GetByIdAsync(actingMemberId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid acting member.");

        if (actingMember.GroupId != member.GroupId)
        {
            throw new ForbiddenException("You are not a member of this group.");
        }

        var isAdmin = actingMember.Role == MemberRole.Admin;
        if (!isAdmin && actingMemberId != memberId)
        {
            throw new ForbiddenException("You can only view your own contributions.");
        }

        var contributions = await _contributionRepository.GetByMemberIdAsync(memberId, cancellationToken);
        var paidTotals = await _paymentRepository.GetTotalsPaidByContributionIdsAsync(
            contributions.Select(c => c.Id).ToList(),
            cancellationToken);
        return contributions
            .Select(c => MapContribution(c, GetPaidAmount(paidTotals, c.Id), null))
            .ToList();
    }

    public async Task<IReadOnlyList<ContributionResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        var contributions = await _contributionRepository.GetByGroupIdAsync(groupId, cancellationToken);
        var paidTotals = await _paymentRepository.GetTotalsPaidByContributionIdsAsync(
            contributions.Select(c => c.Id).ToList(),
            cancellationToken);
        return contributions
            .Select(c => MapContribution(
                c,
                GetPaidAmount(paidTotals, c.Id),
                c.Member?.User?.Name))
            .ToList();
    }

    public async Task<GroupPendingContributionsResponse> GetPendingSummaryAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        var contributions = await _contributionRepository.GetByGroupIdAsync(groupId, cancellationToken);
        var paidTotals = await _paymentRepository.GetTotalsPaidByContributionIdsAsync(
            contributions.Select(c => c.Id).ToList(),
            cancellationToken);

        var members = contributions
            .Select(c =>
            {
                var paid = GetPaidAmount(paidTotals, c.Id);
                var remaining = Math.Max(0, c.Amount - paid);
                return new
                {
                    Contribution = c,
                    Paid = paid,
                    Remaining = remaining,
                    MemberName = c.Member?.User?.Name ?? "Member",
                };
            })
            .Where(x => x.Contribution.Status != ContributionStatus.Paid && x.Remaining > 0)
            .GroupBy(x => x.Contribution.MemberId)
            .Select(g =>
            {
                var items = g
                    .OrderByDescending(x => x.Contribution.Period)
                    .Select(x => new PendingContributionItemResponse(
                        x.Contribution.Id,
                        x.Contribution.Period,
                        x.Contribution.Amount,
                        x.Paid,
                        x.Remaining))
                    .ToList();

                return new MemberPendingContributionsResponse(
                    g.Key,
                    g.First().MemberName,
                    items.Sum(i => i.RemainingAmount),
                    items);
            })
            .OrderBy(m => m.MemberName)
            .ToList();

        return new GroupPendingContributionsResponse(
            groupId,
            members.Sum(m => m.TotalOutstanding),
            members.Count,
            members);
    }

    private static ContributionResponse MapContribution(
        Contribution contribution,
        decimal paidAmount,
        string? memberName = null)
    {
        var effectivePaidAmount = contribution.Status == ContributionStatus.Paid
            ? contribution.Amount
            : paidAmount;
        var remaining = Math.Max(0, contribution.Amount - effectivePaidAmount);

        return new ContributionResponse(
            contribution.Id,
            contribution.MemberId,
            contribution.GroupId,
            contribution.Period,
            contribution.Amount,
            contribution.Status,
            contribution.CreatedAt,
            memberName,
            effectivePaidAmount,
            remaining);
    }

    private static decimal GetPaidAmount(IReadOnlyDictionary<Guid, decimal> paidTotals, Guid contributionId) =>
        paidTotals.TryGetValue(contributionId, out var paid) ? paid : 0;

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors.Select(x => x.ErrorMessage));
        }
    }
}
