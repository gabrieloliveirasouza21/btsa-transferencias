using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;
using Transfers.Domain.Transfers;

namespace Transfers.Infrastructure.Persistence.Repositories
{
    public class TransferRepository(AppDbContext db) : ITransferRepository
    {
        public void Add(Transfer transfer) => db.Transfers.Add(transfer);

        public Task<Transfer?> GetByIdAsync(Guid id, CancellationToken ct) =>
            db.Transfers.FirstOrDefaultAsync(t => t.Id == id, ct);

        public async Task<HourlyUsage> GetUsageSinceAsync(Guid sourceAccountId, DateTimeOffset since, CancellationToken ct)
        {
            var inWindow = db.Transfers.Where(t =>
                t.SourceAccountId == sourceAccountId &&
                t.ProcessedAt >= since &&
                (t.Status == TransferStatus.Completed || t.Status == TransferStatus.Failed));

            var attempts = await inWindow.CountAsync(ct);
            var amount = await inWindow
                .Where(t => t.Status == TransferStatus.Completed)
                .SumAsync(t => t.Amount, ct);

            return new HourlyUsage(attempts, amount);
        }

        public async Task<IReadOnlyList<Guid>> ListDueScheduledIdsAsync(DateTimeOffset now, int batchSize, CancellationToken ct) =>
            await db.Transfers
                .AsNoTracking()
                .Where(t => t.Status == TransferStatus.Scheduled && t.ScheduledFor <= now)
                .OrderBy(t => t.ScheduledFor)
                .Select(t => t.Id)
                .Take(batchSize)
                .ToListAsync(ct);
    }
}
