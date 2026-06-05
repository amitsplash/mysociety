using MySociety.Application.Common.Exceptions;
using MySociety.Domain.Entities;
using MySociety.Domain.Enums;

namespace MySociety.Application.Contributions;

public static class ContributionAmountCalculator
{
    public static decimal CalculateBaseAmount(Group group, Member member, int monthCount = 1)
    {
        if (monthCount < 1)
        {
            throw new ValidationException("Month count must be at least 1.");
        }

        var monthly = group.ContributionModel switch
        {
            ContributionModel.Fixed => group.ContributionAmount,
            ContributionModel.PerSquareFeet => member.SquareFeet.HasValue
                ? group.ContributionAmount * member.SquareFeet.Value
                : throw new ValidationException("Square feet is required for this member."),
            _ => throw new ValidationException("Unsupported contribution model.")
        };

        return monthly * monthCount;
    }

    /// <summary>
    /// Net amount still to collect after applying member ledger credit (positive balance).
    /// Used for display hints; contribution invoices always use the full base amount.
    /// </summary>
    public static decimal CalculateNetDueAfterCredit(decimal baseAmount, decimal ledgerBalance)
    {
        var credit = ledgerBalance > 0 ? ledgerBalance : 0;
        return Math.Max(0, baseAmount - credit);
    }
}
