using Transfers.Domain.Accounts;

namespace Transfers.Application.Accounts
{
    public sealed record AccountResponse(
        Guid Id,
        string HolderName,
        decimal Balance,
        decimal OverdraftLimit,
        decimal AvailableAmount,
        string Status,
        LimitsResponse Limits)
    {
        public static AccountResponse From(Account account) => new(
            account.Id,
            account.HolderName,
            account.Balance,
            account.OverdraftLimit,
            account.AvailableAmount,
            account.Status.ToString(),
            new LimitsResponse(
                account.Limits.DayMaxAmountPerHour,
                account.Limits.DayMaxAttemptsPerHour,
                account.Limits.NightMaxAmountPerHour,
                account.Limits.NightMaxAttemptsPerHour));
    }

    public sealed record LimitsResponse(
        decimal DayMaxAmountPerHour,
        int DayMaxAttemptsPerHour,
        decimal NightMaxAmountPerHour,
        int NightMaxAttemptsPerHour);
}
