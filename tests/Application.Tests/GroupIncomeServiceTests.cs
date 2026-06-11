using MySociety.Application.Common.Exceptions;
using MySociety.Application.GroupExpenses;
using MySociety.Application.GroupExpenses.Dtos;
using MySociety.Application.GroupExpenses.Validators;
using MySociety.Application.GroupIncomes;
using MySociety.Application.GroupIncomes.Dtos;
using MySociety.Application.GroupIncomes.Validators;
using MySociety.Application.Groups.Dtos;
using MySociety.Application.Members.Dtos;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;
using MySociety.Infrastructure.Services;

namespace MySociety.Application.Tests;

public class GroupIncomeServiceTests
{
    [Fact]
    public async Task Create_group_income_increases_maintenance_fund_balance()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupAsync(context);
        var expenseService = CreateGroupExpenseService(context);
        var incomeService = CreateGroupIncomeService(context);

        var before = await expenseService.GetFundsAsync(groupId, memberId, CancellationToken.None);
        Assert.Equal(0m, before.Maintenance.Balance);

        await incomeService.CreateAsync(
            new CreateGroupIncomeRequest(groupId, 1500m, "Club house booking", UtcToday),
            adminId,
            CancellationToken.None);

        var after = await expenseService.GetFundsAsync(groupId, memberId, CancellationToken.None);
        Assert.Equal(1500m, after.Maintenance.Balance);
        Assert.Equal(1500m, after.Maintenance.TotalInflows);
        Assert.Equal(0m, after.Maintenance.TotalOutflows);
    }

    [Fact]
    public async Task Maintenance_balance_includes_contributions_and_income_minus_expenses()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithPaymentAsync(context);
        var expenseService = CreateGroupExpenseService(context);
        var incomeService = CreateGroupIncomeService(context);

        await incomeService.CreateAsync(
            new CreateGroupIncomeRequest(groupId, 500m, "Swimming pool booking", UtcToday),
            adminId,
            CancellationToken.None);

        await expenseService.CreateAsync(
            new CreateGroupExpenseRequest(groupId, 300m, "Security salary", UtcToday),
            adminId,
            CancellationToken.None);

        var funds = await expenseService.GetFundsAsync(groupId, memberId, CancellationToken.None);
        Assert.Equal(1200m, funds.Maintenance.Balance);
        Assert.Equal(1500m, funds.Maintenance.TotalInflows);
        Assert.Equal(300m, funds.Maintenance.TotalOutflows);
    }

    [Fact]
    public async Task Create_group_income_requires_admin()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupAsync(context);
        var sut = CreateGroupIncomeService(context);

        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await sut.CreateAsync(
                new CreateGroupIncomeRequest(groupId, 200m, "Parking fee", UtcToday),
                memberId,
                CancellationToken.None));
    }

    private static DateTime UtcToday => DateTime.UtcNow.Date;

    private static GroupIncomeService CreateGroupIncomeService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new GroupIncomeRepository(context),
            new CreateGroupIncomeRequestValidator());

    private static GroupExpenseService CreateGroupExpenseService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new GroupExpenseRepository(context),
            new LedgerService(context),
            new CreateGroupExpenseRequestValidator());

    private static async Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedGroupAsync(
        AppDbContext context)
    {
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "income_owner", "income@test.com", "Owner");
        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(),
            CancellationToken.None);

        var member = await memberService.CreateAsync(
            new CreateMemberRequest(
                created.Group.Id,
                "Member One",
                "9000000010",
                MemberRole.Member,
                0m,
                null),
            created.CreatorMember.Id,
            CancellationToken.None);

        return (created.Group.Id, created.CreatorMember.Id, member.Member.Id);
    }

    private static async Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedGroupWithPaymentAsync(
        AppDbContext context)
    {
        var (groupId, adminId, memberId) = await SeedGroupAsync(context);
        var contributionService = new Contributions.ContributionService(
            new GroupRepository(context),
            new MemberRepository(context),
            new ContributionRepository(context),
            new PaymentRepository(context),
            new LedgerService(context),
            new UnitOfWork(context),
            new Contributions.Validators.GenerateContributionsRequestValidator(),
            new Contributions.Validators.RecordPaymentRequestValidator(),
            new FakeNotificationService());

        var generated = await contributionService.GenerateAsync(
            new Contributions.Dtos.GenerateContributionsRequest(groupId, "2026-01", "2026-01"),
            adminId,
            CancellationToken.None);
        var contribution = generated.Contributions.Single(c => c.MemberId == memberId);

        var payment = await contributionService.RecordPaymentAsync(
            new Contributions.Dtos.RecordPaymentRequest(memberId, contribution.Amount, contribution.Id),
            memberId,
            CancellationToken.None);
        await contributionService.ApprovePaymentSubmissionAsync(
            payment.SubmissionId,
            adminId,
            CancellationToken.None);

        return (groupId, adminId, memberId);
    }
}
