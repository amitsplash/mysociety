using MySociety.Application.Financial;

namespace MySociety.Application.Ledgers;

public interface ILedgerQueryService
{
    Task<MemberLedgerResponse> GetMemberLedgerAsync(
        Guid memberId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberBalanceDto>> GetGroupBalancesAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<GroupLedgerOverviewResponse> GetGroupLedgerOverviewAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken);

    Task<FundLedgerResponse> GetFundLedgerAsync(
        Guid groupId,
        Guid actingMemberId,
        CancellationToken cancellationToken);
}

public record MemberLedgerResponse(
    Guid MemberId,
    Guid GroupId,
    decimal Balance,
    IReadOnlyList<LedgerEntryDto> Entries);

public record GroupLedgerOverviewResponse(
    Guid GroupId,
    IReadOnlyList<MemberLedgerSummary> Members);

public record MemberLedgerSummary(
    Guid MemberId,
    string MemberName,
    decimal Balance,
    IReadOnlyList<LedgerEntryDto> Entries);
