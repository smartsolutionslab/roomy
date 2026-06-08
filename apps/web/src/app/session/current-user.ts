// The BFF's token-free projection of the signed-in user (ADR-0013): mirrors the gateway's
// CurrentUser record. The SPA only ever sees a name and roles, never a token.
export interface CurrentUser {
  readonly name: string;
  readonly roles: readonly string[];
}
