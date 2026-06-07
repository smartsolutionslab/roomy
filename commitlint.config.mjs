// Conventional Commits enforcement for Roomy (issue #11, ADR-0010).
//
// History is never squashed: every commit lands on `main` as written and feeds the
// Nx release changelog (CONTRIBUTING.md, ADR-0010). This config rejects a
// non-conforming commit message at commit time via the `commit-msg` Husky hook.
//
// We extend @commitlint/config-conventional unchanged. Its defaults already match the
// conventions this repo follows: the standard Conventional Commit types and an
// OPTIONAL scope (the history mixes scoped commits like `docs(adr):` with unscoped
// ones like `chore:`), so we do not mandate a scope. The CONTRIBUTING.md type list
// (feat, fix, refactor, test, docs, chore, build, ci) plus `style`, `perf`, and
// `revert` are all part of config-conventional, so no custom type list is needed.

/** @type {import('@commitlint/types').UserConfig} */
export default {
  extends: ['@commitlint/config-conventional'],
};
