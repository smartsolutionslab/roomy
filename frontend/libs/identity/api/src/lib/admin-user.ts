import { toAccountRole } from './account';
import type { AccountRole } from './account';
import type { AdminUserResponse } from './generated';
import { userId } from './user-id';
import type { UserId } from './user-id';

export type AccountStatus = 'provisioning' | 'active';

// The admin account overview projection: like Account, plus the provisioning status the administrator
// surface needs (identity-api.md).
export interface AdminUser {
  readonly userId: UserId;
  readonly email: string;
  readonly displayName: string;
  readonly role: AccountRole;
  readonly status: AccountStatus;
}

// Maps the trusted generated DTO to the branded domain type at the data-access boundary (ADR-0020),
// narrowing the contract's plain-string `role`/`status` to the enums it cannot express.
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
  if (value === 'provisioning' || value === 'active') {
    return value;
  }

  throw new Error(`Unexpected account status: ${value}`);
}
