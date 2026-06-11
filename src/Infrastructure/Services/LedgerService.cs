using Microsoft.EntityFrameworkCore;
using MySociety.Application.Common;
using MySociety.Application.Common.Exceptions;
using MySociety.Application.Financial;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Infrastructure.Services;

public class LedgerService : ILedgerService
{
    private readonly AppDbContext _dbContext;

    public LedgerService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordOpeningBalanceAsync(Guid memberId, Guid groupId, decimal openingBalance, CancellationToken cancellationToken)
    {
        if (openingBalance == 0)
        {
            return;
        }

        var direction = openingBalance > 0
            ? LedgerEntryDirection.Credit
            : LedgerEntryDirection.Debit;

        await AddLedgerEntryAsync(
            memberId,
            groupId,
            LedgerEntryType.OpeningBalance,
            direction,
            Math.Abs(openingBalance),
            null,
            cancellationToken);
    }

    public Task RecordContributionDebitAsync(Guid memberId, Guid groupId, Guid contributionId, decimal amount, CancellationToken cancellationToken)
    {
        return AddLedgerEntryAsync(
            memberId,
            groupId,
            LedgerEntryType.Contribution,
            LedgerEntryDirection.Debit,
            amount,
            contributionId,
            cancellationToken);
    }

    public Task RecordPaymentCreditAsync(Guid memberId, Guid groupId, Guid paymentId, decimal amount, CancellationToken cancellationToken)
    {
        return AddLedgerEntryAsync(
            memberId,
            groupId,
            LedgerEntryType.Payment,
            LedgerEntryDirection.Credit,
            amount,
            paymentId,
            cancellationToken);
    }

    public Task RecordExpenseCreditAsync(Guid memberId, Guid groupId, Guid expenseId, decimal amount, CancellationToken cancellationToken)
    {
        return AddLedgerEntryAsync(
            memberId,
            groupId,
            LedgerEntryType.Expense,
            LedgerEntryDirection.Credit,
            amount,
            expenseId,
            cancellationToken);
    }

    public Task RecordCorpusPaymentAsync(Guid memberId, Guid groupId, decimal amount, CancellationToken cancellationToken)
    {
        return AddLedgerEntryAsync(
            memberId,
            groupId,
            LedgerEntryType.CorpusPayment,
            LedgerEntryDirection.Credit,
            amount,
            memberId,
            cancellationToken);
    }

    public async Task<decimal> GetBalanceAsync(Guid memberId, Guid groupId, CancellationToken cancellationToken)
    {
        var entries = await _dbContext.LedgerEntries
            .Where(x => x.MemberId == memberId && x.GroupId == groupId)
            .Select(x => new { x.Direction, x.Amount })
            .ToListAsync(cancellationToken);

        var totalCredits = entries
            .Where(x => x.Direction == LedgerEntryDirection.Credit)
            .Sum(x => x.Amount);

        var totalDebits = entries
            .Where(x => x.Direction == LedgerEntryDirection.Debit)
            .Sum(x => x.Amount);

        return totalCredits - totalDebits;
    }

    public async Task<IReadOnlyList<LedgerEntryDto>> GetEntriesAsync(
        Guid memberId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var entries = await _dbContext.LedgerEntries
            .Where(x => x.MemberId == memberId && x.GroupId == groupId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return entries.Select(x => new LedgerEntryDto(
            x.Id,
            x.MemberId,
            x.GroupId,
            x.Type.ToString(),
            x.Direction.ToString(),
            x.Amount,
            x.ReferenceId,
            x.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<MemberBalanceDto>> GetGroupBalancesAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var members = await _dbContext.Members
            .Include(x => x.User)
            .Where(x => x.GroupId == groupId)
            .ToListAsync(cancellationToken);

        var result = new List<MemberBalanceDto>();
        foreach (var member in members)
        {
            var balance = await GetBalanceAsync(member.Id, groupId, cancellationToken);
            result.Add(new MemberBalanceDto(member.Id, member.User.Name, balance));
        }

        return result.OrderBy(x => x.MemberName).ToList();
    }

    public async Task<FundBalanceDto> GetMaintenanceFundBalanceAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var openingBalance = await _dbContext.Groups
            .AsNoTracking()
            .Where(x => x.Id == groupId)
            .Select(x => x.OpeningMaintenanceBalance)
            .FirstOrDefaultAsync(cancellationToken);

        var paymentInflows = await _dbContext.LedgerEntries
            .Where(x =>
                x.GroupId == groupId &&
                x.Direction == LedgerEntryDirection.Credit &&
                x.Type == LedgerEntryType.Payment)
            .Select(x => x.Amount)
            .ToListAsync(cancellationToken);

        var otherInflows = await _dbContext.GroupIncomes
            .Where(x => x.GroupId == groupId)
            .Select(x => x.Amount)
            .ToListAsync(cancellationToken);

        var outflows = await _dbContext.GroupExpenses
            .Where(x => x.GroupId == groupId && x.FundType == GroupFundType.Maintenance)
            .Select(x => x.Amount)
            .ToListAsync(cancellationToken);

        var inflowTotal = openingBalance + paymentInflows.Sum() + otherInflows.Sum();
        var outflowTotal = outflows.Sum();

        return new FundBalanceDto(
            inflowTotal - outflowTotal,
            inflowTotal,
            outflowTotal);
    }

    public async Task<FundBalanceDto> GetCorpusFundBalanceAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var openingBalance = await _dbContext.Groups
            .AsNoTracking()
            .Where(x => x.Id == groupId)
            .Select(x => x.OpeningCorpusBalance)
            .FirstOrDefaultAsync(cancellationToken);

        var corpusInflows = await _dbContext.LedgerEntries
            .Where(x =>
                x.GroupId == groupId &&
                x.Direction == LedgerEntryDirection.Credit &&
                x.Type == LedgerEntryType.CorpusPayment)
            .Select(x => x.Amount)
            .ToListAsync(cancellationToken);

        var outflows = await _dbContext.GroupExpenses
            .Where(x => x.GroupId == groupId && x.FundType == GroupFundType.Corpus)
            .Select(x => x.Amount)
            .ToListAsync(cancellationToken);

        var inflowTotal = openingBalance + corpusInflows.Sum();
        var outflowTotal = outflows.Sum();

        return new FundBalanceDto(
            inflowTotal - outflowTotal,
            inflowTotal,
            outflowTotal);
    }

    public async Task<GroupFundsResponse> GetGroupFundsAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var maintenance = await GetMaintenanceFundBalanceAsync(groupId, cancellationToken);
        var corpus = await GetCorpusFundBalanceAsync(groupId, cancellationToken);

        return new GroupFundsResponse(groupId, maintenance, corpus);
    }

    public async Task<FundLedgerResponse> GetFundLedgerAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var group = await _dbContext.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == groupId, cancellationToken);

        var payments = await _dbContext.Payments
            .AsNoTracking()
            .Include(x => x.Member)
            .ThenInclude(x => x.User)
            .Where(x => x.GroupId == groupId && x.Status == PaymentStatus.Approved)
            .ToListAsync(cancellationToken);

        var corpusPayments = await _dbContext.LedgerEntries
            .AsNoTracking()
            .Include(x => x.Member)
            .ThenInclude(x => x.User)
            .Where(x =>
                x.GroupId == groupId &&
                x.Type == LedgerEntryType.CorpusPayment &&
                x.Direction == LedgerEntryDirection.Credit)
            .ToListAsync(cancellationToken);

        var societyExpenses = await _dbContext.GroupExpenses
            .AsNoTracking()
            .Where(x => x.GroupId == groupId)
            .ToListAsync(cancellationToken);

        var societyIncomes = await _dbContext.GroupIncomes
            .AsNoTracking()
            .Where(x => x.GroupId == groupId)
            .ToListAsync(cancellationToken);

        var rawLines = new List<(Guid Id, DateTime Date, string Description, string FundType, decimal Inflow, decimal Outflow)>();

        if (group is { OpeningMaintenanceBalance: > 0 })
        {
            rawLines.Add((
                group.Id,
                ExpenseDateRules.NormalizeToUtcDate(group.CreatedAt),
                "Opening maintenance fund",
                GroupFundType.Maintenance.ToString(),
                group.OpeningMaintenanceBalance,
                0m));
        }

        if (group is { OpeningCorpusBalance: > 0 })
        {
            rawLines.Add((
                group.Id,
                ExpenseDateRules.NormalizeToUtcDate(group.CreatedAt),
                "Opening corpus fund",
                GroupFundType.Corpus.ToString(),
                group.OpeningCorpusBalance,
                0m));
        }

        foreach (var payment in payments)
        {
            rawLines.Add((
                payment.Id,
                ExpenseDateRules.NormalizeToUtcDate(payment.CreatedAt),
                $"Contribution payment · {payment.Member.User.Name}",
                GroupFundType.Maintenance.ToString(),
                payment.Amount,
                0m));
        }

        foreach (var corpusPayment in corpusPayments)
        {
            rawLines.Add((
                corpusPayment.Id,
                ExpenseDateRules.NormalizeToUtcDate(corpusPayment.CreatedAt),
                $"Corpus payment · {corpusPayment.Member.User.Name}",
                GroupFundType.Corpus.ToString(),
                corpusPayment.Amount,
                0m));
        }

        foreach (var income in societyIncomes)
        {
            rawLines.Add((
                income.Id,
                ExpenseDateRules.NormalizeToUtcDate(income.IncomeDate),
                income.Description,
                GroupFundType.Maintenance.ToString(),
                income.Amount,
                0m));
        }

        foreach (var expense in societyExpenses)
        {
            rawLines.Add((
                expense.Id,
                ExpenseDateRules.NormalizeToUtcDate(expense.ExpenseDate),
                expense.Description,
                expense.FundType.ToString(),
                0m,
                expense.Amount));
        }

        var ordered = rawLines
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Inflow > 0 ? 0 : 1)
            .ThenBy(x => x.Id)
            .ToList();

        var runningByFund = new Dictionary<string, decimal>
        {
            [GroupFundType.Maintenance.ToString()] = 0m,
            [GroupFundType.Corpus.ToString()] = 0m
        };

        var lines = new List<FundLedgerLineDto>();
        foreach (var line in ordered)
        {
            runningByFund[line.FundType] += line.Inflow - line.Outflow;
            lines.Add(new FundLedgerLineDto(
                line.Id,
                line.Date,
                line.Description,
                line.FundType,
                line.Inflow,
                line.Outflow,
                runningByFund[line.FundType]));
        }

        var funds = await GetGroupFundsAsync(groupId, cancellationToken);

        return new FundLedgerResponse(groupId, funds, lines);
    }

    private async Task AddLedgerEntryAsync(
        Guid memberId,
        Guid groupId,
        LedgerEntryType type,
        LedgerEntryDirection direction,
        decimal amount,
        Guid? referenceId,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        var memberExists = await _dbContext.Members
            .AnyAsync(x => x.Id == memberId && x.GroupId == groupId, cancellationToken);

        if (!memberExists)
        {
            throw new InvalidOperationException("Member does not belong to the specified group.");
        }

        if (referenceId.HasValue)
        {
            var duplicateExists = await _dbContext.LedgerEntries.AnyAsync(
                x => x.MemberId == memberId &&
                     x.Type == type &&
                     x.ReferenceId == referenceId,
                cancellationToken);

            if (duplicateExists)
            {
                throw new ConflictException("Duplicate ledger entry detected for the same member, type and reference.");
            }
        }

        _dbContext.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            GroupId = groupId,
            Type = type,
            Direction = direction,
            Amount = amount,
            ReferenceId = referenceId,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
