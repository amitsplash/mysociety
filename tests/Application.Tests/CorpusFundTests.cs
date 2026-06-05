using MySociety.Application.Common.Exceptions;
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

namespace MySociety.Application.Tests;

public class CorpusFundTests
{
    [Fact]
    public async Task Create_member_with_corpus_paid_does_not_credit_corpus_fund()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupAsync(context, openingCorpusBalance: 200000m);

        var memberService = TestData.CreateMemberService(context);
        var created = await memberService.CreateAsync(
            new CreateMemberRequest(
                groupId,
                "Corpus Paid",
                "9000000010",
                MemberRole.Member,
                0m,
                null,
                50000m,
                true),
            adminId,
            CancellationToken.None);

        Assert.NotNull(created.Member.CorpusPaidAt);
        Assert.Equal(50000m, created.Member.CorpusAmount);

        var funds = await CreateGroupExpenseService(context)
            .GetFundsAsync(groupId, adminId, CancellationToken.None);
        Assert.Equal(200000m, funds.Corpus.Balance);
        Assert.Equal(200000m, funds.Corpus.TotalInflows);
    }

    [Fact]
    public async Task Create_member_with_corpus_pending_does_not_credit_fund()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupAsync(context);

        var memberService = TestData.CreateMemberService(context);
        var created = await memberService.CreateAsync(
            new CreateMemberRequest(
                groupId,
                "Corpus Pending",
                "9000000011",
                MemberRole.Member,
                0m,
                null,
                25000m,
                false),
            adminId,
            CancellationToken.None);

        Assert.Null(created.Member.CorpusPaidAt);

        var funds = await CreateGroupExpenseService(context)
            .GetFundsAsync(groupId, adminId, CancellationToken.None);
        Assert.Equal(0m, funds.Corpus.Balance);
    }

    [Fact]
    public async Task Mark_corpus_received_credits_corpus_fund()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupAsync(context);

        var memberService = TestData.CreateMemberService(context);
        var created = await memberService.CreateAsync(
            new CreateMemberRequest(
                groupId,
                "Corpus Pending",
                "9000000012",
                MemberRole.Member,
                0m,
                null,
                30000m,
                false),
            adminId,
            CancellationToken.None);

        var result = await memberService.MarkCorpusReceivedAsync(
            created.Member.Id,
            adminId,
            CancellationToken.None);

        Assert.NotNull(result.Member.CorpusPaidAt);
        Assert.Equal(30000m, result.CorpusAmountAdded);
        Assert.Equal(30000m, result.CorpusFundBalance);

        await Assert.ThrowsAsync<ValidationException>(() =>
            memberService.MarkCorpusReceivedAsync(
                created.Member.Id,
                adminId,
                CancellationToken.None));
    }

    [Fact]
    public async Task Corpus_expense_deducts_from_corpus_not_maintenance()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupAsync(context, openingCorpusBalance: 100000m);

        var sut = CreateGroupExpenseService(context);
        await sut.CreateAsync(
            new CreateGroupExpenseRequest(
                groupId,
                20000m,
                "Lift upgrade",
                DateTime.UtcNow.Date,
                GroupFundType.Corpus),
            adminId,
            CancellationToken.None);

        var funds = await sut.GetFundsAsync(groupId, adminId, CancellationToken.None);
        Assert.Equal(80000m, funds.Corpus.Balance);
        Assert.Equal(0m, funds.Maintenance.Balance);
    }

    [Fact]
    public async Task Corpus_expense_rejected_when_insufficient_corpus_balance()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupAsync(context);

        var sut = CreateGroupExpenseService(context);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.CreateAsync(
                new CreateGroupExpenseRequest(
                    groupId,
                    1000m,
                    "Unauthorized corpus spend",
                    DateTime.UtcNow.Date,
                    GroupFundType.Corpus),
                adminId,
                CancellationToken.None));
    }

    private static GroupExpenseService CreateGroupExpenseService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new GroupExpenseRepository(context),
            new LedgerService(context),
            new CreateGroupExpenseRequestValidator());

    private static async Task<(Guid GroupId, Guid AdminId, Guid GroupCreatorUserId)> SeedGroupAsync(
        AppDbContext context,
        decimal openingCorpusBalance = 0m)
    {
        var groupService = TestData.CreateGroupService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "corpus_owner", "corpus@test.com", "Owner");
        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(openingCorpusBalance: openingCorpusBalance),
            CancellationToken.None);

        return (created.Group.Id, created.CreatorMember.Id, owner.Id);
    }
}
