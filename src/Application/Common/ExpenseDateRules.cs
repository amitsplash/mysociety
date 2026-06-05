namespace MySociety.Application.Common;

public static class ExpenseDateRules
{
    public static DateTime NormalizeToUtcDate(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    public static bool IsNotInFuture(DateTime expenseDate) =>
        NormalizeToUtcDate(expenseDate) <= DateTime.UtcNow.Date;
}
