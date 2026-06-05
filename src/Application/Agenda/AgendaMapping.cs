using MySociety.Application.Agenda.Dtos;
using MySociety.Application.Minutes.Dtos;
using MySociety.Domain.Entities;

namespace MySociety.Application.Agenda;

public static class AgendaMapping
{
    public static AgendaItemResponse MapToResponse(AgendaItem item)
    {
        var discussion = item.Minute?.DiscussionSummary ?? item.DiscussionSummary;
        MinuteResponse? minute = item.Minute is null
            ? null
            : new MinuteResponse(
                item.Minute.Id,
                item.Minute.AgendaItemId,
                item.Minute.DiscussionSummary,
                item.Minute.DecisionTaken,
                item.Minute.BudgetApproved,
                item.Minute.CreatedAt);

        return new AgendaItemResponse(
            item.Id,
            item.MeetingId,
            item.OpenMatterId,
            item.OpenMatter?.Title,
            item.AgendaNumber,
            item.Title,
            item.Description,
            item.DisplayOrder,
            item.Source,
            item.Outcome,
            discussion,
            minute,
            item.CreatedAt);
    }
}
