import { AccountRole } from './account';

export type AccountStatus = 'provisioning' | 'active';

export interface AdminUser {
  readonly userId: string;
  readonly email: string;
  readonly displayName: string;
  readonly role: AccountRole;
  readonly status: AccountStatus;
}
