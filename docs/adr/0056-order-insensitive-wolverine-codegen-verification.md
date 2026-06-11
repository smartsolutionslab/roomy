# 0056. Verify committed Wolverine codegen order-insensitively

- **Status:** Accepted
- **Date:** 2026-06-11
- **Deciders:** Heiko Weiß
- **Amends:** ADR-0034 (the *verification* of the committed static codegen; the static-codegen
  decision itself is unchanged)

## Context and problem statement

ADR-0034 commits each host's Wolverine handler code (`apps/<host>/Internal/Generated`) and runs it in
production with `TypeLoadMode.Static` — there is no Roslyn compiler at runtime (GH-2876). To stop the
committed code drifting from the handlers, CI re-runs `codegen write` and gates on
`git diff --exit-code`.

That gate is **not reproducible**, and it broke `main`: the build job fails at *Verify Wolverine
codegen is current*. The generated body of an EF-transactional handler (e.g. organization-api's
`UserRegisteredHandler`) contains **two independent dependency roots** —

- the inline Wolverine outbox (`new DbContextOutbox(...)` → `WolverineIntegrationEventOutbox`), and
- a **service-located** `OrganizationDbContext` (a `serviceScope` frame) —

and Wolverine emits them in an order that **varies by environment**. This was proven, not assumed:

- It is **not** OS: a clean Linux container reproduced the committed order, not CI's.
- It is **not** the SDK: CI installs SDK **10.0.301**; the container used the **same 10.0.301**, and
  still produced a different order than the CI runner.
- It is **not** the `ServiceLocationPolicy`: disabling `ServiceLocationPolicy.AllowedButWarn` left the
  `serviceScope` frame in place — the `DbContext` is marked for service location *intrinsically* by
  Wolverine's EF-Core transaction integration (a pooled/scoped `AddDbContext` registration), so the two
  racing frames cannot be collapsed by a DI change without abandoning the EF outbox/transaction codegen.

So `codegen write` legitimately produces different — but semantically identical — output on different
machines. A byte-exact `git diff` of that output is a false signal: it fails on reordering that has no
effect on behaviour, and the committed bytes cannot be reliably reproduced by the verifier.

## Decision drivers

- **Keep ADR-0034.** Static, committed codegen is still required (no runtime compiler in production).
- **The gate must catch real drift** — a handler added, removed, or whose dependencies/signature
  changed without regenerating — so production never runs stale handler code.
- **The gate must not fail on non-reproducible reordering** of semantically-identical generated code.
- **No suppression.** We are not disabling a gate to go green; we are making it assert the right
  invariant (semantic currency) instead of a non-reproducible one (byte order).

## Decision

Change the CI *Verify Wolverine codegen* step from a byte-exact `git diff --exit-code` to an
**order-insensitive** comparison. After `codegen write`:

- A **new or orphaned** generated file fails the gate. A handler's filename carries a signature hash
  (`UserRegisteredHandler1226547712.cs`); a changed signature/dependency set yields a *different*
  filename, so a forgotten regeneration surfaces as an untracked (or vanished) file — real drift.
- A **modified** generated file is compared **after sorting its lines**: if the committed and freshly
  generated versions match once sorted, the only difference is statement order and the gate passes;
  if they differ when sorted (a line added, removed, or changed), the gate fails — real drift.

Pure reordering of the independent variable declarations within one handler body therefore passes,
while any change to *which* code is generated fails. The committed files keep whatever valid ordering
they have; contributors no longer need to regenerate on a particular machine.

## Consequences

**Positive**
- `main` is green without rewriting the generated files: the committed order and CI's regenerated order
  differ only by statement order, which now normalizes equal.
- The gate still fails if someone changes a handler and forgets to commit the regenerated code (new
  filename hash → untracked file, or changed body lines → sorted-diff mismatch).
- Removes a recurring, machine-dependent CI failure that no amount of "regenerate and commit" fixed.

**Negative / trade-offs**
- The gate no longer guarantees the committed output is *byte-identical* to a fresh generation — only
  that it is semantically current. The whole-file line sort is a pragmatic normalizer; a change that
  both reorders and (coincidentally) leaves the sorted multiset identical would pass, which for
  generated handler bodies is not a realistic failure mode.
- This is a property of Wolverine/JasperFx code generation (non-deterministic ordering of independent
  frames); if a future Wolverine version emits a stable order, the byte-exact gate could be restored.

## Notes
Root-caused live against the failing `main` run: same SDK and OS as CI still diverged, and the
`ServiceLocationPolicy` toggle was tested and ruled out, so a deterministic-codegen DI restructure is
not available — hence fixing the *verification* rather than the *output*.
