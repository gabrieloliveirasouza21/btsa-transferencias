using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;
using Transfers.Application.Common;

namespace Transfers.Infrastructure.Persistence
{
    public class UnitOfWork(AppDbContext db) : IUnitOfWork
    {
        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            try
            {
                var result = await work(ct);
                await SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return result;
            }
            finally
            {
                db.ChangeTracker.Clear();
            }
        }

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConcurrencyConflictException("The record was changed by another operation.", ex);
            }
        }
    }
}
