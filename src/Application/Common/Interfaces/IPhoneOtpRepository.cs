using MySociety.Domain.Entities;
using MySociety.Domain.Enums;

namespace MySociety.Application.Common.Interfaces;

public interface IPhoneOtpRepository
{
    Task AddAsync(PhoneOtpVerification verification, CancellationToken cancellationToken);
    Task<PhoneOtpVerification?> GetActiveAsync(string phone, OtpPurpose purpose, CancellationToken cancellationToken);
    Task<PhoneOtpVerification?> GetLatestAsync(string phone, OtpPurpose purpose, CancellationToken cancellationToken);
    Task InvalidateActiveAsync(string phone, OtpPurpose purpose, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
