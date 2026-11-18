using Transfers.Application.Abstractions;

namespace Transfers.Infrastructure.Persistence
{
    public class UnitOfWork(AppDbContext db) : IUnitOfWork
    {
        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var result = await work(ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return result;
        }
    }
}
