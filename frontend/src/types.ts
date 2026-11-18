export type AccountStatus = 'Active' | 'Blocked' | 'Inactive';

export type TransferStatus = 'Scheduled' | 'Processing' | 'Completed' | 'Failed' | 'Cancelled';

export interface Limits {
  dayMaxAmountPerHour: number;
  dayMaxAttemptsPerHour: number;
  nightMaxAmountPerHour: number;
  nightMaxAttemptsPerHour: number;
}

export interface Account {
  id: string;
  holderName: string;
  balance: number;
  overdraftLimit: number;
  availableAmount: number;
  status: AccountStatus;
  limits: Limits;
}

export interface Transfer {
  id: string;
  sourceAccountId: string;
  destinationAccountId: string;
  amount: number;
  status: TransferStatus;
  createdAt: string;
  scheduledFor: string | null;
  processedAt: string | null;
  failureReason: string | null;
}

export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  reason?: string;
  transferId?: string;
}
