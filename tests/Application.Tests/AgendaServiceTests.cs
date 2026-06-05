using MySociety.Application.Agenda;
using MySociety.Application.Agenda.Dtos;
using MySociety.Application.Agenda.Validators;
using MySociety.Application.Committee;
using MySociety.Application.Groups.Dtos;
using MySociety.Application.Meetings;
using MySociety.Application.Meetings.Dtos;
using MySociety.Application.Meetings.Validators;
using MySociety.Application.Members.Dtos;
using MySociety.Application.OpenMatters;
using MySociety.Application.OpenMatters.Dtos;
using MySociety.Application.OpenMatters.Validators;
using MySociety.Application.GroupDecisions;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;

namespace MySociety.Application.Tests;

public class AgendaServiceTests
{
    [Fact]
    public async Task Ad_hoc_agenda_item_is_linked_to_open_matters_backlog()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupAsync(context);
        var meeting = await CreateDraftMeetingAsync(context, groupId, adminId);
        var agendaService = CreateAgendaService(context);
        var openMatterService = CreateOpenMatterService(context);

        var item = await agendaService.AddAsync(
            groupId,
            meeting.Id,
            new CreateAgendaItemRequest("New parking rule", Source: AgendaItemSource.AdHoc),
            adminId,
            CancellationToken.None);

        Assert.NotNull(item.OpenMatterId);
        Assert.Equal("New parking rule", item.OpenMatterTitle);

        var matters = await openMatterService.GetByGroupIdAsync(
            groupId, adminId, OpenMatterStatus.Open, CancellationToken.None);
        Assert.Contains(matters, m => m.Id == item.OpenMatterId && m.Title == "New parking rule");
    }

    private static async Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedGroupAsync(AppDbContext context)
    {
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "agenda_owner", "agenda@test.com", "Owner");
        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(),
            CancellationToken.None);

        var member = await memberService.CreateAsync(
            new CreateMemberRequest(created.Group.Id, "Member One", "9000000033", MemberRole.Member, 0m, null),
            created.CreatorMember.Id,
            CancellationToken.None);

        return (created.Group.Id, created.CreatorMember.Id, member.Member.Id);
    }

    private static async Task<MeetingDetailResponse> CreateDraftMeetingAsync(
        AppDbContext context,
        Guid groupId,
        Guid adminId)
    {
        var meetingService = CreateMeetingService(context);
        return await meetingService.CreateAsync(
            new CreateMeetingRequest(groupId, "Committee Meeting", DateTime.UtcNow.Date, Status: MeetingStatus.Draft),
            adminId,
            CancellationToken.None);
    }

    private static MeetingService CreateMeetingService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new CommitteeMemberRepository(context),
            new MeetingRepository(context),
            new MeetingAttendeeRepository(context),
            new GroupDecisionService(
                new GroupRepository(context),
                new MemberRepository(context),
                new CommitteeMemberRepository(context),
                new GroupDecisionRepository(context)),
            new CreateMeetingRequestValidator(),
            new UpdateMeetingRequestValidator(),
            new UpdateMeetingStatusRequestValidator());

    private static AgendaService CreateAgendaService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new CommitteeMemberRepository(context),
            new MeetingRepository(context),
            new OpenMatterRepository(context),
            new AgendaItemRepository(context),
            new CreateAgendaItemRequestValidator(),
            new UpdateAgendaItemRequestValidator(),
            new UpdateAgendaOutcomeRequestValidator());

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
