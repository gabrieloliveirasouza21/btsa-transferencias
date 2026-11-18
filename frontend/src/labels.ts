import type { TransferStatus } from './types';

const reasonLabels: Record<string, string> = {
  InvalidAmount: 'Valor inválido: precisa ser maior que zero.',
  SameAccount: 'A conta de origem e a de destino são a mesma.',
  AccountNotActive: 'Uma das contas não está ativa.',
  InsufficientFunds: 'Saldo insuficiente, mesmo usando o cheque especial.',
  HourlyLimitExceeded: 'Limite de valor por hora excedido.',
  HourlyAttemptsExceeded: 'Limite de tentativas por hora excedido.',
  InvalidScheduledDate: 'A data do agendamento precisa estar no futuro.',
  InvalidStatusTransition: 'Essa operação não é permitida no status atual da transferência.',
};

export const reasonLabel = (reason: string | null | undefined) =>
  reason ? (reasonLabels[reason] ?? reason) : '';

export const statusLabels: Record<TransferStatus, string> = {
  Scheduled: 'Agendada',
  Processing: 'Processando',
  Completed: 'Concluída',
  Failed: 'Recusada',
  Cancelled: 'Cancelada',
};
