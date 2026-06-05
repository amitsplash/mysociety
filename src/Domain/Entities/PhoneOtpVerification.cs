using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class PhoneOtpVerification : BaseEntity
{
    public string Phone { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
