import type { Account, ProblemDetails, Transfer } from './types';

const baseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5009';

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `HTTP ${status}`);
    this.status = status;
    this.problem = problem;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  });

  const body = await response.json().catch(() => null);

  if (!response.ok) {
    throw new ApiError(response.status, body ?? { title: response.statusText });
  }

  return body as T;
}

export interface TransferInput {
  sourceAccountId: string;
  destinationAccountId: string;
  amount: number;
}

export const api = {
  listAccounts: () => request<Account[]>('/api/accounts'),

  getAccount: (id: string) => request<Account>(`/api/accounts/${id}`),

  createTransfer: (input: TransferInput) =>
    request<Transfer>('/api/transfers', { method: 'POST', body: JSON.stringify(input) }),

  scheduleTransfer: (input: TransferInput & { scheduledFor: string }) =>
    request<Transfer>('/api/transfers/scheduled', { method: 'POST', body: JSON.stringify(input) }),

  getTransfer: (id: string) => request<Transfer>(`/api/transfers/${id}`),

  cancelTransfer: (id: string) =>
    request<Transfer>(`/api/transfers/${id}/cancel`, { method: 'POST' }),
};
