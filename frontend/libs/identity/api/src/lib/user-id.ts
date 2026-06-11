import type { Brand } from '@roomy/util';

// Backend uuids are trusted, so userId mints the brand without re-validating.
export type UserId = Brand<string, 'UserId'>;

export const userId = (value: string): UserId => value as UserId;
