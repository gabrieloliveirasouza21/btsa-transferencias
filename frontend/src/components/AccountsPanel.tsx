import type { Account } from '../types';
import { formatMoney } from '../format';

interface Props {
  accounts: Account[];
  loading: boolean;
  error: string | null;
  onRefresh: () => void;
}

const statusLabel: Record<Account['status'], string> = {
  Active: 'Ativa',
  Blocked: 'Bloqueada',
  Inactive: 'Inativa',
};

export function AccountsPanel({ accounts, loading, error, onRefresh }: Props) {
  return (
    <section className="panel">
      <header className="panel-header">
        <h2>Contas</h2>
        <button type="button" className="secondary" onClick={onRefresh} disabled={loading}>
          {loading ? 'Atualizando…' : 'Atualizar'}
        </button>
      </header>

      {error && <p className="alert error">{error}</p>}

      <ul className="accounts">
        {accounts.map((account) => (
          <li key={account.id} className="account">
            <div className="account-top">
              <strong>{account.holderName}</strong>
              <span className={`badge ${account.status.toLowerCase()}`}>{statusLabel[account.status]}</span>
            </div>

            <dl className="figures">
              <div>
                <dt>Saldo</dt>
                <dd className={account.balance < 0 ? 'negative' : undefined}>{formatMoney(account.balance)}</dd>
              </div>
              <div>
                <dt>Cheque especial</dt>
                <dd>{formatMoney(account.overdraftLimit)}</dd>
              </div>
              <div>
                <dt>Disponível</dt>
                <dd>{formatMoney(account.availableAmount)}</dd>
              </div>
            </dl>

            <p className="limits">
              Por hora: dia {formatMoney(account.limits.dayMaxAmountPerHour)} / {account.limits.dayMaxAttemptsPerHour} tentativas
              · noite {formatMoney(account.limits.nightMaxAmountPerHour)} / {account.limits.nightMaxAttemptsPerHour} tentativas
            </p>

            <code className="account-id">{account.id}</code>
          </li>
        ))}
      </ul>
    </section>
  );
}
