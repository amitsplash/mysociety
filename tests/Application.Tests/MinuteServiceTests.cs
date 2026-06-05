using MySociety.Application.Agenda;
using MySociety.Application.Agenda.Validators;
using MySociety.Application.Committee;
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
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;

namespace MySociety.Application.Tests;

public class MinuteServiceTests
{
    [Fact]
    public async Task Upsert_minute_records_decision_and_syncs_discussion()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupAsync(context);
        var meeting = await CreateDraftMeetingAsync(context, groupId, adminId);
        var agenda = await CreateAgendaService(context).AddAsync(
            groupId,
            meeting.Id,
            new Agenda.Dtos.CreateAgendaItemRequest("Budget review", Source: AgendaItemSource.AdHoc),
            adminId,
            CancellationToken.None);

        var minuteService = CreateMinuteService(context);
        var updated = await minuteService.UpsertAsync(
            groupId,
            agenda.Id,
            new UpsertMinuteRequest(
                DiscussionSummary: "Reviewed quotes from three vendors",
                DecisionTaken: "Approve vendor A at quoted rate",
                BudgetApproved: 250000m),
            adminId,
            CancellationToken.None);

        Assert.Equal("Reviewed quotes from three vendors", updated.DiscussionSummary);
        Assert.NotNull(updated.Minute);
        Assert.Equal("Approve vendor A at quoted rate", updated.Minute!.DecisionTaken);
        Assert.Equal(250000m, updated.Minute.BudgetApproved);
    }

    private static async Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedGroupAsync(AppDbContext context)
    {
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "minute_owner", "minute@test.com", "Owner");
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
}
