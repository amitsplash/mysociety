using MySociety.Application.Common.Authorization;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Common.Interfaces;
using MySociety.Application.GroupDecisions.Dtos;
using MySociety.Application.Meetings;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;

namespace MySociety.Application.GroupDecisions;

public interface IGroupDecisionService
{
    Task<IReadOnlyList<GroupDecisionResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        GroupDecisionFilter? filter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GroupDecisionResponse>> GetByMeetingIdAsync(
        Guid groupId,
        Guid meetingId,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public class GroupDecisionService : IGroupDecisionService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ICommitteeMemberRepository _committeeMemberRepository;
    private readonly IGroupDecisionRepository _groupDecisionRepository;

    public GroupDecisionService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        ICommitteeMemberRepository committeeMemberRepository,
        IGroupDecisionRepository groupDecisionRepository)
    {
        _groupRepository = groupRepository;
        _memberRepository = memberRepository;
        _committeeMemberRepository = committeeMemberRepository;
        _groupDecisionRepository = groupDecisionRepository;
    }

    public async Task<IReadOnlyList<GroupDecisionResponse>> GetByGroupIdAsync(
        Guid groupId,
        Guid actingMemberId,
        GroupDecisionFilter? filter,
        CancellationToken cancellationToken)
    {
        await EnsureGroupMemberAsync(groupId, actingMemberId, cancellationToken);
        var canManage = await MeetingVisibility.CanManageMeetingsAsync(
            _memberRepository, _committeeMemberRepository, actingMemberId, groupId, cancellationToken);

        var agendaItems = await _groupDecisionRepository.GetAgendaItemsWithDecisionsAsync(
            groupId, null, cancellationToken);
        var resolutions = await _groupDecisionRepository.GetResolutionsAsync(groupId, null, cancellationToken);

        return ApplyFilter(BuildFeed(agendaItems, resolutions, canManage), filter);
    }

    public async Task<IReadOnlyList<GroupDecisionResponse>> GetByMeetingIdAsync(
        Guid groupId,
        Guid meetingId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        await EnsureGroupMemberAsync(groupId, actingMemberId, cancellationToken);
        var canManage = await MeetingVisibility.CanManageMeetingsAsync(
            _memberRepository, _committeeMemberRepository, actingMemberId, groupId, cancellationToken);

        var agendaItems = await _groupDecisionRepository.GetAgendaItemsWithDecisionsAsync(
            groupId, meetingId, cancellationToken);
        var resolutions = await _groupDecisionRepository.GetResolutionsAsync(groupId, meetingId, cancellationToken);

        if (agendaItems.Count == 0 && resolutions.Count == 0)
        {
            return [];
        }

        var meeting = agendaItems.FirstOrDefault()?.Meeting ?? resolutions.First().Meeting;
        if (!MeetingVisibility.CanMemberView(meeting, canManage))
        {
            throw new ForbiddenException("This meeting is not published yet.");
        }

        return BuildFeed(agendaItems, resolutions, canManage);
    }

    internal static IReadOnlyList<GroupDecisionResponse> BuildFeed(
        IReadOnlyList<AgendaItem> agendaItems,
        IReadOnlyList<Resolution> resolutions,
        bool canManageMeetings)
    {
        var decisions = new List<GroupDecisionResponse>();
        var resolutionByAgenda = resolutions
            .Where(r => r.AgendaItemId.HasValue)
            .GroupBy(r => r.AgendaItemId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var coveredAgendaIds = new HashSet<Guid>();

        foreach (var item in agendaItems)
        {
            if (!MeetingVisibility.CanMemberView(item.Meeting, canManageMeetings))
            {
                continue;
            }

            var minute = item.Minute;
            if (minute is null || string.IsNullOrWhiteSpace(minute.DecisionTaken))
            {
                continue;
            }

            resolutionByAgenda.TryGetValue(item.Id, out var linkedResolution);
            coveredAgendaIds.Add(item.Id);

            decisions.Add(MapFromMinute(item, minute, linkedResolution));
        }

        foreach (var resolution in resolutions)
        {
            if (!MeetingVisibility.CanMemberView(resolution.Meeting, canManageMeetings))
            {
                continue;
            }

            if (resolution.AgendaItemId.HasValue && coveredAgendaIds.Contains(resolution.AgendaItemId.Value))
            {
                continue;
            }

            decisions.Add(MapFromResolution(resolution));
        }

        return decisions
            .OrderByDescending(x => x.DecidedAt)
            .ThenByDescending(x => x.MeetingDate)
            .ToList();
    }

    private static GroupDecisionResponse MapFromMinute(
        AgendaItem item,
        Minute minute,
        Resolution? linkedResolution)
    {
        var meeting = item.Meeting;
        var isDraft = !MeetingVisibility.MemberVisibleStatuses.Contains(meeting.Status);

        return new GroupDecisionResponse(
            Id: linkedResolution?.Id ?? minute.Id,
            Source: GroupDecisionSource.Minutes,
            DecisionText: minute.DecisionTaken!.Trim(),
            ResolutionNumber: linkedResolution?.ResolutionNumber,
            ResolutionId: linkedResolution?.Id,
            MeetingId: meeting.Id,
            MeetingTitle: meeting.Title,
            MeetingStatus: meeting.Status,
            MeetingDate: meeting.MeetingDate,
            IsDraft: isDraft,
            AgendaItemId: item.Id,
            TopicTitle: item.Title,
            OpenMatterId: item.OpenMatterId,
            ApprovedBudget: minute.BudgetApproved ?? linkedResolution?.ApprovedBudget,
            Outcome: item.Outcome,
            ResolutionStatus: linkedResolution?.Status,
            DecidedAt: linkedResolution?.ResolutionDate ?? meeting.MeetingDate);
    }

    private static GroupDecisionResponse MapFromResolution(Resolution resolution)
    {
        var meeting = resolution.Meeting;
        var isDraft = !MeetingVisibility.MemberVisibleStatuses.Contains(meeting.Status);
        var decisionText = !string.IsNullOrWhiteSpace(resolution.Description)
            ? resolution.Description.Trim()
            : resolution.Title;

        return new GroupDecisionResponse(
            Id: resolution.Id,
            Source: GroupDecisionSource.FormalResolution,
            DecisionText: decisionText,
            ResolutionNumber: resolution.ResolutionNumber,
            ResolutionId: resolution.Id,
            MeetingId: meeting.Id,
            MeetingTitle: meeting.Title,
            MeetingStatus: meeting.Status,
            MeetingDate: meeting.MeetingDate,
            IsDraft: isDraft,
            AgendaItemId: resolution.AgendaItemId,
            TopicTitle: resolution.AgendaItem?.Title ?? resolution.Title,
            OpenMatterId: resolution.OpenMatterId,
            ApprovedBudget: resolution.ApprovedBudget,
            Outcome: resolution.AgendaItem?.Outcome,
            ResolutionStatus: resolution.Status,
            DecidedAt: resolution.ResolutionDate);
    }

    private static IReadOnlyList<GroupDecisionResponse> ApplyFilter(
        IReadOnlyList<GroupDecisionResponse> decisions,
        GroupDecisionFilter? filter)
    {
        if (filter == GroupDecisionFilter.HasBudget)
        {
            return decisions.Where(x => x.ApprovedBudget.HasValue).ToList();
        }

        return decisions;
    }

    private async Task EnsureGroupMemberAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        _ = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        await MemberAuthorization.EnsureGroupMemberAsync(
            _memberRepository, actingMemberId, groupId, cancellationToken);
    }
}
