using System.Globalization;
using FluentValidation;
using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.Contributions.Dtos;
using MySociety.Application.Financial;
using MySociety.Application.Notifications;
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

    Task<RecordPaymentResponse> RecordPaymentAsync(
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

    Task<IReadOnlyList<PendingPaymentSubmissionResponse>> GetPendingPaymentSubmissionsAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PendingPaymentSubmissionResponse>> GetMyPendingPaymentSubmissionsAsync(
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<PaymentSubmissionActionResponse> ApprovePaymentSubmissionAsync(
        Guid submissionId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<PaymentSubmissionActionResponse> RejectPaymentSubmissionAsync(
        Guid submissionId,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public class ContributionService : IContributionService
{
    internal const string AdvanceCreditPeriod = "Advance credit";

    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IContributionRepository _contributionRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILedgerService _ledgerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<GenerateContributionsRequest> _generateValidator;
    private readonly IValidator<RecordPaymentRequest> _paymentValidator;
    private readonly INotificationService _notificationService;

    public ContributionService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        IContributionRepository contributionRepository,
        IPaymentRepository paymentRepository,
        ILedgerService ledgerService,
        IUnitOfWork unitOfWork,
        IValidator<GenerateContributionsRequest> generateValidator,
        IValidator<RecordPaymentRequest> paymentValidator,
        INotificationService notificationService)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _contributionRepository = contributionRepository;
        _paymentRepository = paymentRepository;
        _ledgerService = ledgerService;
        _unitOfWork = unitOfWork;
        _generateValidator = generateValidator;
        _paymentValidator = paymentValidator;
        _notificationService = notificationService;
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

                if (isCoveredByExistingCredit)
                {
                    AppendAdvanceAdjustmentRemark(contribution, baseAmount, DateTime.UtcNow);
                }

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

        if (created.Count > 0)
        {
            var actingMember = await _memberRepository.GetByIdAsync(actingMemberId, cancellationToken);
            var periodLabel = ContributionMonthRange.FormatDisplayLabel(periodKey);

            await _notificationService.NotifyGroupMembersAsync(
                request.GroupId,
                NotificationType.ContributionsGenerated,
                "New contributions generated",
                $"Contributions for {periodLabel} have been generated in {group.Name}.",
                new
                {
                    groupId = request.GroupId,
                    period = periodKey,
                    fromMonth = request.FromMonth.Trim(),
                    toMonth = request.ToMonth.Trim(),
                    createdCount = created.Count,
                },
                excludeUserId: actingMember?.UserId,
                cancellationToken);
        }

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

    public async Task<RecordPaymentResponse> RecordPaymentAsync(
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

        var autoApprove = isAdmin;
        var submissionId = Guid.NewGuid();

        RecordPaymentResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var paidAt = DateTime.UtcNow;
            var allocations = new List<PaymentAllocationDetail>();

            if (request.ContributionId.HasValue)
            {
                var contribution = await _contributionRepository.GetByIdAsync(request.ContributionId.Value, ct)
                    ?? throw new NotFoundException("Contribution not found.");

                if (contribution.MemberId != member.Id)
                {
                    throw new ValidationException("Contribution does not belong to this member.");
                }

                var remaining = await GetContributionRemainingAsync(contribution, ct);

                if (remaining > 0)
                {
                    var apply = Math.Min(request.Amount, remaining);
                    allocations.Add(await CreateContributionPaymentAsync(
                        member,
                        contribution,
                        apply,
                        paidAt,
                        submissionId,
                        actingMemberId,
                        autoApprove,
                        ct));

                    var excess = request.Amount - apply;
                    if (excess > 0)
                    {
                        allocations.Add(await CreateAdvancePaymentAsync(
                            member, excess, paidAt, submissionId, actingMemberId, autoApprove, ct));
                    }
                }
                else if (request.Amount > 0)
                {
                    allocations.Add(await CreateAdvancePaymentAsync(
                        member, request.Amount, paidAt, submissionId, actingMemberId, autoApprove, ct));
                }
            }
            else
            {
                var contributions = await _contributionRepository.GetByMemberIdAsync(member.Id, ct);
                var contributionIds = contributions.Select(c => c.Id).ToList();
                var paidTotals = await _paymentRepository.GetTotalsPaidByContributionIdsAsync(
                    contributionIds, ct);
                var pendingApprovalTotals = await _paymentRepository.GetPendingApprovalTotalsByContributionIdsAsync(
                    contributionIds, ct);

                var pending = contributions
                    .Select(c =>
                    {
                        var paid = GetPaidAmount(paidTotals, c.Id);
                        var pendingApproval = GetPaidAmount(pendingApprovalTotals, c.Id);
                        var remaining = Math.Max(0, c.Amount - paid - pendingApproval);
                        return new { Contribution = c, Remaining = remaining };
                    })
                    .Where(x => x.Contribution.Status != ContributionStatus.Paid && x.Remaining > 0)
                    .OrderBy(x => x.Contribution.Period)
                    .ToList();

                var amountLeft = request.Amount;
                foreach (var item in pending)
                {
                    if (amountLeft <= 0)
                    {
                        break;
                    }

                    var apply = Math.Min(amountLeft, item.Remaining);
                    allocations.Add(await CreateContributionPaymentAsync(
                        member,
                        item.Contribution,
                        apply,
                        paidAt,
                        submissionId,
                        actingMemberId,
                        autoApprove,
                        ct));
                    amountLeft -= apply;
                }

                if (amountLeft > 0)
                {
                    allocations.Add(await CreateAdvancePaymentAsync(
                        member, amountLeft, paidAt, submissionId, actingMemberId, autoApprove, ct));
                }
            }

            if (allocations.Count == 0)
            {
                throw new ValidationException("Enter a valid payment amount.");
            }

            await _paymentRepository.SaveChangesAsync(ct);

            var advanceAmount = allocations
                .Where(a => a.ContributionId is null)
                .Sum(a => a.AmountApplied);
            var status = autoApprove ? PaymentStatus.Approved : PaymentStatus.PendingApproval;

            response = new RecordPaymentResponse(
                submissionId,
                member.Id,
                member.GroupId,
                request.Amount,
                advanceAmount,
                status,
                paidAt,
                allocations);
        }, cancellationToken);

        return response!;
    }

    public async Task<IReadOnlyList<PendingPaymentSubmissionResponse>> GetPendingPaymentSubmissionsAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);

        var payments = await _paymentRepository.GetPendingApprovalByGroupIdAsync(groupId, cancellationToken);
        return GroupPendingPaymentSubmissions(payments);
    }

    public async Task<IReadOnlyList<PendingPaymentSubmissionResponse>> GetMyPendingPaymentSubmissionsAsync(
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetPendingApprovalByMemberIdAsync(actingMemberId, cancellationToken);
        return GroupPendingPaymentSubmissions(payments);
    }

    public async Task<PaymentSubmissionActionResponse> ApprovePaymentSubmissionAsync(
        Guid submissionId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetBySubmissionIdAsync(submissionId, cancellationToken);
        if (payments.Count == 0)
        {
            throw new NotFoundException("Payment submission not found.");
        }

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, payments[0].GroupId, cancellationToken);

        EnsureSubmissionPendingApproval(payments);

        PaymentSubmissionActionResponse? response = null;
        var approvedAt = DateTime.UtcNow;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var allocations = new List<PaymentAllocationDetail>();

            foreach (var payment in payments)
            {
                payment.Status = PaymentStatus.Approved;
                payment.ApprovedByMemberId = actingMemberId;
                payment.ApprovedAt = approvedAt;

                if (payment.ContributionId.HasValue)
                {
                    var contribution = payment.Contribution
                        ?? await _contributionRepository.GetByIdAsync(payment.ContributionId.Value, ct)
                        ?? throw new NotFoundException("Contribution not found.");

                    allocations.Add(await FinalizeApprovedContributionPaymentAsync(
                        payment.Member, contribution, payment, approvedAt, ct));
                }
                else
                {
                    allocations.Add(await FinalizeApprovedAdvancePaymentAsync(
                        payment.Member, payment, approvedAt, ct));
                }
            }

            await _paymentRepository.SaveChangesAsync(ct);

            response = new PaymentSubmissionActionResponse(
                submissionId,
                PaymentStatus.Approved,
                payments.Sum(p => p.Amount),
                allocations);
        }, cancellationToken);

        return response!;
    }

    public async Task<PaymentSubmissionActionResponse> RejectPaymentSubmissionAsync(
        Guid submissionId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetBySubmissionIdAsync(submissionId, cancellationToken);
        if (payments.Count == 0)
        {
            throw new NotFoundException("Payment submission not found.");
        }

        await MemberAuthorization.EnsureGroupAdminAsync(
            _memberRepository, actingMemberId, payments[0].GroupId, cancellationToken);

        EnsureSubmissionPendingApproval(payments);

        var rejectedAt = DateTime.UtcNow;
        foreach (var payment in payments)
        {
            payment.Status = PaymentStatus.Rejected;
            payment.ApprovedByMemberId = actingMemberId;
            payment.ApprovedAt = rejectedAt;
        }

        await _paymentRepository.SaveChangesAsync(cancellationToken);

        return new PaymentSubmissionActionResponse(
            submissionId,
            PaymentStatus.Rejected,
            payments.Sum(p => p.Amount),
            payments.Select(MapPendingAllocation).ToList());
    }

    private async Task<PaymentAllocationDetail> CreateAdvancePaymentAsync(
        Member member,
        decimal amount,
        DateTime paidAt,
        Guid submissionId,
        Guid recordedByMemberId,
        bool autoApprove,
        CancellationToken cancellationToken)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            GroupId = member.GroupId,
            ContributionId = null,
            Amount = amount,
            Status = autoApprove ? PaymentStatus.Approved : PaymentStatus.PendingApproval,
            SubmissionId = submissionId,
            RecordedByMemberId = recordedByMemberId,
            ApprovedByMemberId = autoApprove ? recordedByMemberId : null,
            ApprovedAt = autoApprove ? paidAt : null,
            CreatedAt = paidAt
        };

        await _paymentRepository.AddAsync(payment, cancellationToken);

        if (autoApprove)
        {
            return await FinalizeApprovedAdvancePaymentAsync(member, payment, paidAt, cancellationToken);
        }

        return new PaymentAllocationDetail(
            payment.Id,
            null,
            AdvanceCreditPeriod,
            amount,
            0,
            AppendAdvancePaymentRemark(amount, paidAt),
            PaymentStatus.PendingApproval);
    }

    private async Task<PaymentAllocationDetail> CreateContributionPaymentAsync(
        Member member,
        Contribution contribution,
        decimal amount,
        DateTime paidAt,
        Guid submissionId,
        Guid recordedByMemberId,
        bool autoApprove,
        CancellationToken cancellationToken)
    {
        if (contribution.Status == ContributionStatus.Paid)
        {
            throw new ConflictException("Contribution is already paid.");
        }

        var remaining = await GetContributionRemainingAsync(contribution, cancellationToken);
        if (remaining <= 0)
        {
            contribution.Status = ContributionStatus.Paid;
            throw new ConflictException("Contribution is already paid.");
        }

        if (amount > remaining)
        {
            throw new ValidationException(
                $"Payment amount cannot exceed remaining balance ({remaining}).");
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            GroupId = member.GroupId,
            ContributionId = contribution.Id,
            Amount = amount,
            Status = autoApprove ? PaymentStatus.Approved : PaymentStatus.PendingApproval,
            SubmissionId = submissionId,
            RecordedByMemberId = recordedByMemberId,
            ApprovedByMemberId = autoApprove ? recordedByMemberId : null,
            ApprovedAt = autoApprove ? paidAt : null,
            CreatedAt = paidAt
        };

        await _paymentRepository.AddAsync(payment, cancellationToken);

        if (autoApprove)
        {
            return await FinalizeApprovedContributionPaymentAsync(member, contribution, payment, paidAt, cancellationToken);
        }

        var remainingAfter = Math.Max(0, remaining - amount);
        return new PaymentAllocationDetail(
            payment.Id,
            contribution.Id,
            contribution.Period,
            amount,
            remainingAfter,
            null,
            PaymentStatus.PendingApproval);
    }

    private async Task<PaymentAllocationDetail> FinalizeApprovedAdvancePaymentAsync(
        Member member,
        Payment payment,
        DateTime paidAt,
        CancellationToken cancellationToken)
    {
        await _ledgerService.RecordPaymentCreditAsync(
            member.Id, member.GroupId, payment.Id, payment.Amount, cancellationToken);

        return new PaymentAllocationDetail(
            payment.Id,
            null,
            AdvanceCreditPeriod,
            payment.Amount,
            0,
            AppendAdvancePaymentRemark(payment.Amount, paidAt),
            PaymentStatus.Approved);
    }

    private async Task<PaymentAllocationDetail> FinalizeApprovedContributionPaymentAsync(
        Member member,
        Contribution contribution,
        Payment payment,
        DateTime paidAt,
        CancellationToken cancellationToken)
    {
        var paidSoFar = await _paymentRepository.GetTotalPaidForContributionAsync(contribution.Id, cancellationToken);
        var remaining = contribution.Amount - paidSoFar;
        if (remaining <= 0)
        {
            contribution.Status = ContributionStatus.Paid;
            throw new ConflictException("Contribution is already paid.");
        }

        if (payment.Amount > remaining)
        {
            throw new ValidationException(
                $"Approved amount exceeds remaining balance ({remaining}).");
        }

        if (paidSoFar + payment.Amount >= contribution.Amount)
        {
            contribution.Status = ContributionStatus.Paid;
        }

        AppendInternalRemark(contribution, payment.Amount, paidAt);

        await _ledgerService.RecordPaymentCreditAsync(
            member.Id, member.GroupId, payment.Id, payment.Amount, cancellationToken);

        var remainingAfter = Math.Max(0, remaining - payment.Amount);
        return new PaymentAllocationDetail(
            payment.Id,
            contribution.Id,
            contribution.Period,
            payment.Amount,
            remainingAfter,
            contribution.InternalRemark,
            PaymentStatus.Approved);
    }

    private async Task<decimal> GetContributionRemainingAsync(
        Contribution contribution,
        CancellationToken cancellationToken)
    {
        var paidSoFar = await _paymentRepository.GetTotalPaidForContributionAsync(contribution.Id, cancellationToken);
        var pendingApproval = (await _paymentRepository.GetPendingApprovalTotalsByContributionIdsAsync(
            [contribution.Id], cancellationToken)).GetValueOrDefault(contribution.Id);
        return Math.Max(0, contribution.Amount - paidSoFar - pendingApproval);
    }

    private static void EnsureSubmissionPendingApproval(IReadOnlyList<Payment> payments)
    {
        if (payments.Any(p => p.Status != PaymentStatus.PendingApproval))
        {
            throw new ConflictException("Payment submission is no longer awaiting approval.");
        }
    }

    private static IReadOnlyList<PendingPaymentSubmissionResponse> GroupPendingPaymentSubmissions(
        IReadOnlyList<Payment> payments)
    {
        return payments
            .GroupBy(p => p.SubmissionId)
            .Select(g =>
            {
                var first = g.First();
                var allocations = g.Select(MapPendingAllocation).ToList();
                return new PendingPaymentSubmissionResponse(
                    g.Key,
                    first.MemberId,
                    first.Member?.User?.Name ?? "Member",
                    g.Sum(p => p.Amount),
                    g.Where(p => p.ContributionId is null).Sum(p => p.Amount),
                    first.CreatedAt,
                    allocations);
            })
            .OrderByDescending(x => x.SubmittedAt)
            .ToList();
    }

    private static PaymentAllocationDetail MapPendingAllocation(Payment payment) =>
        new(
            payment.Id,
            payment.ContributionId,
            payment.ContributionId is null ? AdvanceCreditPeriod : payment.Contribution!.Period,
            payment.Amount,
            0,
            null,
            payment.Status);

    private static void AppendInternalRemark(Contribution contribution, decimal amountApplied, DateTime paidAt)
    {
        var line = $"[{paidAt:yyyy-MM-dd}] {amountApplied.ToString("0.00", CultureInfo.InvariantCulture)} received";
        contribution.InternalRemark = string.IsNullOrWhiteSpace(contribution.InternalRemark)
            ? line
            : $"{contribution.InternalRemark}\n{line}";
    }

    private static void AppendAdvanceAdjustmentRemark(
        Contribution contribution,
        decimal amountApplied,
        DateTime appliedAt)
    {
        var line =
            $"[{appliedAt:yyyy-MM-dd}] {amountApplied.ToString("0.00", CultureInfo.InvariantCulture)} adjusted from advance credit";
        contribution.InternalRemark = string.IsNullOrWhiteSpace(contribution.InternalRemark)
            ? line
            : $"{contribution.InternalRemark}\n{line}";
    }

    private static string AppendAdvancePaymentRemark(decimal amountApplied, DateTime paidAt) =>
        $"[{paidAt:yyyy-MM-dd}] {amountApplied.ToString("0.00", CultureInfo.InvariantCulture)} held as advance for upcoming contributions";

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
                    .OrderBy(x => x.Contribution.Period)
                    .Select(x => new PendingContributionItemResponse(
                        x.Contribution.Id,
                        x.Contribution.Period,
                        x.Contribution.Amount,
                        x.Paid,
                        x.Remaining,
                        x.Contribution.InternalRemark))
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
            remaining,
            contribution.InternalRemark);
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
