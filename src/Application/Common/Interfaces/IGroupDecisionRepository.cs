using MySociety.Domain.Entities;

namespace MySociety.Application.Common.Interfaces;

public interface IGroupDecisionRepository
{
    Task<IReadOnlyList<AgendaItem>> GetAgendaItemsWithDecisionsAsync(
        Guid groupId,
        Guid? meetingId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Resolution>> GetResolutionsAsync(
        Guid groupId,
        Guid? meetingId,
        CancellationToken cancellationToken);
}
