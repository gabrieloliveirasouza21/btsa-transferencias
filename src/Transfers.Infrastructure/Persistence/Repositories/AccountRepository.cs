using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;
using Transfers.Domain.Accounts;

namespace Transfers.Infrastructure.Persistence.Repositories
{
    public class AccountRepository(AppDbContext db) : IAccountRepository
    {
        public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct) =>
            db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

        public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken ct) =>
            await db.Accounts.AsNoTracking().OrderBy(a => a.HolderName).ToListAsync(ct);

        public Task<Account?> GetForUpdateAsync(Guid id, CancellationToken ct) =>
            db.Accounts
                .FromSql($"SELECT * FROM Accounts WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}")
                .SingleOrDefaultAsync(ct);
    }
}
