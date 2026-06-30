import type { AccountResponse } from './generated';
import { userId } from './user-id';
import type { UserId } from './user-id';

export type AccountRole = 'employee' | 'administrator';

export interface Account {
  readonly userId: UserId;
  readonly email: string;
  readonly displayName: string;
  readonly role: AccountRole;
}

// role is narrowed from the contract's plain string to the enum it can't express.
export function toAccount(response: AccountResponse): Account {
  return {
    userId: userId(response.userId),
    email: response.email,
    displayName: response.displayName,
    role: toAccountRole(response.role),
  };
}

export function toAccountRole(value: string): AccountRole {
  if (value === 'employee' || value === 'administrator') return value;

  throw new Error(`Unexpected account role: ${value}`);
}
