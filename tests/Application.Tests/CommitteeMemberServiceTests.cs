using MySociety.Application.Committee;
using MySociety.Application.Committee.Dtos;
using MySociety.Application.Committee.Validators;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Groups.Dtos;
using MySociety.Application.Members.Dtos;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;

namespace MySociety.Application.Tests;

public class CommitteeMemberServiceTests
{
    [Fact]
    public async Task Admin_can_assign_committee_role_to_existing_member()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateCommitteeMemberService(context);

        var result = await sut.CreateAsync(
            new CreateCommitteeMemberRequest(groupId, memberId, CommitteeRole.Secretary),
            adminId,
            CancellationToken.None);

        Assert.Equal(CommitteeRole.Secretary, result.Role);
        Assert.Equal(memberId, result.MemberId);
    }

    [Fact]
    public async Task Duplicate_officer_role_is_rejected()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateCommitteeMemberService(context);

        await sut.CreateAsync(
            new CreateCommitteeMemberRequest(groupId, memberId, CommitteeRole.President),
            adminId,
            CancellationToken.None);

        var secondMember = await TestData.CreateMemberService(context).CreateAsync(
            new CreateMemberRequest(groupId, "Member Two", "9000000004", MemberRole.Member, 0m, null),
            adminId,
            CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(async () =>
            await sut.CreateAsync(
                new CreateCommitteeMemberRequest(groupId, secondMember.Member.Id, CommitteeRole.President),
                adminId,
                CancellationToken.None));
    }

    [Fact]
    public async Task Same_member_cannot_be_assigned_twice()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateCommitteeMemberService(context);

        await sut.CreateAsync(
            new CreateCommitteeMemberRequest(groupId, memberId, CommitteeRole.Treasurer),
            adminId,
            CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(async () =>
            await sut.CreateAsync(
                new CreateCommitteeMemberRequest(groupId, memberId, CommitteeRole.CommitteeMember),
                adminId,
                CancellationToken.None));
    }

    [Fact]
    public async Task Non_admin_cannot_manage_roster()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupWithMemberAsync(context);
        var sut = CreateCommitteeMemberService(context);

        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await sut.CreateAsync(
                new CreateCommitteeMemberRequest(groupId, memberId, CommitteeRole.Secretary),
                memberId,
                CancellationToken.None));
    }

    private static async Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedGroupWithMemberAsync(AppDbContext context)
    {
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "committee_owner", "committee@test.com", "Owner");
        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(),
            CancellationToken.None);

        var member = await memberService.CreateAsync(
            new CreateMemberRequest(created.Group.Id, "Member One", "9000000003", MemberRole.Member, 0m, null),
            created.CreatorMember.Id,
            CancellationToken.None);

        return (created.Group.Id, created.CreatorMember.Id, member.Member.Id);
    }

    private static CommitteeMemberService CreateCommitteeMemberService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new CommitteeMemberRepository(context),
            new CreateCommitteeMemberRequestValidator(),
            new UpdateCommitteeMemberRequestValidator());
}
