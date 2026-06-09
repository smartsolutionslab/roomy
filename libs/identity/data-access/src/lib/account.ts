import type { AccountResponse } from './generated';
import { userId } from './user-id';
import type { UserId } from './user-id';

export type AccountRole = 'employee' | 'administrator';

// The signed-in account projection (IA-2/IA-5), with the role the app authorizes on.
export interface Account {
  readonly userId: UserId;
  readonly email: string;
  readonly displayName: string;
  readonly role: AccountRole;
}

// Maps the trusted generated DTO to the branded domain type at the data-access boundary (ADR-0020).
// The contract types `role` as a plain string, so it is narrowed here — the one place the enum the
// contract cannot express is enforced.
export function toAccount(response: AccountResponse): Account {
  return {
    userId: userId(response.userId),
    email: response.email,
    displayName: response.displayName,
    role: toAccountRole(response.role),
  };
}

export function toAccountRole(value: string): AccountRole {
  if (value === 'employee' || value === 'administrator') {
    return value;
  }

  throw new Error(`Unexpected account role: ${value}`);
}
