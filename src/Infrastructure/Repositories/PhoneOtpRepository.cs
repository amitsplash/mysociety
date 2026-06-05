using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common.Interfaces;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Repositories;

public class PhoneOtpRepository : IPhoneOtpRepository
{
    private readonly AppDbContext _dbContext;

    public PhoneOtpRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PhoneOtpVerification verification, CancellationToken cancellationToken)
    {
        await _dbContext.PhoneOtpVerifications.AddAsync(verification, cancellationToken);
    }

    public Task<PhoneOtpVerification?> GetActiveAsync(
        string phone,
        OtpPurpose purpose,
        CancellationToken cancellationToken)
    {
        return _dbContext.PhoneOtpVerifications
            .Where(x =>
                x.Phone == phone
                && x.Purpose == purpose
                && x.UsedAt == null
                && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PhoneOtpVerification?> GetLatestAsync(
        string phone,
        OtpPurpose purpose,
        CancellationToken cancellationToken)
    {
        return _dbContext.PhoneOtpVerifications
            .Where(x => x.Phone == phone && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task InvalidateActiveAsync(string phone, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var active = await _dbContext.PhoneOtpVerifications
            .Where(x =>
                x.Phone == phone
                && x.Purpose == purpose
                && x.UsedAt == null
                && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var item in active)
        {
            item.UsedAt = now;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
