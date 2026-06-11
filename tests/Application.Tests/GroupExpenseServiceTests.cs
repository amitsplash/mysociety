using MySociety.Application.Common.Exceptions;
using MySociety.Application.Contributions;
using MySociety.Application.Contributions.Dtos;
using MySociety.Application.Contributions.Validators;
using MySociety.Application.Groups;
using MySociety.Application.Groups.Dtos;
using MySociety.Application.Groups.Validators;
using MySociety.Application.Members;
using MySociety.Application.Members.Dtos;
using MySociety.Application.Members.Validators;
using MySociety.Application.GroupExpenses;
using MySociety.Application.GroupExpenses.Dtos;
using MySociety.Application.GroupExpenses.Validators;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;
using MySociety.Infrastructure.Security;
using MySociety.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace MySociety.Application.Tests;

public class GroupExpenseServiceTests
{
    [Fact]
    public async Task Group_funds_balance_reflects_payments_minus_group_expenses()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var contributionService = CreateContributionService(context);

        var generated = await contributionService.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-01", "2026-01"),
            adminId,
            CancellationToken.None);
        var contribution = generated.Contributions.Single(c => c.MemberId == memberId);

        var payment = await contributionService.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, contribution.Amount, contribution.Id),
            memberId,
            CancellationToken.None);
        await contributionService.ApprovePaymentSubmissionAsync(
            payment.SubmissionId,
            adminId,
            CancellationToken.None);

        var sut = CreateGroupExpenseService(context);
        var balanceBefore = await sut.GetFundsAsync(groupId, memberId, CancellationToken.None);
        Assert.Equal(1000m, balanceBefore.Maintenance.Balance);
        Assert.Equal(1000m, balanceBefore.Maintenance.TotalInflows);

        await sut.CreateAsync(
            new CreateGroupExpenseRequest(groupId, 400m, "Lift maintenance", UtcToday),
            adminId,
            CancellationToken.None);

        var balanceAfter = await sut.GetFundsAsync(groupId, memberId, CancellationToken.None);
        Assert.Equal(600m, balanceAfter.Maintenance.Balance);
        Assert.Equal(400m, balanceAfter.Maintenance.TotalOutflows);
    }

    [Fact]
    public async Task Create_group_expense_rejects_when_insufficient_balance()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateGroupExpenseService(context);

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await sut.CreateAsync(
                new CreateGroupExpenseRequest(groupId, 100m, "Office rent", UtcToday),
                adminId,
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_group_expense_requires_admin()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var contributionService = CreateContributionService(context);

        var generated = await contributionService.GenerateAsync(
            new GenerateContributionsRequest(groupId, "2026-01", "2026-01"),
            adminId,
            CancellationToken.None);
        var contribution = generated.Contributions.Single(c => c.MemberId == memberId);

        await contributionService.RecordPaymentAsync(
            new RecordPaymentRequest(memberId, contribution.Amount, contribution.Id),
            memberId,
            CancellationToken.None);

        var sut = CreateGroupExpenseService(context);

        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await sut.CreateAsync(
                new CreateGroupExpenseRequest(groupId, 100m, "Unauthorized spend", UtcToday),
                memberId,
                CancellationToken.None));
    }

    private static DateTime UtcToday => DateTime.UtcNow.Date;

    private static GroupExpenseService CreateGroupExpenseService(AppDbContext context)
    {
        return new GroupExpenseService(
            new GroupRepository(context),
            new MemberRepository(context),
            new GroupExpenseRepository(context),
            new LedgerService(context),
            new CreateGroupExpenseRequestValidator());
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

    private static async Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedGroupWithMemberAsync(
        AppDbContext context)
    {
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "society_owner", "society@test.com", "Owner");
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
}
