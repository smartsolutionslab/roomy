// The BFF's token-free projection of the signed-in user: the SPA only ever sees a name and roles, never a token.
export interface CurrentUser {
  readonly name: string;
  readonly roles: readonly string[];
}
