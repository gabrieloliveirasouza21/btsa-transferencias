import { useCallback, useEffect, useState } from 'react';
import { api } from './api';
import type { Account } from './types';
import { AccountsPanel } from './components/AccountsPanel';
import { TransferForm } from './components/TransferForm';
import { TransferLookup } from './components/TransferLookup';

export function App() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedTransferId, setSelectedTransferId] = useState('');

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

  const handleSubmitted = useCallback(
    (transferId?: string) => {
      if (transferId) setSelectedTransferId(transferId);
      void loadAccounts();
    },
    [loadAccounts],
  );

  return (
    <div className="layout">
      <header className="app-header">
        <h1>Transferências</h1>
      </header>

      <main className="content">
        <AccountsPanel accounts={accounts} loading={loading} error={error} onRefresh={loadAccounts} />
        <div className="stack">
          <TransferForm accounts={accounts} onSubmitted={handleSubmitted} />
          <TransferLookup accounts={accounts} selectedId={selectedTransferId} onChanged={loadAccounts} />
        </div>
      </main>
    </div>
  );
}
