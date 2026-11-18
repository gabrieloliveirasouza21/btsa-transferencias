using Transfers.Application.Abstractions;
using Transfers.Application.Common;

namespace Transfers.Application.Accounts
{
    public class AccountService(IAccountRepository accounts)
    {
        public async Task<AccountResponse> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var account = await accounts.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("Account", id);

            return AccountResponse.From(account);
        }

        public async Task<IReadOnlyList<AccountResponse>> ListAsync(CancellationToken ct)
        {
            var list = await accounts.ListAsync(ct);
            return list.Select(AccountResponse.From).ToList();
        }
    }
}
