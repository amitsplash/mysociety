namespace MySociety.Application.Financial;

public interface ILedgerService
{
    Task RecordOpeningBalanceAsync(Guid memberId, Guid groupId, decimal openingBalance, CancellationToken cancellationToken);

    Task RecordContributionDebitAsync(Guid memberId, Guid groupId, Guid contributionId, decimal amount, CancellationToken cancellationToken);

    Task RecordPaymentCreditAsync(Guid memberId, Guid groupId, Guid paymentId, decimal amount, CancellationToken cancellationToken);

    Task RecordExpenseCreditAsync(Guid memberId, Guid groupId, Guid expenseId, decimal amount, CancellationToken cancellationToken);

    Task RecordCorpusPaymentAsync(Guid memberId, Guid groupId, decimal amount, CancellationToken cancellationToken);

    Task<decimal> GetBalanceAsync(Guid memberId, Guid groupId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LedgerEntryDto>> GetEntriesAsync(Guid memberId, Guid groupId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberBalanceDto>> GetGroupBalancesAsync(Guid groupId, CancellationToken cancellationToken);

    Task<FundBalanceDto> GetMaintenanceFundBalanceAsync(Guid groupId, CancellationToken cancellationToken);

    Task<FundBalanceDto> GetCorpusFundBalanceAsync(Guid groupId, CancellationToken cancellationToken);

    Task<GroupFundsResponse> GetGroupFundsAsync(Guid groupId, CancellationToken cancellationToken);

    Task<FundLedgerResponse> GetFundLedgerAsync(Guid groupId, CancellationToken cancellationToken);
}

public record FundBalanceDto(
    decimal Balance,
    decimal TotalInflows,
    decimal TotalOutflows);

public record GroupFundsResponse(
    Guid GroupId,
    FundBalanceDto Maintenance,
    FundBalanceDto Corpus);

public record FundLedgerLineDto(
    Guid Id,
    DateTime TransactionDate,
    string Description,
    string FundType,
    decimal Inflow,
    decimal Outflow,
    decimal RunningBalance);

public record FundLedgerResponse(
    Guid GroupId,
    GroupFundsResponse Funds,
    IReadOnlyList<FundLedgerLineDto> Lines);

public record LedgerEntryDto(
    Guid Id,
    Guid MemberId,
    Guid GroupId,
    string Type,
    string Direction,
    decimal Amount,
    Guid? ReferenceId,
    DateTime CreatedAt);

public record MemberBalanceDto(
    Guid MemberId,
    string MemberName,
    decimal Balance);
