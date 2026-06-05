using MySociety.Application.Meetings;
using MySociety.Domain.Entities;

namespace MySociety.Application.Resolutions;

public static class ResolutionVisibility
{
    public static bool CanMemberView(Resolution resolution, bool canManageMeetings) =>
        canManageMeetings || MeetingVisibility.MemberVisibleStatuses.Contains(resolution.Meeting.Status);

    public static IReadOnlyList<Resolution> FilterForViewer(
        IReadOnlyList<Resolution> resolutions,
        bool canManageMeetings)
    {
        if (canManageMeetings)
        {
            return resolutions;
        }

        return resolutions
            .Where(r => MeetingVisibility.MemberVisibleStatuses.Contains(r.Meeting.Status))
            .ToList();
    }
}
