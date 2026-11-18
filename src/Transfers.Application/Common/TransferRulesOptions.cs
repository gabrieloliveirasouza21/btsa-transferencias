using Transfers.Domain.Accounts;

namespace Transfers.Application.Common
{
    public sealed class TransferRulesOptions
    {
        public const string SectionName = "TransferRules";

        public string TimeZoneId { get; init; } = "America/Sao_Paulo";
        public TimeOnly NightStart { get; init; } = new(20, 0);
        public TimeOnly NightEnd { get; init; } = new(6, 0);

        public TimeSpan WindowLength { get; init; } = TimeSpan.FromMinutes(60);

        public NightPeriod NightPeriod => new(NightStart, NightEnd);
    }
}
