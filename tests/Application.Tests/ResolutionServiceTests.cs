using MySociety.Application.Agenda;
using MySociety.Application.Agenda.Validators;
using MySociety.Application.Committee;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Groups.Dtos;
using MySociety.Application.Meetings;
using MySociety.Application.Meetings.Dtos;
using MySociety.Application.Meetings.Validators;
using MySociety.Application.Members.Dtos;
using MySociety.Application.OpenMatters;
using MySociety.Application.OpenMatters.Dtos;
using MySociety.Application.OpenMatters.Validators;
using MySociety.Application.Resolutions;
using MySociety.Application.Resolutions.Dtos;
using MySociety.Application.Resolutions.Validators;
using MySociety.Application.GroupDecisions;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;
using MySociety.Infrastructure.Repositories;

namespace MySociety.Application.Tests;

public class ResolutionServiceTests
{
    [Fact]
    public async Task Resolution_numbers_increment_per_year()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, _) = await SeedGroupAsync(context);
        var meeting = await CreateDraftMeetingAsync(context, groupId, adminId);
        var resolutionService = CreateResolutionService(context);

        var first = await resolutionService.CreateAsync(
            new CreateResolutionRequest(groupId, meeting.Id, "Install filtration"),
            adminId,
            CancellationToken.None);

        var second = await resolutionService.CreateAsync(
            new CreateResolutionRequest(groupId, meeting.Id, "Appoint security agency"),
            adminId,
            CancellationToken.None);

        var year = DateTime.UtcNow.Year;
        Assert.Equal($"RES-{year}-001", first.ResolutionNumber);
        Assert.Equal($"RES-{year}-002", second.ResolutionNumber);
    }

    [Fact]
    public async Task Member_sees_only_resolutions_from_published_meetings()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupAsync(context);
        var meetingService = CreateMeetingService(context);
        var resolutionService = CreateResolutionService(context);

        var draftMeeting = await meetingService.CreateAsync(
            new CreateMeetingRequest(groupId, "Draft", DateTime.UtcNow.Date, Status: MeetingStatus.Draft),
            adminId,
            CancellationToken.None);

        await resolutionService.CreateAsync(
            new CreateResolutionRequest(groupId, draftMeeting.Id, "Secret draft decision"),
            adminId,
            CancellationToken.None);

        var publishedMeeting = await meetingService.CreateAsync(
            new CreateMeetingRequest(groupId, "Published", DateTime.UtcNow.Date, Status: MeetingStatus.Draft),
            adminId,
            CancellationToken.None);

        await resolutionService.CreateAsync(
            new CreateResolutionRequest(groupId, publishedMeeting.Id, "Public decision"),
            adminId,
            CancellationToken.None);

        await meetingService.UpdateStatusAsync(
            groupId,
            publishedMeeting.Id,
            new UpdateMeetingStatusRequest(MeetingStatus.Published),
            adminId,
            CancellationToken.None);

        var memberList = await resolutionService.GetByGroupIdAsync(
            groupId, memberId, null, CancellationToken.None);

        Assert.Single(memberList);
        Assert.Equal("Public decision", memberList[0].Title);
    }

    [Fact]
    public async Task Member_cannot_view_draft_meeting_resolution_detail()
    {
        await using var context = await TestDbContextFactory.CreateAsync();
        var (groupId, adminId, memberId) = await SeedGroupAsync(context);
        var meeting = await CreateDraftMeetingAsync(context, groupId, adminId);
        var resolutionService = CreateResolutionService(context);

        var resolution = await resolutionService.CreateAsync(
            new CreateResolutionRequest(groupId, meeting.Id, "Draft only"),
            adminId,
            CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await resolutionService.GetByIdAsync(groupId, resolution.Id, memberId, CancellationToken.None));
    }

    private static async Task<(Guid GroupId, Guid AdminId, Guid MemberId)> SeedGroupAsync(AppDbContext context)
    {
        var groupService = TestData.CreateGroupService(context);
        var memberService = TestData.CreateMemberService(context);
        var owner = await TestData.AddRegisteredUserAsync(context, "resolution_owner", "resolution@test.com", "Owner");
        var created = await groupService.CreateAsync(
            owner.Id,
            TestData.DefaultCreateGroupRequest(),
            CancellationToken.None);

        var member = await memberService.CreateAsync(
            new CreateMemberRequest(created.Group.Id, "Member One", "9000000022", MemberRole.Member, 0m, null),
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

    private static ResolutionService CreateResolutionService(AppDbContext context) =>
        new(
            new GroupRepository(context),
            new MemberRepository(context),
            new CommitteeMemberRepository(context),
            new MeetingRepository(context),
            new AgendaItemRepository(context),
            new ResolutionRepository(context),
            new CreateResolutionRequestValidator(),
            new UpdateResolutionRequestValidator());
}
