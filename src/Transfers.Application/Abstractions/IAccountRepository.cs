using Transfers.Domain.Accounts;

namespace Transfers.Application.Abstractions
{
    public interface IAccountRepository
    {
        Task<Account?> GetByIdAsync(Guid id, CancellationToken ct);

        Task<IReadOnlyList<Account>> ListAsync(CancellationToken ct);

        Task<Account?> GetForUpdateAsync(Guid id, CancellationToken ct);
    }
}
