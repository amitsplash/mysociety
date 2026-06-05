using MySociety.Domain.Entities;

using MySociety.Domain.Enums;



namespace MySociety.Application.Common.Interfaces;



public interface IResolutionRepository

{

    Task<Resolution?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Resolution?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Resolution>> GetByGroupIdAsync(

        Guid groupId,

        ResolutionStatus? statusFilter,

        CancellationToken cancellationToken);

    Task<int> CountForGroupYearAsync(Guid groupId, int year, CancellationToken cancellationToken);
    Task<Resolution?> GetByMeetingAndAgendaItemIdAsync(
        Guid meetingId,
        Guid agendaItemId,
        CancellationToken cancellationToken);
    Task AddAsync(Resolution resolution, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

}

