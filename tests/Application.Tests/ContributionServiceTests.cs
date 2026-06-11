using FluentValidation;
using MySociety.Application.Common.Exceptions;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;
using MySociety.Application.Contributions;
using MySociety.Application.Contributions.Dtos;
using MySociety.Application.Contributions.Validators;
using MySociety.Application.Groups;
using MySociety.Application.Groups.Dtos;
using MySociety.Application.Groups.Validators;
using MySociety.Application.Members;
using MySociety.Application.Members.Dtos;
using MySociety.Application.Members.Validators;
using MySociety.Domain.Enums;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;
using MySociety.Infrastructure.Security;
using MySociety.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace MySociety.Application.Tests;

public class ContributionServiceTests
{
    [Fact]
    public async Task Generate_creates_pending_contributions_with_ledger_debits()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);

        var sut = CreateContributionService(context);
        var result = await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-05", "2026-05"),
            adminId,
            CancellationToken.None);

        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(1, result.MonthCount);
        Assert.Equal("2026-05..2026-05", result.Period);
        Assert.All(result.Contributions, c => Assert.Equal(ContributionStatus.Pending, c.Status));
        Assert.Equal(2, context.LedgerEntries.Count(x => x.Type == LedgerEntryType.Contribution));
    }

    [Fact]
    public async Task Generate_rejects_duplicate_period_for_group()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-05", "2026-05"),
            adminId,
            CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(async () =>
            await sut.GenerateAsync(
                new GenerateContributionsRequest(groupId, "2026-05", "2026-05"),
                adminId,
                CancellationToken.None));
    }

    [Fact]
    public async Task Generate_rejects_range_that_overlaps_existing_period()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-05", "2026-05"),
            adminId,
            CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConflictException>(async () =>
            await sut.GenerateAsync(
                new GenerateContributionsRequest(groupId, "2026-03", "2026-06"),
                adminId,
                CancellationToken.None));

        Assert.Contains("overlaps", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("May 2026", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Generate_allows_adjacent_non_overlapping_ranges()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-03", "2026-04"),
            adminId,
            CancellationToken.None);

        var result = await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-05", "2026-06"),
            adminId,
            CancellationToken.None);

        Assert.Equal("2026-05..2026-06", result.Period);
        Assert.Equal(2, result.CreatedCount);
    }

    [Fact]
    public async Task Generate_bills_full_amount_even_when_member_has_opening_credit()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);

        var ledger = new LedgerService(context);
        await ledger.RecordOpeningBalanceAsync(memberId, groupId, 1000m, CancellationToken.None);

        var sut = CreateContributionService(context);
        var result = await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-06", "2026-06"),
            adminId,
            CancellationToken.None);

        var memberContribution = result.Contributions.Single(x => x.MemberId == memberId);
        Assert.Equal(1000m, memberContribution.Amount);
        Assert.Equal(ContributionStatus.Paid, memberContribution.Status);
        Assert.Equal(1000m, memberContribution.PaidAmount);
        Assert.Equal(0m, memberContribution.RemainingAmount);

        var balance = await ledger.GetBalanceAsync(memberId, groupId, CancellationToken.None);
        Assert.Equal(0m, balance);

        var summary = await sut.GetPendingSummaryAsync(groupId, adminId, CancellationToken.None);
        Assert.DoesNotContain(summary.Members, m => m.MemberId == memberId);
    }

    [Fact]
    public async Task Generate_marks_paid_when_opening_credit_covers_monthly_due()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);

        var owner = await TestData.AddRegisteredUserAsync(context, "contrib_owner1", "c1@test.com", "Owner");
        var created = await groupService.CreateAsync(
            owner.Id,
            new CreateGroupRequest(
                "Credit Covered Group",
                GroupType.Friends,
                ContributionModel.Fixed,
                100m,
                ContributionFrequency.Monthly,
                0m,
                CreatorOpeningBalance: 500m),
            CancellationToken.None);

        var member = created.CreatorMember;

        var sut = CreateContributionService(context);
        var generated = await sut.GenerateAsync(
            new GenerateContributionsRequest(created.Group.Id, "2026-05", "2026-05"),
            created.CreatorMember.Id,
            CancellationToken.None);

        var memberContribution = generated.Contributions.Single(x => x.MemberId == member.Id);
        Assert.Equal(100m, memberContribution.Amount);
        Assert.Equal(ContributionStatus.Paid, memberContribution.Status);
        Assert.Equal(100m, memberContribution.PaidAmount);
        Assert.Equal(0m, memberContribution.RemainingAmount);

        var summary = await sut.GetPendingSummaryAsync(
            created.Group.Id,
            created.CreatorMember.Id,
            CancellationToken.None);
        Assert.DoesNotContain(summary.Members, x => x.MemberId == member.Id);
    }

    [Fact]
    public async Task Generate_with_2000_rate_and_1000_credit_shows_full_pending_and_net_balance()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);

        var owner = await TestData.AddRegisteredUserAsync(context, "contrib_owner2", "c2@test.com", "Owner");
        var created = await groupService.CreateAsync(
            owner.Id,
            new CreateGroupRequest(
                "RWA Group",
                GroupType.Rwa,
                ContributionModel.Fixed,
                2000m,
                ContributionFrequency.Monthly,
                0m),
            CancellationToken.None);

        var member = await memberService.CreateAsync(new CreateMemberRequest(
            created.Group.Id,
            "New Member",
            "9000000101",
            MemberRole.Member,
            1000m,
            null), created.CreatorMember.Id, CancellationToken.None);

        var sut = CreateContributionService(context);
        await sut.GenerateAsync(
            new GenerateContributionsRequest(created.Group.Id, "2026-05", "2026-05"),
            created.CreatorMember.Id,
            CancellationToken.None);

        var contribution = context.Contributions.Single(c => c.MemberId == member.Member.Id);
        Assert.Equal(2000m, contribution.Amount);

        var ledger = new LedgerService(context);
        var balance = await ledger.GetBalanceAsync(member.Member.Id, created.Group.Id, CancellationToken.None);
        Assert.Equal(-1000m, balance);

        var summary = await sut.GetPendingSummaryAsync(
            created.Group.Id,
            created.CreatorMember.Id,
            CancellationToken.None);
        var memberRow = summary.Members.Single(m => m.MemberId == member.Member.Id);
        Assert.Equal(2000m, memberRow.Items.Single().RemainingAmount);
    }

    [Fact]
    public async Task Generate_bills_full_amount_when_member_has_opening_due()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);

        var ledger = new LedgerService(context);
        await ledger.RecordOpeningBalanceAsync(memberId, groupId, -1000m, CancellationToken.None);

        var sut = CreateContributionService(context);
        var result = await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-12", "2026-12"),
            adminId,
            CancellationToken.None);

        var memberContribution = result.Contributions.Single(x => x.MemberId == memberId);
        Assert.Equal(1000m, memberContribution.Amount);

        var balance = await ledger.GetBalanceAsync(memberId, groupId, CancellationToken.None);
        Assert.Equal(-2000m, balance);
    }

    [Fact]
    public async Task Generate_multiplies_amount_by_month_count()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        var result = await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-01", "2026-03"),
            adminId,
            CancellationToken.None);

        Assert.Equal(3, result.MonthCount);
        Assert.Equal("2026-01..2026-03", result.Period);
        var memberContribution = result.Contributions.Single(x => x.MemberId == memberId);
        Assert.Equal(3000m, memberContribution.Amount);
    }

    [Fact]
    public async Task Generate_rejects_invalid_month_range()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await sut.GenerateAsync(
                new GenerateContributionsRequest(groupId, "2026-05", "2026-04"),
                adminId,
                CancellationToken.None));
    }

    [Fact]
    public async Task RecordPayment_marks_contribution_paid_and_creates_ledger_credit()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        var generated = await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-07", "2026-07"),
            adminId,
            CancellationToken.None);

        var contribution = generated.Contributions.First(x => x.MemberId == memberId);

        var payment = await sut.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, contribution.Amount, contribution.Id),
            adminId,
            CancellationToken.None);

        Assert.NotEmpty(payment.Allocations);
        Assert.NotEqual(Guid.Empty, payment.Allocations[0].PaymentId);
        var updated = await context.Contributions.FindAsync(contribution.Id);
        Assert.Equal(ContributionStatus.Paid, updated!.Status);
        Assert.Contains(context.LedgerEntries, x => x.Type == LedgerEntryType.Payment);
    }

    [Fact]
    public async Task RecordPayment_admin_can_record_cash_for_another_member()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        var generated = await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-08", "2026-08"),
            adminId,
            CancellationToken.None);

        var memberContribution = generated.Contributions.First(x => x.MemberId == memberId);

        await sut.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, memberContribution.Amount, memberContribution.Id),
            adminId,
            CancellationToken.None);

        var updated = await context.Contributions.FindAsync(memberContribution.Id);
        Assert.Equal(ContributionStatus.Paid, updated!.Status);
    }

    [Fact]
    public async Task GetByGroupId_returns_all_member_contributions_for_admin()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-09", "2026-09"),
            adminId,
            CancellationToken.None);

        var all = await sut.GetByGroupIdAsync(groupId, adminId, CancellationToken.None);

        Assert.Equal(2, all.Count);
        Assert.Contains(all, x => x.MemberId == memberId && !string.IsNullOrWhiteSpace(x.MemberName));
    }

    [Fact]
    public async Task RecordPayment_partial_keeps_pending_until_fully_paid()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        var generated = await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-10", "2026-10"),
            adminId,
            CancellationToken.None);

        var contribution = generated.Contributions.First(x => x.MemberId == memberId);
        var partial = contribution.Amount / 2;

        await sut.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, partial, contribution.Id),
            adminId,
            CancellationToken.None);

        var updated = await context.Contributions.FindAsync(contribution.Id);
        Assert.Equal(ContributionStatus.Pending, updated!.Status);

        await sut.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, contribution.Amount - partial, contribution.Id),
            adminId,
            CancellationToken.None);

        updated = await context.Contributions.FindAsync(contribution.Id);
        Assert.Equal(ContributionStatus.Paid, updated!.Status);
    }

    [Fact]
    public async Task RecordPayment_without_contribution_allocates_oldest_periods_first()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-01", "2026-01"),
            adminId,
            CancellationToken.None);
        await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-02", "2026-02"),
            adminId,
            CancellationToken.None);
        await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-03", "2026-03"),
            adminId,
            CancellationToken.None);

        var result = await sut.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, 1500m, null),
            adminId,
            CancellationToken.None);

        Assert.Equal(1500m, result.TotalAmount);
        Assert.Equal(2, result.Allocations.Count);
        Assert.Equal("2026-01..2026-01", result.Allocations[0].Period);
        Assert.Equal(1000m, result.Allocations[0].AmountApplied);
        Assert.Equal(0m, result.Allocations[0].RemainingAfter);
        Assert.Equal("2026-02..2026-02", result.Allocations[1].Period);
        Assert.Equal(500m, result.Allocations[1].AmountApplied);
        Assert.Equal(500m, result.Allocations[1].RemainingAfter);

        var summary = await sut.GetPendingSummaryAsync(groupId, adminId, CancellationToken.None);
        var memberRow = summary.Members.Single(m => m.MemberId == memberId);
        Assert.Equal(2, memberRow.Items.Count);
        Assert.Equal("2026-02..2026-02", memberRow.Items[0].Period);
        Assert.Equal("2026-03..2026-03", memberRow.Items[1].Period);
        Assert.Contains("500.00 received", memberRow.Items[0].InternalRemark ?? string.Empty);

        var janContribution = context.Contributions.Single(c =>
            c.MemberId == memberId && c.Period == "2026-01..2026-01");
        Assert.Contains("1000.00 received", janContribution.InternalRemark ?? string.Empty);
    }

    [Fact]
    public async Task RecordPayment_appends_internal_remark_on_single_contribution()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        var generated = await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-12", "2026-12"),
            adminId,
            CancellationToken.None);

        var contribution = generated.Contributions.First(x => x.MemberId == memberId);
        var partial = contribution.Amount / 2;

        await sut.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, partial, contribution.Id),
            adminId,
            CancellationToken.None);

        var updated = await context.Contributions.FindAsync(contribution.Id);
        Assert.Contains($"{partial:N2} received", updated!.InternalRemark);
    }

    [Fact]
    public async Task RecordPayment_without_contribution_applies_excess_as_advance_credit()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-04", "2026-04"),
            adminId,
            CancellationToken.None);

        var result = await sut.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, 1500m, null),
            adminId,
            CancellationToken.None);

        Assert.Equal(1500m, result.TotalAmount);
        Assert.Equal(500m, result.AdvanceAmount);
        Assert.Equal(2, result.Allocations.Count);
        Assert.Null(result.Allocations[1].ContributionId);
        Assert.Equal("Advance credit", result.Allocations[1].Period);

        var ledger = new LedgerService(context);
        var balance = await ledger.GetBalanceAsync(memberId, groupId, CancellationToken.None);
        Assert.Equal(500m, balance);
    }

    [Fact]
    public async Task RecordPayment_without_pending_creates_advance_only()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        var result = await sut.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, 750m, null),
            adminId,
            CancellationToken.None);

        Assert.Equal(750m, result.TotalAmount);
        Assert.Equal(750m, result.AdvanceAmount);
        Assert.Single(result.Allocations);
        Assert.Equal("Advance credit", result.Allocations[0].Period);
    }

    [Fact]
    public async Task Generate_applies_advance_credit_to_upcoming_contribution_with_remark()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        await sut.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, 1000m, null),
            adminId,
            CancellationToken.None);

        var generated = await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-05", "2026-05"),
            adminId,
            CancellationToken.None);

        var memberContribution = generated.Contributions.Single(x => x.MemberId == memberId);
        Assert.Equal(ContributionStatus.Paid, memberContribution.Status);

        var stored = await context.Contributions.FindAsync(memberContribution.Id);
        Assert.Contains("adjusted from advance credit", stored!.InternalRemark);
    }

    [Fact]
    public async Task RecordPayment_member_submission_requires_admin_approval()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-06", "2026-06"),
            adminId,
            CancellationToken.None);

        var result = await sut.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, 500m, null),
            memberId,
            CancellationToken.None);

        Assert.Equal(PaymentStatus.PendingApproval, result.Status);
        Assert.Equal(500m, result.TotalAmount);

        var contribution = context.Contributions.Single(c => c.MemberId == memberId);
        Assert.Equal(ContributionStatus.Pending, contribution.Status);
        Assert.DoesNotContain(context.LedgerEntries, x => x.Type == LedgerEntryType.Payment);

        var approved = await sut.ApprovePaymentSubmissionAsync(result.SubmissionId, adminId, CancellationToken.None);
        Assert.Equal(PaymentStatus.Approved, approved.Status);

        contribution = await context.Contributions.FindAsync(contribution.Id);
        Assert.Equal(ContributionStatus.Pending, contribution.Status);
        Assert.Contains(context.LedgerEntries, x => x.Type == LedgerEntryType.Payment);
    }

    [Fact]
    public async Task RejectPaymentSubmission_does_not_apply_payment()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-07", "2026-07"),
            adminId,
            CancellationToken.None);

        var result = await sut.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, 1000m, null),
            memberId,
            CancellationToken.None);

        await sut.RejectPaymentSubmissionAsync(result.SubmissionId, adminId, CancellationToken.None);

        var contribution = context.Contributions.Single(c => c.MemberId == memberId);
        Assert.Equal(ContributionStatus.Pending, contribution.Status);
        Assert.DoesNotContain(context.LedgerEntries, x => x.Type == LedgerEntryType.Payment);
    }

    [Fact]
    public async Task GetPendingSummary_groups_outstanding_by_member()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateContributionService(context);

        await sut.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-11", "2026-11"),
            adminId,
            CancellationToken.None);

        var summary = await sut.GetPendingSummaryAsync(groupId, adminId, CancellationToken.None);

        Assert.True(summary.TotalOutstanding > 0);
        Assert.Equal(2, summary.MemberCount);
        var memberRow = summary.Members.Single(m => m.MemberId == memberId);
        Assert.Single(memberRow.Items);
        Assert.Equal(memberRow.Items[0].RemainingAmount, memberRow.TotalOutstanding);
    }

    private static async Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedGroupWithMemberAsync(AppDbContext context)
    {
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);

        var owner = await TestData.AddRegisteredUserAsync(context, "contrib_owner3", "c3@test.com", "Owner");
        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(),
            CancellationToken.None);

        var member = await memberService.CreateAsync(new CreateMemberRequest(
            created.Group.Id,
            "Member One",
            "9000000003",
            MemberRole.Member,
            0m,
            null), created.CreatorMember.Id, CancellationToken.None);

        return (created.Group.Id, created.CreatorMember.Id, member.Member.Id);
    }

    private static ContributionService CreateContributionService(AppDbContext context)
    {
        return new ContributionService(
            new GroupRepository(context),
            new MemberRepository(context),
            new ContributionRepository(context),
            new PaymentRepository(context),
            new LedgerService(context),
            new UnitOfWork(context),
            new GenerateContributionsRequestValidator(),
            new RecordPaymentRequestValidator(),
            new FakeNotificationService());
    }
}
