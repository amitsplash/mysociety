using MySociety.Application.Common.Exceptions;
using MySociety.Application.Contributions;
using MySociety.Application.Contributions.Dtos;
using MySociety.Application.Contributions.Validators;
using MySociety.Application.Groups;
using MySociety.Application.Groups.Dtos;
using MySociety.Application.Groups.Validators;
using MySociety.Application.Ledgers;
using MySociety.Application.Members;
using MySociety.Application.Members.Dtos;
using MySociety.Application.Members.Validators;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Application.GroupExpenses;
using MySociety.Application.GroupExpenses.Dtos;
using MySociety.Application.GroupExpenses.Validators;
using MySociety.Infrastructure.Repositories;
using MySociety.Infrastructure.Security;
using MySociety.Infrastructure.Services;

namespace MySociety.Application.Tests;

public class LedgerQueryServiceTests
{
    [Fact]
    public async Task GetMemberLedger_returns_entries_and_balance()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedAsync(context);

        var ledger = new LedgerService(context);
        await ledger.RecordOpeningBalanceAsync(memberId, groupId, -200m, CancellationToken.None);

        var sut = new LedgerQueryService(new MemberRepository(context), ledger);
        var result = await sut.GetMemberLedgerAsync(memberId, memberId, CancellationToken.None);

        Assert.Equal(-200m, result.Balance);
        Assert.Single(result.Entries);
    }

    [Fact]
    public async Task GetGroupBalances_returns_all_members()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedAsync(context);

        var sut = new LedgerQueryService(new MemberRepository(context), new LedgerService(context));
        var balances = await sut.GetGroupBalancesAsync(groupId, adminId, CancellationToken.None);

        Assert.Equal(2, balances.Count);
    }

    [Fact]
    public async Task GetGroupBalances_forbidden_for_non_admin()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedAsync(context);

        var sut = new LedgerQueryService(new MemberRepository(context), new LedgerService(context));

        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await sut.GetGroupBalancesAsync(groupId, memberId, CancellationToken.None));
    }

    [Fact]
    public async Task GetFundLedger_returns_inflows_outflows_and_running_balance()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedAsync(context);

        var contributionService = new ContributionService(
            new GroupRepository(context),
            new MemberRepository(context),
            new ContributionRepository(context),
            new PaymentRepository(context),
            new LedgerService(context),
            new UnitOfWork(context),
            new GenerateContributionsRequestValidator(),
            new RecordPaymentRequestValidator());

        var generated = await contributionService.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-01", "2026-01"),
            adminId,
            CancellationToken.None);
        var contribution = generated.Contributions.Single(c => c.MemberId == memberId);

        await contributionService.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, contribution.Amount, contribution.Id),
            memberId,
            CancellationToken.None);

        var societyExpenseService = new GroupExpenseService(
            new GroupRepository(context),
            new MemberRepository(context),
            new GroupExpenseRepository(context),
            new LedgerService(context),
            new CreateGroupExpenseRequestValidator());

        await societyExpenseService.CreateAsync(
            new CreateGroupExpenseRequest(groupId, 400m, "Lift repair", DateTime.UtcNow.Date),
            adminId,
            CancellationToken.None);

        var sut = new LedgerQueryService(new MemberRepository(context), new LedgerService(context));
        var ledger = await sut.GetFundLedgerAsync(groupId, adminId, CancellationToken.None);

        Assert.Equal(1000m, ledger.Funds.Maintenance.TotalInflows);
        Assert.Equal(400m, ledger.Funds.Maintenance.TotalOutflows);
        Assert.Equal(600m, ledger.Funds.Maintenance.Balance);
        Assert.Equal(2, ledger.Lines.Count);
        Assert.Equal(600m, ledger.Lines.Last().RunningBalance);
        Assert.Contains(ledger.Lines, x => x.Inflow > 0);
        Assert.Contains(ledger.Lines, x => x.Outflow > 0);
        Assert.True(
            ledger.Lines.Zip(ledger.Lines.Skip(1)).All(pair => pair.First.TransactionDate <= pair.Second.TransactionDate),
            "Lines must be in ascending date order.");
    }

    [Fact]
    public async Task GetFundLedger_includes_opening_maintenance_fund()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedAsync(context, openingMaintenanceBalance: 2500m);

        var sut = new LedgerQueryService(new MemberRepository(context), new LedgerService(context));
        var ledger = await sut.GetFundLedgerAsync(groupId, adminId, CancellationToken.None);

        Assert.Equal(2500m, ledger.Funds.Maintenance.Balance);
        Assert.Equal(2500m, ledger.Funds.Maintenance.TotalInflows);
        Assert.Equal(0m, ledger.Funds.Maintenance.TotalOutflows);
        Assert.Single(ledger.Lines);
        Assert.Equal("Opening maintenance fund", ledger.Lines[0].Description);
        Assert.Equal(2500m, ledger.Lines[0].Inflow);
        Assert.Equal(2500m, ledger.Lines[0].RunningBalance);
    }

    [Fact]
    public async Task GetFundLedger_forbidden_for_non_admin()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, _, memberId) = await SeedAsync(context);

        var sut = new LedgerQueryService(new MemberRepository(context), new LedgerService(context));

        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await sut.GetFundLedgerAsync(groupId, memberId, CancellationToken.None));
    }

    [Fact]
    public async Task GetGroupLedgerOverview_returns_all_member_ledgers()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedAsync(context);

        var ledger = new LedgerService(context);
        await ledger.RecordOpeningBalanceAsync(memberId, groupId, -200m, CancellationToken.None);

        var sut = new LedgerQueryService(new MemberRepository(context), ledger);
        var overview = await sut.GetGroupLedgerOverviewAsync(groupId, adminId, CancellationToken.None);

        Assert.Equal(2, overview.Members.Count);
        var memberSummary = overview.Members.Single(x => x.MemberId == memberId);
        Assert.Equal(-200m, memberSummary.Balance);
        Assert.Single(memberSummary.Entries);
    }

    [Fact]
    public async Task GetMemberLedger_forbidden_for_other_member()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedAsync(context);

        var sut = new LedgerQueryService(new MemberRepository(context), new LedgerService(context));

        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await sut.GetMemberLedgerAsync(adminId, memberId, CancellationToken.None));
    }

    private static Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedAsync(
        AppDbContext context,
        decimal openingMaintenanceBalance = 0m) =>
        SeedAsync(context, openingMaintenanceBalance, CancellationToken.None);

    private static async Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedAsync(
        AppDbContext context,
        decimal openingMaintenanceBalance,
        CancellationToken cancellationToken)
    {
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "ledger_owner", "ledger@test.com", "Owner");
        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(
                name: "Ledger Group",
                openingMaintenanceBalance: openingMaintenanceBalance),
            cancellationToken);

        var member = await memberService.CreateAsync(new CreateMemberRequest(
            created.Group.Id,
            "Member",
            "9222222222",
            MemberRole.Member,
            0m,
            null), created.CreatorMember.Id, cancellationToken);

        return (created.Group.Id, created.CreatorMember.Id, member.Member.Id);
    }
}
