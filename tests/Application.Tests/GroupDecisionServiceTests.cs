using MySociety.Application.Agenda;
using MySociety.Application.Agenda.Dtos;
using MySociety.Application.Agenda.Validators;
using MySociety.Application.Committee;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Groups.Dtos;
using MySociety.Application.Meetings;
using MySociety.Application.Meetings.Dtos;
using MySociety.Application.Meetings.Validators;
using MySociety.Application.Members.Dtos;
using MySociety.Application.Minutes;
using MySociety.Application.Minutes.Dtos;
using MySociety.Application.Minutes.Validators;
using MySociety.Application.OpenMatters;
using MySociety.Application.OpenMatters.Dtos;
using MySociety.Application.OpenMatters.Validators;
using MySociety.Application.GroupDecisions;
using MySociety.Application.GroupDecisions.Dtos;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;

namespace MySociety.Application.Tests;

public class GroupDecisionServiceTests
{
    [Fact]
    public async Task Saving_minute_decision_creates_resolution_and_appears_in_feed()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupAsync(context);
        var meeting = await CreateDraftMeetingAsync(context, groupId, adminId);
        var agendaService = CreateAgendaService(context);
        var minuteService = CreateMinuteService(context);
        var decisionService = CreateGroupDecisionService(context);

        var item = await agendaService.AddAsync(
            groupId,
            meeting.Id,
            new CreateAgendaItemRequest("Approve vendor", Source: AgendaItemSource.AdHoc),
            adminId,
            CancellationToken.None);

        await minuteService.UpsertAsync(
            groupId,
            item.Id,
            new UpsertMinuteRequest(DecisionTaken: "Approved vendor A at quoted rate", BudgetApproved: 100000m),
            adminId,
            CancellationToken.None);

        var feed = await decisionService.GetByGroupIdAsync(groupId, adminId, null, CancellationToken.None);
        Assert.Single(feed);
        Assert.Equal("Approved vendor A at quoted rate", feed[0].DecisionText);
        Assert.NotNull(feed[0].ResolutionNumber);
        Assert.StartsWith("RES-", feed[0].ResolutionNumber);
    }

    [Fact]
    public async Task Member_feed_excludes_draft_meeting_decisions()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupAsync(context);
        var meetingService = CreateMeetingService(context);
        var agendaService = CreateAgendaService(context);
        var minuteService = CreateMinuteService(context);
        var decisionService = CreateGroupDecisionService(context);

        var meeting = await meetingService.CreateAsync(
            new CreateMeetingRequest(groupId, "Draft", DateTime.UtcNow.Date, Status: MeetingStatus.Draft),
            adminId,
            CancellationToken.None);

        var item = await agendaService.AddAsync(
            groupId,
            meeting.Id,
            new CreateAgendaItemRequest("Secret decision topic", Source: AgendaItemSource.AdHoc),
            adminId,
            CancellationToken.None);

        await minuteService.UpsertAsync(
            groupId,
            item.Id,
            new UpsertMinuteRequest(DecisionTaken: "Secret decision"),
            adminId,
            CancellationToken.None);

        var memberFeed = await decisionService.GetByGroupIdAsync(groupId, memberId, null, CancellationToken.None);
        Assert.Empty(memberFeed);

        var adminFeed = await decisionService.GetByGroupIdAsync(groupId, adminId, null, CancellationToken.None);
        Assert.Single(adminFeed);
        Assert.True(adminFeed[0].IsDraft);
    }

    [Fact]
    public async Task Publish_meeting_makes_decision_visible_to_member()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupAsync(context);
        var meetingService = CreateMeetingService(context);
        var agendaService = CreateAgendaService(context);
        var minuteService = CreateMinuteService(context);
        var decisionService = CreateGroupDecisionService(context);

        var meeting = await meetingService.CreateAsync(
            new CreateMeetingRequest(groupId, "Monthly", DateTime.UtcNow.Date, Status: MeetingStatus.Draft),
            adminId,
            CancellationToken.None);

        var item = await agendaService.AddAsync(
            groupId,
            meeting.Id,
            new CreateAgendaItemRequest("Borewell", Source: AgendaItemSource.AdHoc),
            adminId,
            CancellationToken.None);

        await minuteService.UpsertAsync(
            groupId,
            item.Id,
            new UpsertMinuteRequest(DecisionTaken: "Approved borewell vendor"),
            adminId,
            CancellationToken.None);

        await meetingService.UpdateStatusAsync(
            groupId,
            meeting.Id,
            new UpdateMeetingStatusRequest(MeetingStatus.Published),
            adminId,
            CancellationToken.None);

        var memberFeed = await decisionService.GetByGroupIdAsync(groupId, memberId, null, CancellationToken.None);
        Assert.Single(memberFeed);
        Assert.False(memberFeed[0].IsDraft);
        Assert.Equal("Approved borewell vendor", memberFeed[0].DecisionText);
    }

    [Fact]
    public async Task Feed_deduplicates_minute_and_linked_resolution()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupAsync(context);
        var meeting = await CreateDraftMeetingAsync(context, groupId, adminId);
        var agendaService = CreateAgendaService(context);
        var minuteService = CreateMinuteService(context);
        var decisionService = CreateGroupDecisionService(context);

        var item = await agendaService.AddAsync(
            groupId,
            meeting.Id,
            new CreateAgendaItemRequest("Security contract", Source: AgendaItemSource.AdHoc),
            adminId,
            CancellationToken.None);

        await minuteService.UpsertAsync(
            groupId,
            item.Id,
            new UpsertMinuteRequest(DecisionTaken: "Renew security agency for 1 year"),
            adminId,
            CancellationToken.None);

        var feed = await decisionService.GetByGroupIdAsync(groupId, adminId, null, CancellationToken.None);
        Assert.Single(feed);
        Assert.Equal(GroupDecisionSource.Minutes, feed[0].Source);
        Assert.NotNull(feed[0].ResolutionNumber);
    }

    private static async Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedGroupAsync(AppDbContext context)
    {
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "decision_owner", "decision@test.com", "Owner");
        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(),
            CancellationToken.None);

        var member = await memberService.CreateAsync(
            new CreateMemberRequest(created.Group.Id, "Member One", "9000000044", MemberRole.Member, 0m, null),
            created.CreatorMember.Id,
            CancellationToken.None);

        return (created.Group.Id, created.CreatorMember.Id, member.Member.Id);
    }

    private static async Task<MeetingDetailResponse> CreateDraftMeetingAsync(
        AppDbContext context,
        Guid groupId,
        Guid adminId)
    {
        return await CreateMeetingService(context).CreateAsync(
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
            CreateGroupDecisionService(context),
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

    private static MinuteService CreateMinuteService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new CommitteeMemberRepository(context),
            new AgendaItemRepository(context),
            new MeetingRepository(context),
            new MinuteRepository(context),
            new ResolutionRepository(context),
            new UpsertMinuteRequestValidator());

    private static GroupDecisionService CreateGroupDecisionService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new CommitteeMemberRepository(context),
            new GroupDecisionRepository(context));
}
