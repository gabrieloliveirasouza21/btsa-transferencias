using System;
using System.Collections.Generic;
using System.Text;
using Transfers.Domain.Common;

namespace Transfers.Domain.Accounts
{
    public sealed record TransferLimits
    {
        public decimal DayMaxAmountPerHour { get; }
        public int DayMaxAttemptsPerHour { get; }
        public decimal NightMaxAmountPerHour { get; }
        public int NightMaxAttemptsPerHour { get; }

        public TransferLimits(decimal dayMaxAmountPerHour, int dayMaxAttemptsPerHour, decimal nightMaxAmountPerHour, int nightMaxAttemptsPerHour)
        {
            if (dayMaxAmountPerHour <= 0)
                throw new DomainException(FailureReason.InvalidAmount, "DayMaxAmountPerHour must be a positive value.");

            if (dayMaxAttemptsPerHour <= 0)
                throw new DomainException(FailureReason.InvalidAmount, "DayMaxAttemptsPerHour must be a positive value.");

            if (nightMaxAmountPerHour <= 0)
                throw new DomainException(FailureReason.InvalidAmount, "NightMaxAmountPerHour must be a positive value.");

            if (nightMaxAttemptsPerHour <= 0)
                throw new DomainException(FailureReason.InvalidAmount, "NightMaxAttemptsPerHour must be a positive value.");

            DayMaxAmountPerHour = dayMaxAmountPerHour;
            DayMaxAttemptsPerHour = dayMaxAttemptsPerHour;
            NightMaxAmountPerHour = nightMaxAmountPerHour;
            NightMaxAttemptsPerHour = nightMaxAttemptsPerHour;
        }

        public void EnsureAllows(bool isNight, decimal amountUsedInWindow, int attemptsInWindow, decimal newAmount)
        {
            var maxAmount = isNight ? NightMaxAmountPerHour : DayMaxAmountPerHour;
            var maxAttempts = isNight ? NightMaxAttemptsPerHour : DayMaxAttemptsPerHour;
            if(attemptsInWindow >= maxAttempts) throw new DomainException(FailureReason.HourlyAttemptsExceeded, $"Transfer attempts exceed the limit of {maxAttempts}.");
            if(amountUsedInWindow + newAmount > maxAmount) throw new DomainException(FailureReason.HourlyLimitExceeded, $"Transfer amount exceeds the limit of {maxAmount}.");
        }
    }
}
