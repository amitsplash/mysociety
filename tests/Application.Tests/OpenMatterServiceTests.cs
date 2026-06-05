using MySociety.Application.Agenda;
using MySociety.Application.Agenda.Validators;
using MySociety.Application.Committee;
using MySociety.Application.Committee.Dtos;
using MySociety.Application.Committee.Validators;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Groups.Dtos;
using MySociety.Application.Members.Dtos;
using MySociety.Application.Meetings;
using MySociety.Application.Meetings.Dtos;
using MySociety.Application.Meetings.Validators;
using MySociety.Application.OpenMatters;
using MySociety.Application.OpenMatters.Dtos;
using MySociety.Application.OpenMatters.Validators;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;

namespace MySociety.Application.Tests;

public class OpenMatterServiceTests
{
    [Fact]
    public async Task Create_open_matter_and_list_by_group()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupAsync(context);
        var sut = CreateOpenMatterService(context);

        var created = await sut.CreateAsync(
            new CreateOpenMatterRequest(groupId, "Borewell filtration", "Quotes pending"),
            adminId,
            CancellationToken.None);

        var list = await sut.GetByGroupIdAsync(groupId, adminId, OpenMatterStatus.Open, CancellationToken.None);

        Assert.Single(list);
        Assert.Equal("Borewell filtration", created.Title);
        Assert.Equal(OpenMatterStatus.Open, created.Status);
    }

    [Fact]
    public async Task Member_can_view_open_matters_but_not_create()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupAsync(context);
        var sut = CreateOpenMatterService(context);

        await sut.CreateAsync(new CreateOpenMatterRequest(groupId, "Topic A"), adminId, CancellationToken.None);

        var list = await sut.GetByGroupIdAsync(groupId, memberId, null, CancellationToken.None);
        Assert.Single(list);

        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await sut.CreateAsync(new CreateOpenMatterRequest(groupId, "Blocked"), memberId, CancellationToken.None));
    }

    private static async Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedGroupAsync(AppDbContext context)
    {
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "open_matter_owner", "om@test.com", "Owner");
        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(),
            CancellationToken.None);

        var member = await memberService.CreateAsync(
            new CreateMemberRequest(created.Group.Id, "Member One", "9000000011", MemberRole.Member, 0m, null),
            created.CreatorMember.Id,
            CancellationToken.None);

        return (created.Group.Id, created.CreatorMember.Id, member.Member.Id);
    }

    private static OpenMatterService CreateOpenMatterService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new CommitteeMemberRepository(context),
            new OpenMatterRepository(context),
            new AgendaItemRepository(context),
            new CreateOpenMatterRequestValidator(),
            new UpdateOpenMatterRequestValidator());
}
