using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Exceptions;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Services;

namespace MySociety.Application.Tests;

public class LedgerServiceTests
{
    [Fact]
    public async Task Opening_balance_positive_creates_credit_entry()
    {
        await using var context = await CreateDbContextAsync();
        var member = await SeedMemberAsync(context);
        var sut = new LedgerService(context);

        await sut.RecordOpeningBalanceAsync(member.Id, member.GroupId, 500m, CancellationToken.None);

        var entry = await context.LedgerEntries.SingleAsync();
        Assert.Equal(LedgerEntryType.OpeningBalance, entry.Type);
        Assert.Equal(LedgerEntryDirection.Credit, entry.Direction);
        Assert.Equal(500m, entry.Amount);
    }

    [Fact]
    public async Task Opening_balance_negative_creates_debit_entry()
    {
        await using var context = await CreateDbContextAsync();
        var member = await SeedMemberAsync(context);
        var sut = new LedgerService(context);

        await sut.RecordOpeningBalanceAsync(member.Id, member.GroupId, -1250m, CancellationToken.None);

        var entry = await context.LedgerEntries.SingleAsync();
        Assert.Equal(LedgerEntryType.OpeningBalance, entry.Type);
        Assert.Equal(LedgerEntryDirection.Debit, entry.Direction);
        Assert.Equal(1250m, entry.Amount);
    }

    [Fact]
    public async Task Contribution_payment_and_expense_entries_compute_balance_correctly()
    {
        await using var context = await CreateDbContextAsync();
        var member = await SeedMemberAsync(context);
        var sut = new LedgerService(context);

        await sut.RecordContributionDebitAsync(member.Id, member.GroupId, Guid.NewGuid(), 2000m, CancellationToken.None);
        await sut.RecordPaymentCreditAsync(member.Id, member.GroupId, Guid.NewGuid(), 1500m, CancellationToken.None);
        await sut.RecordExpenseCreditAsync(member.Id, member.GroupId, Guid.NewGuid(), 500m, CancellationToken.None);

        var balance = await sut.GetBalanceAsync(member.Id, member.GroupId, CancellationToken.None);
        Assert.Equal(0m, balance);
    }

    [Fact]
    public async Task Duplicate_reference_for_same_member_and_entry_type_is_rejected()
    {
        await using var context = await CreateDbContextAsync();
        var member = await SeedMemberAsync(context);
        var sut = new LedgerService(context);
        var referenceId = Guid.NewGuid();

        await sut.RecordContributionDebitAsync(member.Id, member.GroupId, referenceId, 500m, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(async () =>
            await sut.RecordContributionDebitAsync(member.Id, member.GroupId, referenceId, 500m, CancellationToken.None));
    }

    private static async Task<AppDbContext> CreateDbContextAsync()
    {
        return await TestDbContextFactory.CreateAsync();
    }

    private static async Task<Member> SeedMemberAsync(AppDbContext context)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "ledger_test_user",
            Email = "ledger_test_user@test.com",
            Name = "Test User",
            Phone = "9999999999",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Test Group",
            Type = GroupType.Friends,
            ContributionModel = ContributionModel.Fixed,
            ContributionAmount = 1000m,
            ContributionFrequency = ContributionFrequency.Monthly,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var member = new Member
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = user.Id,
            Role = MemberRole.Member,
            CreatedAt = DateTime.UtcNow
        };

        context.Groups.Add(group);
        context.Users.Add(user);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        return member;
    }
}
