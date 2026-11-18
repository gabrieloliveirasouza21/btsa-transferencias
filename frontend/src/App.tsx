import { useCallback, useEffect, useState } from 'react';
import { api } from './api';
import type { Account } from './types';
import { AccountsPanel } from './components/AccountsPanel';

export function App() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadAccounts = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setAccounts(await api.listAccounts());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível carregar as contas.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadAccounts();
  }, [loadAccounts]);

  return (
    <div className="layout">
      <header className="app-header">
        <h1>Transferências</h1>
      </header>

      <main className="content">
        <AccountsPanel accounts={accounts} loading={loading} error={error} onRefresh={loadAccounts} />
      </main>
    </div>
  );
}
