using MySociety.Domain.Common;
using MySociety.Domain.Enums;

namespace MySociety.Domain.Entities;

public class LedgerEntry : BaseEntity
{
    public Guid MemberId { get; set; }
    public Guid GroupId { get; set; }
    public LedgerEntryType Type { get; set; }
    public LedgerEntryDirection Direction { get; set; }
    public decimal Amount { get; set; }
    public Guid? ReferenceId { get; set; }

    public Member Member { get; set; } = null!;
    public Group Group { get; set; } = null!;
}
