using Microsoft.EntityFrameworkCore;

using MySociety.Application.Common.Interfaces;

using MySociety.Domain.Entities;

using MySociety.Domain.Enums;

using MySociety.Infrastructure.Persistence;



namespace MySociety.Infrastructure.Repositories;



public class ResolutionRepository : IResolutionRepository

{

    private readonly AppDbContext _dbContext;



    public ResolutionRepository(AppDbContext dbContext)

    {

        _dbContext = dbContext;

    }



    public Task<Resolution?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>

        _dbContext.Resolutions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);



    public Task<Resolution?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken) =>

        _dbContext.Resolutions

            .Include(x => x.Meeting)

            .Include(x => x.CreatedByMember)

            .ThenInclude(x => x.User)

            .Include(x => x.AgendaItem)

            .Include(x => x.OpenMatter)

            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);



    public async Task<IReadOnlyList<Resolution>> GetByGroupIdAsync(

        Guid groupId,

        ResolutionStatus? statusFilter,

        CancellationToken cancellationToken)

    {

        var query = _dbContext.Resolutions

            .Include(x => x.Meeting)

            .Include(x => x.CreatedByMember)

            .ThenInclude(x => x.User)

            .Where(x => x.GroupId == groupId);



        if (statusFilter.HasValue)

        {

            query = query.Where(x => x.Status == statusFilter.Value);

        }



        return await query

            .OrderByDescending(x => x.ResolutionDate)

            .ThenByDescending(x => x.CreatedAt)

            .ToListAsync(cancellationToken);

    }



    public Task<int> CountForGroupYearAsync(Guid groupId, int year, CancellationToken cancellationToken)
    {
        var prefix = $"RES-{year}-";
        return _dbContext.Resolutions
            .CountAsync(x => x.GroupId == groupId && x.ResolutionNumber.StartsWith(prefix), cancellationToken);
    }

    public Task<Resolution?> GetByMeetingAndAgendaItemIdAsync(
        Guid meetingId,
        Guid agendaItemId,
        CancellationToken cancellationToken) =>
        _dbContext.Resolutions
            .FirstOrDefaultAsync(x => x.MeetingId == meetingId && x.AgendaItemId == agendaItemId, cancellationToken);

    public async Task AddAsync(Resolution resolution, CancellationToken cancellationToken) =>

        await _dbContext.Resolutions.AddAsync(resolution, cancellationToken);



    public Task SaveChangesAsync(CancellationToken cancellationToken) =>

        _dbContext.SaveChangesAsync(cancellationToken);

}

