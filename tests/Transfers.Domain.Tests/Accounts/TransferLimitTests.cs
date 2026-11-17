using Transfers.Domain.Accounts;
using Transfers.Domain.Common;

namespace Transfers.Domain.Tests.Accounts;

public class TransferLimitTests
{
    private static readonly TransferLimits Limits = new(5000m, 5, 1000m, 3);

    private const bool Day = false;
    private const bool Night = true;

    [Theory]
    [InlineData(Day, 4000, 0, 1000)]  // exactly at the day amount limit
    [InlineData(Day, 0, 4, 100)]      // 5th attempt of the hour
    [InlineData(Night, 0, 0, 1000)]   // exactly at the night amount limit
    [InlineData(Night, 0, 2, 100)]    // 3rd attempt of the night hour
    public void EnsureAllows_ShouldNotThrow_WhenWithinLimits(
        bool isNight, double amountUsedInWindow, int attemptsInWindow, double newAmount)
    {
        var exception = Record.Exception(() =>
            Limits.EnsureAllows(isNight, (decimal)amountUsedInWindow, attemptsInWindow, (decimal)newAmount));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(Day, 4000, 0, 1001, FailureReason.HourlyLimitExceeded)]
    [InlineData(Day, 0, 5, 100, FailureReason.HourlyAttemptsExceeded)]
    [InlineData(Night, 0, 0, 1001, FailureReason.HourlyLimitExceeded)]
    [InlineData(Night, 0, 3, 100, FailureReason.HourlyAttemptsExceeded)]
    [InlineData(Day, 5000, 5, 100, FailureReason.HourlyAttemptsExceeded)]
    public void EnsureAllows_ShouldThrow_WhenLimitExceeded(
        bool isNight, double amountUsedInWindow, int attemptsInWindow, double newAmount, FailureReason expectedReason)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Limits.EnsureAllows(isNight, (decimal)amountUsedInWindow, attemptsInWindow, (decimal)newAmount));

        Assert.Equal(expectedReason, exception.Reason);
    }

    [Theory]
    [InlineData(0, 5, 1000, 3)]
    [InlineData(5000, 0, 1000, 3)]
    [InlineData(5000, 5, 0, 3)]
    [InlineData(5000, 5, 1000, 0)]
    [InlineData(-1, 5, 1000, 3)]
    public void Constructor_ShouldThrowInvalidAmount_WhenAnyLimitIsNotPositive(
        double dayMaxAmount, int dayMaxAttempts, double nightMaxAmount, int nightMaxAttempts)
    {
        var exception = Assert.Throws<DomainException>(() =>
            new TransferLimits((decimal)dayMaxAmount, dayMaxAttempts, (decimal)nightMaxAmount, nightMaxAttempts));

        Assert.Equal(FailureReason.InvalidAmount, exception.Reason);
    }
}
