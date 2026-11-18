using Transfers.Domain.Transfers;

namespace Transfers.Application.Abstractions
{
    public interface ITransferRepository
    {
        void Add(Transfer transfer);

        Task<Transfer?> GetByIdAsync(Guid id, CancellationToken ct);

        Task<HourlyUsage> GetUsageSinceAsync(Guid sourceAccountId, DateTimeOffset since, CancellationToken ct);

        Task<IReadOnlyList<Guid>> ListDueScheduledIdsAsync(DateTimeOffset now, int batchSize, CancellationToken ct);
    }

    public sealed record HourlyUsage(int Attempts, decimal Amount);
}
