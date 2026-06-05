using MySociety.Application.Agenda;
using MySociety.Application.Agenda.Dtos;
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
using MySociety.Application.GroupDecisions;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;

namespace MySociety.Application.Tests;

public class MeetingServiceTests
{
    [Fact]
    public async Task Member_sees_only_published_meetings()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupAsync(context);
        var meetingService = CreateMeetingService(context);

        var draft = await meetingService.CreateAsync(
            new CreateMeetingRequest(groupId, "Draft Meeting", UtcToday, Status: MeetingStatus.Draft),
            adminId,
            CancellationToken.None);

        await meetingService.UpdateStatusAsync(
            groupId,
            draft.Id,
            new UpdateMeetingStatusRequest(MeetingStatus.Published),
            adminId,
            CancellationToken.None);

        await meetingService.CreateAsync(
            new CreateMeetingRequest(groupId, "Still Draft", UtcToday, Status: MeetingStatus.Draft),
            adminId,
            CancellationToken.None);

        var memberList = await meetingService.GetByGroupIdAsync(groupId, memberId, null, CancellationToken.None);
        Assert.Single(memberList);
        Assert.Equal("Draft Meeting", memberList[0].Title);
        Assert.Equal(MeetingStatus.Published, memberList[0].Status);
    }

    [Fact]
    public async Task Member_cannot_view_draft_meeting_detail()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupAsync(context);
        var meetingService = CreateMeetingService(context);

        var draft = await meetingService.CreateAsync(
            new CreateMeetingRequest(groupId, "Secret Draft", UtcToday, Status: MeetingStatus.Draft),
            adminId,
            CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await meetingService.GetByIdAsync(groupId, draft.Id, memberId, CancellationToken.None));
    }

    [Fact]
    public async Task Agenda_from_backlog_and_outcome_updates_open_matter()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupAsync(context);
        var openMatterService = CreateOpenMatterService(context);
        var meetingService = CreateMeetingService(context);
        var agendaService = CreateAgendaService(context);

        var matter = await openMatterService.CreateAsync(
            new CreateOpenMatterRequest(groupId, "Parking rules"),
            adminId,
            CancellationToken.None);

        var meeting = await meetingService.CreateAsync(
            new CreateMeetingRequest(groupId, "Monthly Meeting", UtcToday, Status: MeetingStatus.Draft),
            adminId,
            CancellationToken.None);

        var agendaItem = await agendaService.AddFromOpenMatterAsync(
            groupId, meeting.Id, matter.Id, adminId, CancellationToken.None);

        Assert.Equal(matter.Id, agendaItem.OpenMatterId);
        Assert.Equal(AgendaItemSource.FromBacklog, agendaItem.Source);

        await agendaService.UpdateOutcomeAsync(
            groupId,
            agendaItem.Id,
            new UpdateAgendaOutcomeRequest(MeetingItemOutcome.Postponed, "Need vendor quotes"),
            adminId,
            CancellationToken.None);

        var matterAfter = await openMatterService.GetByIdAsync(groupId, matter.Id, adminId, CancellationToken.None);
        Assert.Equal(OpenMatterStatus.Open, matterAfter.Status);
        Assert.Equal(meeting.Id, matterAfter.LastDiscussedInMeetingId);
    }

    [Fact]
    public async Task Finalized_outcome_closes_open_matter()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupAsync(context);
        var openMatterService = CreateOpenMatterService(context);
        var meetingService = CreateMeetingService(context);
        var agendaService = CreateAgendaService(context);

        var matter = await openMatterService.CreateAsync(
            new CreateOpenMatterRequest(groupId, "Resolved topic"),
            adminId,
            CancellationToken.None);

        var meeting = await meetingService.CreateAsync(
            new CreateMeetingRequest(groupId, "Meeting", UtcToday, Status: MeetingStatus.Draft),
            adminId,
            CancellationToken.None);

        var agendaItem = await agendaService.AddFromOpenMatterAsync(
            groupId, meeting.Id, matter.Id, adminId, CancellationToken.None);

        await agendaService.UpdateOutcomeAsync(
            groupId,
            agendaItem.Id,
            new UpdateAgendaOutcomeRequest(MeetingItemOutcome.Finalized),
            adminId,
            CancellationToken.None);

        var matterAfter = await openMatterService.GetByIdAsync(groupId, matter.Id, adminId, CancellationToken.None);
        Assert.Equal(OpenMatterStatus.Finalized, matterAfter.Status);
    }

    private static DateTime UtcToday => DateTime.UtcNow.Date;

    private static async Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedGroupAsync(AppDbContext context)
    {
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "meeting_owner", "meeting@test.com", "Owner");
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

    private static OpenMatterService CreateOpenMatterService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new CommitteeMemberRepository(context),
            new OpenMatterRepository(context),
            new AgendaItemRepository(context),
            new CreateOpenMatterRequestValidator(),
            new UpdateOpenMatterRequestValidator());

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
}
