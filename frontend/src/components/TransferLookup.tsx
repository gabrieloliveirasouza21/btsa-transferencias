import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { api, ApiError } from '../api';
import type { Account, Transfer } from '../types';
import { formatDateTime, formatMoney } from '../format';
import { reasonLabel, statusLabels } from '../labels';

interface Props {
  accounts: Account[];
  selectedId: string;
  onChanged: () => void;
}

const errorMessage = (err: unknown) => {
  if (err instanceof ApiError) {
    if (err.status === 404) return 'Transferência não encontrada.';
    if (err.status === 409) return 'A transferência mudou enquanto você cancelava. Consulte de novo para ver o status atual.';
    if (err.status === 422) return reasonLabel(err.problem.reason) || err.message;
  }
  return err instanceof Error ? err.message : 'Erro inesperado.';
};

export function TransferLookup({ accounts, selectedId, onChanged }: Props) {
  const [id, setId] = useState('');
  const [transfer, setTransfer] = useState<Transfer | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async (transferId: string) => {
    setBusy(true);
    setError(null);
    setTransfer(null);
    try {
      setTransfer(await api.getTransfer(transferId));
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }, []);

  useEffect(() => {
    if (selectedId) {
      setId(selectedId);
      void load(selectedId);
    }
  }, [selectedId, load]);

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    void load(id.trim());
  }

  async function handleCancel() {
    if (!transfer) return;
    setBusy(true);
    setError(null);
    try {
      setTransfer(await api.cancelTransfer(transfer.id));
      onChanged();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  const holderName = (accountId: string) =>
    accounts.find((account) => account.id === accountId)?.holderName ?? accountId;

  return (
    <section className="panel">
      <header className="panel-header">
        <h2>Consultar transferência</h2>
      </header>

      <form className="form lookup" onSubmit={handleSubmit}>
        <input
          type="text"
          placeholder="Id da transferência"
          value={id}
          onChange={(e) => setId(e.target.value)}
          aria-label="Id da transferência"
        />
        <button type="submit" disabled={busy || id.trim() === ''}>
          Consultar
        </button>
      </form>

      {error && <div className="alert error">{error}</div>}

      {transfer && (
        <div className="transfer-details">
          <div className="account-top">
            <strong>{formatMoney(transfer.amount)}</strong>
            <span className={`badge status-${transfer.status.toLowerCase()}`}>{statusLabels[transfer.status]}</span>
          </div>

          <dl className="details">
            <dt>Origem</dt>
            <dd>{holderName(transfer.sourceAccountId)}</dd>
            <dt>Destino</dt>
            <dd>{holderName(transfer.destinationAccountId)}</dd>
            <dt>Criada em</dt>
            <dd>{formatDateTime(transfer.createdAt)}</dd>
            {transfer.scheduledFor && (
              <>
                <dt>Agendada para</dt>
                <dd>{formatDateTime(transfer.scheduledFor)}</dd>
              </>
            )}
            {transfer.processedAt && (
              <>
                <dt>{transfer.status === 'Cancelled' ? 'Cancelada em' : 'Processada em'}</dt>
                <dd>{formatDateTime(transfer.processedAt)}</dd>
              </>
            )}
            {transfer.failureReason && (
              <>
                <dt>Motivo</dt>
                <dd>{reasonLabel(transfer.failureReason)}</dd>
              </>
            )}
          </dl>

          <code className="account-id">{transfer.id}</code>

          {transfer.status === 'Scheduled' && (
            <button type="button" className="secondary danger" onClick={handleCancel} disabled={busy}>
              Cancelar agendamento
            </button>
          )}
        </div>
      )}
    </section>
  );
}
