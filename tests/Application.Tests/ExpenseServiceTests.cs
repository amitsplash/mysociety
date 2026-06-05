using MySociety.Application.Common.Exceptions;
using MySociety.Application.Contributions;
using MySociety.Application.Contributions.Dtos;
using MySociety.Application.Contributions.Validators;
using MySociety.Application.Expenses;
using MySociety.Application.Expenses.Dtos;
using MySociety.Application.Expenses.Validators;
using MySociety.Application.Groups;
using MySociety.Application.Groups.Dtos;
using MySociety.Application.Groups.Validators;
using MySociety.Application.Members;
using MySociety.Application.Members.Dtos;
using MySociety.Application.Members.Validators;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;
using MySociety.Infrastructure.Security;
using MySociety.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace MySociety.Application.Tests;

public class ExpenseServiceTests
{
    [Fact]
    public async Task Create_expense_stays_pending_without_ledger_entry()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateExpenseService(context);

        var result = await sut.CreateAsync(
            new CreateExpenseRequest(groupId, 500m, "Office supplies", UtcToday),
            memberId,
            CancellationToken.None);

        Assert.Equal(ExpenseStatus.Pending, result.Status);
        Assert.Empty(context.LedgerEntries.Where(x => x.Type == LedgerEntryType.Expense));
    }

    [Fact]
    public async Task Approve_creates_expense_ledger_credit_for_submitter()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateExpenseService(context);

        var expense = await sut.CreateAsync(
            new CreateExpenseRequest(groupId, 750m, "Event catering", UtcToday),
            memberId,
            CancellationToken.None);

        var approved = await sut.ApproveAsync(expense.Id, adminId, CancellationToken.None);

        Assert.Equal(ExpenseStatus.Approved, approved.Status);
        var ledgerEntry = context.LedgerEntries.Single(x => x.Type == LedgerEntryType.Expense);
        Assert.Equal(memberId, ledgerEntry.MemberId);
        Assert.Equal(LedgerEntryDirection.Credit, ledgerEntry.Direction);
        Assert.Equal(750m, ledgerEntry.Amount);
    }

    [Fact]
    public async Task Approve_rejects_already_approved_expense()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateExpenseService(context);

        var expense = await sut.CreateAsync(
            new CreateExpenseRequest(groupId, 200m, "Snacks", UtcToday),
            memberId,
            CancellationToken.None);

        await sut.ApproveAsync(expense.Id, adminId, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(async () =>
            await sut.ApproveAsync(expense.Id, adminId, CancellationToken.None));
    }

    [Fact]
    public async Task Reject_does_not_create_ledger_entry()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateExpenseService(context);

        var expense = await sut.CreateAsync(
            new CreateExpenseRequest(groupId, 300m, "Invalid claim", UtcToday),
            memberId,
            CancellationToken.None);

        var rejected = await sut.RejectAsync(expense.Id, adminId, CancellationToken.None);

        Assert.Equal(ExpenseStatus.Rejected, rejected.Status);
        Assert.Empty(context.LedgerEntries);
    }

    [Fact]
    public async Task Approve_requires_admin_role()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateExpenseService(context);

        var expense = await sut.CreateAsync(
            new CreateExpenseRequest(groupId, 100m, "Test", UtcToday),
            memberId,
            CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await sut.ApproveAsync(expense.Id, memberId, CancellationToken.None));
    }

    [Fact]
    public async Task Create_rejects_future_expense_date()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, _, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateExpenseService(context);

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await sut.CreateAsync(
                new CreateExpenseRequest(groupId, 100m, "Future", UtcToday.AddDays(1)),
                memberId,
                CancellationToken.None));
    }

    private static DateTime UtcToday => DateTime.UtcNow.Date;

    private static async Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedGroupWithMemberAsync(AppDbContext context)
    {
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "expense_owner", "expense@test.com", "Owner");
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

    private static ExpenseService CreateExpenseService(AppDbContext context)
    {
        return new ExpenseService(
            new GroupRepository(context),
            new MemberRepository(context),
            new ExpenseRepository(context),
            new LedgerService(context),
            new UnitOfWork(context),
            new CreateExpenseRequestValidator());
    }
}
