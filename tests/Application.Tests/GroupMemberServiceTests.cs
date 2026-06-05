using MySociety.Application.Common.Exceptions;
using ValidationException = MySociety.Application.Common.Exceptions.ValidationException;
using MySociety.Application.Groups.Dtos;
using MySociety.Application.Members.Dtos;
using MySociety.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Application.Tests;

public class GroupMemberServiceTests
{
    [Fact]
    public async Task CreateGroup_creates_group_and_makes_creator_admin()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var owner = await TestData.AddRegisteredUserAsync(context, "owner1", "owner1@test.com", "Owner");
        var sut = TestData.CreateGroupService(context);

        var result = await sut.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(),
            CancellationToken.None);

        Assert.Equal("Test Group", result.Group.Name);
        Assert.Equal(MemberRole.Admin, result.CreatorMember.Role);
        var creatorMember = context.Members.Single(m => m.Id == result.CreatorMember.Id);
        Assert.Equal(owner.Id, creatorMember.UserId);
        Assert.Equal(1, context.Members.Count());
        Assert.Equal(1, context.Members.Count(m => m.UserId == owner.Id));
    }

    [Fact]
    public async Task CreateGroup_persists_opening_group_funds_balance()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var owner = await TestData.AddRegisteredUserAsync(context, "owner2", "owner2@test.com", "Owner");
        var sut = TestData.CreateGroupService(context);

        var result = await sut.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(name: "Funded RWA", openingMaintenanceBalance: 15000m),
            CancellationToken.None);

        Assert.Equal(15000m, result.Group.OpeningMaintenanceBalance);
    }

    [Fact]
    public async Task UpdateGroup_requires_admin_role()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "owner3", "owner3@test.com", "Owner");

        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(),
            CancellationToken.None);

        var regularMember = await memberService.CreateAsync(new CreateMemberRequest(
            created.Group.Id,
            "Regular Member",
            "9000000003",
            MemberRole.Member,
            0m,
            null), created.CreatorMember.Id, CancellationToken.None);

        var regularUser = await context.Users.SingleAsync(u => u.Phone == "9000000003");
        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await groupService.UpdateAsync(
                created.Group.Id,
                new UpdateGroupRequest("Updated", GroupType.Club, ContributionModel.Fixed, 1200m, ContributionFrequency.Monthly),
                regularUser.Id,
                regularMember.Member.Id,
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateMember_records_opening_balance_in_ledger()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "owner4", "owner4@test.com", "Owner");

        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(),
            CancellationToken.None);

        var member = await memberService.CreateAsync(new CreateMemberRequest(
            created.Group.Id,
            "New Member",
            "9333333333",
            MemberRole.Member,
            -500m,
            null), created.CreatorMember.Id, CancellationToken.None);

        var ledgerEntry = context.LedgerEntries.Single(x => x.MemberId == member.Member.Id);
        Assert.Equal(LedgerEntryType.OpeningBalance, ledgerEntry.Type);
        Assert.Equal(LedgerEntryDirection.Debit, ledgerEntry.Direction);
        Assert.Equal(500m, ledgerEntry.Amount);
    }

    [Fact]
    public async Task DeleteGroup_requires_group_admin()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "owner5", "owner5@test.com", "Owner");

        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(),
            CancellationToken.None);

        var regularMember = await memberService.CreateAsync(new CreateMemberRequest(
            created.Group.Id,
            "Regular Member",
            "9888888888",
            MemberRole.Member,
            0m,
            null), created.CreatorMember.Id, CancellationToken.None);

        var regularUser = await context.Users.SingleAsync(u => u.Phone == "9888888888");
        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await groupService.DeleteAsync(created.Group.Id, regularUser.Id, regularMember.Member.Id, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteGroup_removes_group_and_members()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "owner6", "owner6@test.com", "Owner");

        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(),
            CancellationToken.None);

        await memberService.CreateAsync(new CreateMemberRequest(
            created.Group.Id,
            "Member Two",
            "9222222222",
            MemberRole.Member,
            0m,
            null), created.CreatorMember.Id, CancellationToken.None);

        await groupService.DeleteAsync(created.Group.Id, owner.Id, created.CreatorMember.Id, CancellationToken.None);

        Assert.Empty(context.Groups);
        Assert.Empty(context.Members);
    }
}
