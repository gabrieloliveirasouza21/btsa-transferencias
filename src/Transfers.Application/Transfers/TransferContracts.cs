using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers
{
    public sealed record CreateTransferRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount);

    public sealed record ScheduleTransferRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount, DateTimeOffset ScheduledFor);

    public sealed record TransferResponse(
        Guid Id,
        Guid SourceAccountId,
        Guid DestinationAccountId,
        decimal Amount,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ScheduledFor,
        DateTimeOffset? ProcessedAt,
        string? FailureReason)
    {
        public static TransferResponse From(Transfer transfer) => new(
            transfer.Id,
            transfer.SourceAccountId,
            transfer.DestinationAccountId,
            transfer.Amount,
            transfer.Status.ToString(),
            transfer.CreatedAt,
            transfer.ScheduledFor,
            transfer.ProcessedAt,
            transfer.FailureReason?.ToString());
    }
}
