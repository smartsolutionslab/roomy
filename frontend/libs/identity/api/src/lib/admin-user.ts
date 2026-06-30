import { toAccountRole } from './account';
import type { AccountRole } from './account';
import type { AdminUserResponse } from './generated';
import { userId } from './user-id';
import type { UserId } from './user-id';

export type AccountStatus = 'provisioning' | 'active';

export interface AdminUser {
  readonly userId: UserId;
  readonly email: string;
  readonly displayName: string;
  readonly role: AccountRole;
  readonly status: AccountStatus;
}

// role/status are narrowed from the contract's plain strings to the enums it can't express.
export function toAdminUser(response: AdminUserResponse): AdminUser {
  return {
    userId: userId(response.userId),
    email: response.email,
    displayName: response.displayName,
    role: toAccountRole(response.role),
    status: toAccountStatus(response.status),
  };
}

function toAccountStatus(value: string): AccountStatus {
  if (value === 'provisioning' || value === 'active') return value;

  throw new Error(`Unexpected account status: ${value}`);
}
