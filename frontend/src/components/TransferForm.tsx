import { useState, type FormEvent } from 'react';
import { api, ApiError } from '../api';
import type { Account, Transfer } from '../types';
import { formatDateTime, formatMoney } from '../format';
import { reasonLabel } from '../labels';

interface Props {
  accounts: Account[];
  onSubmitted: (transferId?: string) => void;
}

type Mode = 'now' | 'scheduled';

type Outcome =
  | { kind: 'completed'; transfer: Transfer }
  | { kind: 'scheduled'; transfer: Transfer }
  | { kind: 'rejected'; reason: string; transferId?: string }
  | { kind: 'error'; message: string };

const toLocalInputValue = (date: Date) => {
  const offsetMs = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16);
};

export function TransferForm({ accounts, onSubmitted }: Props) {
  const [sourceId, setSourceId] = useState('');
  const [destinationId, setDestinationId] = useState('');
  const [amount, setAmount] = useState('');
  const [mode, setMode] = useState<Mode>('now');
  const [scheduledFor, setScheduledFor] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [outcome, setOutcome] = useState<Outcome | null>(null);

  const canSubmit =
    sourceId !== '' &&
    destinationId !== '' &&
    Number(amount) > 0 &&
    (mode === 'now' || scheduledFor !== '') &&
    !submitting;

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setOutcome(null);

    const input = { sourceAccountId: sourceId, destinationAccountId: destinationId, amount: Number(amount) };
    let transferId: string | undefined;

    try {
      if (mode === 'now') {
        const transfer = await api.createTransfer(input);
        transferId = transfer.id;
        setOutcome({ kind: 'completed', transfer });
      } else {
        const transfer = await api.scheduleTransfer({ ...input, scheduledFor: new Date(scheduledFor).toISOString() });
        transferId = transfer.id;
        setOutcome({ kind: 'scheduled', transfer });
      }
    } catch (err) {
      if (err instanceof ApiError && err.status === 422) {
        transferId = err.problem.transferId;
        setOutcome({ kind: 'rejected', reason: reasonLabel(err.problem.reason) || err.message, transferId });
      } else {
        setOutcome({ kind: 'error', message: err instanceof Error ? err.message : 'Erro inesperado.' });
      }
    } finally {
      setSubmitting(false);
      onSubmitted(transferId);
    }
  }

  return (
    <section className="panel">
      <header className="panel-header">
        <h2>Nova transferência</h2>
      </header>

      <form className="form" onSubmit={handleSubmit}>
        <label>
          Origem
          <select value={sourceId} onChange={(e) => setSourceId(e.target.value)} required>
            <option value="">Selecione</option>
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.holderName} · disponível {formatMoney(account.availableAmount)}
              </option>
            ))}
          </select>
        </label>

        <label>
          Destino
          <select value={destinationId} onChange={(e) => setDestinationId(e.target.value)} required>
            <option value="">Selecione</option>
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.holderName}
              </option>
            ))}
          </select>
        </label>

        <label>
          Valor (R$)
          <input
            type="number"
            inputMode="decimal"
            min="0.01"
            step="0.01"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
          />
        </label>

        <fieldset className="mode">
          <legend>Quando</legend>
          <label className="inline">
            <input type="radio" name="mode" checked={mode === 'now'} onChange={() => setMode('now')} />
            Agora
          </label>
          <label className="inline">
            <input type="radio" name="mode" checked={mode === 'scheduled'} onChange={() => setMode('scheduled')} />
            Agendar
          </label>
        </fieldset>

        {mode === 'scheduled' && (
          <label>
            Data e hora
            <input
              type="datetime-local"
              min={toLocalInputValue(new Date())}
              value={scheduledFor}
              onChange={(e) => setScheduledFor(e.target.value)}
              required
            />
          </label>
        )}

        <button type="submit" disabled={!canSubmit}>
          {submitting ? 'Enviando…' : mode === 'now' ? 'Transferir' : 'Agendar'}
        </button>
      </form>

      {outcome && <OutcomeMessage outcome={outcome} />}
    </section>
  );
}

function OutcomeMessage({ outcome }: { outcome: Outcome }) {
  switch (outcome.kind) {
    case 'completed':
      return (
        <div className="alert success">
          Transferência de {formatMoney(outcome.transfer.amount)} concluída.
          <code className="transfer-id">{outcome.transfer.id}</code>
        </div>
      );
    case 'scheduled':
      return (
        <div className="alert info">
          Transferência de {formatMoney(outcome.transfer.amount)} agendada para{' '}
          {formatDateTime(outcome.transfer.scheduledFor)}.
          <code className="transfer-id">{outcome.transfer.id}</code>
        </div>
      );
    case 'rejected':
      return (
        <div className="alert error">
          Transferência recusada: {outcome.reason}
          {outcome.transferId && (
            <>
              <span className="hint">A tentativa ficou registrada e conta para o limite por hora.</span>
              <code className="transfer-id">{outcome.transferId}</code>
            </>
          )}
        </div>
      );
    case 'error':
      return <div className="alert error">{outcome.message}</div>;
  }
}
