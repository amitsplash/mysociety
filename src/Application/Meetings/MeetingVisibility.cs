using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;

namespace MySociety.Application.Meetings;

public static class MeetingVisibility
{
    public static readonly MeetingStatus[] MemberVisibleStatuses =
    [
        MeetingStatus.Approved,
        MeetingStatus.Published,
        MeetingStatus.Archived
    ];

    public static async Task<bool> CanManageMeetingsAsync(
        IMemberRepository memberRepository,
        ICommitteeMemberRepository committeeMemberRepository,
        Guid actingMemberId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(actingMemberId, cancellationToken);
        if (member is null || member.GroupId != groupId)
        {
            return false;
        }

        if (member.Role == MemberRole.Admin)
        {
            return true;
        }

        return await committeeMemberRepository.IsCommitteeMemberAsync(groupId, actingMemberId, cancellationToken);
    }

    public static bool CanMemberView(Meeting meeting, bool canManageMeetings) =>
        canManageMeetings || MemberVisibleStatuses.Contains(meeting.Status);

    public static IReadOnlyList<Meeting> FilterForViewer(IReadOnlyList<Meeting> meetings, bool canManageMeetings)
    {
        if (canManageMeetings)
        {
            return meetings;
        }

        return meetings.Where(m => MemberVisibleStatuses.Contains(m.Status)).ToList();
    }
}
