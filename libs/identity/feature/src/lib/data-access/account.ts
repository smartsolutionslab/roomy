export type AccountRole = 'employee' | 'administrator';

export interface Account {
  readonly userId: string;
  readonly email: string;
  readonly displayName: string;
  readonly role: AccountRole;
}
