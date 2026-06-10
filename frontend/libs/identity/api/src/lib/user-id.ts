import type { Brand } from '@roomy/util';

// The identifier of a user account. A branded string so a bare string cannot be passed where a user
// identifier is expected. Backend DTOs are trusted (ADR-0020) and the contract already types this as a
// uuid, so the value is not re-validated here — `userId` only mints the brand at the data-access
// boundary, keeping the cast in one place instead of spread across the code.
export type UserId = Brand<string, 'UserId'>;

export const userId = (value: string): UserId => value as UserId;
