using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;

namespace MySociety.Application.Resolutions;

public static class ResolutionProvisioning
{
    public static async Task<string> GenerateNumberAsync(
        IResolutionRepository resolutionRepository,
        Guid groupId,
        int year,
        CancellationToken cancellationToken)
    {
        var count = await resolutionRepository.CountForGroupYearAsync(groupId, year, cancellationToken);
        return $"RES-{year}-{(count + 1):D3}";
    }

    public static async Task EnsureForAgendaMinuteAsync(
        IResolutionRepository resolutionRepository,
        AgendaItem agendaItem,
        Meeting meeting,
        Minute minute,
        Guid actingMemberId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(minute.DecisionTaken))
        {
            return;
        }

        var existing = await resolutionRepository.GetByMeetingAndAgendaItemIdAsync(
            meeting.Id,
            agendaItem.Id,
            cancellationToken);

        if (existing is not null)
        {
            existing.Title = TruncateTitle(minute.DecisionTaken);
            existing.Description = minute.DecisionTaken.Trim();
            existing.ApprovedBudget = minute.BudgetApproved ?? existing.ApprovedBudget;
            await resolutionRepository.SaveChangesAsync(cancellationToken);
            return;
        }

        var resolutionDate = meeting.MeetingDate.Date;
        var resolution = new Resolution
        {
            Id = Guid.NewGuid(),
            GroupId = meeting.GroupId,
            MeetingId = meeting.Id,
            AgendaItemId = agendaItem.Id,
            OpenMatterId = agendaItem.OpenMatterId,
            ResolutionNumber = await GenerateNumberAsync(
                resolutionRepository,
                meeting.GroupId,
                resolutionDate.Year,
                cancellationToken),
            Title = TruncateTitle(minute.DecisionTaken),
            Description = minute.DecisionTaken.Trim(),
            ResolutionDate = resolutionDate,
            ApprovedBudget = minute.BudgetApproved,
            Status = ResolutionStatus.Active,
            CreatedByMemberId = actingMemberId,
            CreatedAt = DateTime.UtcNow,
        };

        await resolutionRepository.AddAsync(resolution, cancellationToken);
        await resolutionRepository.SaveChangesAsync(cancellationToken);
    }

    private static string TruncateTitle(string decisionText)
    {
        var line = decisionText.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            ?? decisionText.Trim();
        return line.Length <= 200 ? line : line[..200];
    }
}
