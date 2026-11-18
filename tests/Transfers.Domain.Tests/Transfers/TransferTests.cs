using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Domain.Tests.Transfers;

public class TransferTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 17, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset InThirtyMinutes = Now.AddMinutes(30);
    private static readonly Guid SourceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DestinationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public enum TransferAction { StartProcessing, Complete, Fail, Cancel }

    // ---------- Creation ----------

    [Fact]
    public void CreateImmediate_ShouldStartInProcessing_WithGivenData()
    {
        var transfer = Transfer.CreateImmediate(SourceId, DestinationId, 300m, Now);

        Assert.NotEqual(Guid.Empty, transfer.Id);
        Assert.Equal(SourceId, transfer.SourceAccountId);
        Assert.Equal(DestinationId, transfer.DestinationAccountId);
        Assert.Equal(300m, transfer.Amount);
        Assert.Equal(TransferStatus.Processing, transfer.Status);
        Assert.Equal(Now, transfer.CreatedAt);
        Assert.Null(transfer.ScheduledFor);
        Assert.Null(transfer.FailureReason);
    }

    [Fact]
    public void CreateScheduled_ShouldStartInScheduled_WithScheduledFor()
    {
        var transfer = Transfer.CreateScheduled(SourceId, DestinationId, 300m, InThirtyMinutes, Now);

        Assert.Equal(TransferStatus.Scheduled, transfer.Status);
        Assert.Equal(InThirtyMinutes, transfer.ScheduledFor);
        Assert.Equal(Now, transfer.CreatedAt);
        Assert.Null(transfer.ProcessedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateImmediate_ShouldThrowInvalidAmount_WhenAmountIsNotPositive(double amount)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Transfer.CreateImmediate(SourceId, DestinationId, (decimal)amount, Now));

        Assert.Equal(FailureReason.InvalidAmount, exception.Reason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateScheduled_ShouldThrowInvalidAmount_WhenAmountIsNotPositive(double amount)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Transfer.CreateScheduled(SourceId, DestinationId, (decimal)amount, InThirtyMinutes, Now));

        Assert.Equal(FailureReason.InvalidAmount, exception.Reason);
    }

    [Fact]
    public void CreateImmediate_ShouldThrowSameAccount_WhenSourceEqualsDestination()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Transfer.CreateImmediate(SourceId, SourceId, 300m, Now));

        Assert.Equal(FailureReason.SameAccount, exception.Reason);
    }

    [Fact]
    public void CreateScheduled_ShouldThrowSameAccount_WhenSourceEqualsDestination()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Transfer.CreateScheduled(SourceId, SourceId, 300m, InThirtyMinutes, Now));

        Assert.Equal(FailureReason.SameAccount, exception.Reason);
    }

    [Theory]
    [InlineData(0)]    // exactly now
    [InlineData(-1)]   // one minute in the past
    public void CreateScheduled_ShouldThrowInvalidScheduledDate_WhenDateIsNotInTheFuture(int minutesFromNow)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Transfer.CreateScheduled(SourceId, DestinationId, 300m, Now.AddMinutes(minutesFromNow), Now));

        Assert.Equal(FailureReason.InvalidScheduledDate, exception.Reason);
    }

    // ---------- Valid transitions ----------

    [Fact]
    public void StartProcessing_ShouldMoveScheduledToProcessing()
    {
        var transfer = Transfer.CreateScheduled(SourceId, DestinationId, 300m, InThirtyMinutes, Now);

        transfer.StartProcessing(InThirtyMinutes);

        Assert.Equal(TransferStatus.Processing, transfer.Status);
        Assert.Equal(InThirtyMinutes, transfer.ProcessedAt);
    }

    [Fact]
    public void Complete_ShouldMoveProcessingToCompleted()
    {
        var transfer = Transfer.CreateImmediate(SourceId, DestinationId, 300m, Now);

        transfer.Complete(Now);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal(Now, transfer.ProcessedAt);
        Assert.Null(transfer.FailureReason);
    }

    [Fact]
    public void Fail_ShouldMoveProcessingToFailed_WithReason()
    {
        var transfer = Transfer.CreateImmediate(SourceId, DestinationId, 300m, Now);

        transfer.Fail(FailureReason.InsufficientFunds, Now);

        Assert.Equal(TransferStatus.Failed, transfer.Status);
        Assert.Equal(FailureReason.InsufficientFunds, transfer.FailureReason);
        Assert.Equal(Now, transfer.ProcessedAt);
    }

    [Fact]
    public void Cancel_ShouldMoveScheduledToCancelled()
    {
        var transfer = Transfer.CreateScheduled(SourceId, DestinationId, 300m, InThirtyMinutes, Now);

        transfer.Cancel(Now);

        Assert.Equal(TransferStatus.Cancelled, transfer.Status);
    }

    [Fact]
    public void ScheduledTransfer_ShouldFollowFullPath_ScheduledProcessingCompleted()
    {
        var transfer = Transfer.CreateScheduled(SourceId, DestinationId, 300m, InThirtyMinutes, Now);

        transfer.StartProcessing(InThirtyMinutes);
        transfer.Complete(InThirtyMinutes);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
    }

    // ---------- Invalid transitions ----------
    // State machine: Scheduled -> Processing -> Completed | Failed, and Scheduled -> Cancelled.

    [Theory]
    [InlineData(TransferStatus.Scheduled, TransferAction.Complete)]   // must go through Processing first
    [InlineData(TransferStatus.Scheduled, TransferAction.Fail)]       // must go through Processing first
    [InlineData(TransferStatus.Processing, TransferAction.StartProcessing)]
    [InlineData(TransferStatus.Processing, TransferAction.Cancel)]    // already running, too late to cancel
    [InlineData(TransferStatus.Completed, TransferAction.Fail)]
    [InlineData(TransferStatus.Completed, TransferAction.Cancel)]
    [InlineData(TransferStatus.Failed, TransferAction.Complete)]
    [InlineData(TransferStatus.Cancelled, TransferAction.StartProcessing)]
    [InlineData(TransferStatus.Cancelled, TransferAction.Complete)]
    public void Transition_ShouldThrowInvalidStatusTransition_WhenNotAllowed(TransferStatus from, TransferAction action)
    {
        var transfer = CreateInStatus(from);

        var exception = Assert.Throws<DomainException>(() => Apply(transfer, action));

        Assert.Equal(FailureReason.InvalidStatusTransition, exception.Reason);
        Assert.Equal(from, transfer.Status); // status must not change on a rejected transition
    }

    // ---------- Helpers ----------

    private static Transfer CreateInStatus(TransferStatus status)
    {
        switch (status)
        {
            case TransferStatus.Scheduled:
                return Transfer.CreateScheduled(SourceId, DestinationId, 300m, InThirtyMinutes, Now);
            case TransferStatus.Processing:
                return Transfer.CreateImmediate(SourceId, DestinationId, 300m, Now);
            case TransferStatus.Completed:
                var completed = Transfer.CreateImmediate(SourceId, DestinationId, 300m, Now);
                completed.Complete(Now);
                return completed;
            case TransferStatus.Failed:
                var failed = Transfer.CreateImmediate(SourceId, DestinationId, 300m, Now);
                failed.Fail(FailureReason.InsufficientFunds, Now);
                return failed;
            case TransferStatus.Cancelled:
                var cancelled = Transfer.CreateScheduled(SourceId, DestinationId, 300m, InThirtyMinutes, Now);
                cancelled.Cancel(Now);
                return cancelled;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }

    private static void Apply(Transfer transfer, TransferAction action)
    {
        switch (action)
        {
            case TransferAction.StartProcessing: transfer.StartProcessing(Now); break;
            case TransferAction.Complete: transfer.Complete(Now); break;
            case TransferAction.Fail: transfer.Fail(FailureReason.InsufficientFunds, Now); break;
            case TransferAction.Cancel: transfer.Cancel(Now); break;
        }
    }
}
