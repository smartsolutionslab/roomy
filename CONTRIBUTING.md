# Contributing to Roomy

Roomy is built primarily by AI agents under human direction. This guide covers the
contribution mechanics. The deeper rules live elsewhere and are authoritative:

- `CLAUDE.md` — the operating contract, golden rules, and work loop.
- `docs/coding-standards/` — C# and TypeScript rules.
- `docs/adr/` — architecture and process decisions (this workflow is ADR-0010).

## Prerequisites

- .NET 10 SDK — **FILL IN exact version**
- Node.js — **FILL IN LTS version** — and **pnpm**, the only supported package manager
  (do not use npm or yarn)
- Nx (run via `pnpm`)

Setup:

```
pnpm install
dotnet restore   # FILL IN: solution path / Nx target
```

## The work loop (summary)

Every change is one story, started in a **fresh agent context**, driven **test-first**:
Specify → Plan → (Tasks) → **Red → Green → Refactor** → Verify → Review & merge. See
`CLAUDE.md` for the full loop and golden rules.

## Branching

- Branch off `main`. Keep branches short-lived — hours to a day or two.
- One story per branch. If it cannot land that fast the story is too big — slice it
  smaller, or hide unfinished behaviour behind a feature flag rather than letting the
  branch live for a week.
- Name the branch for its Conventional Commit type and the issue:
  `feat/123-desk-booking`, `fix/130-overlapping-bookings`, `refactor/...`, `chore/...`.
- `main` is protected; you cannot push to it directly.

## Commits

We do **not** squash. Every commit on a branch lands on `main` as-is and feeds the
changelog, so each commit must stand on its own.

- **Conventional Commits, per commit:** `type(scope): summary` — e.g.
  `feat(attendance): add desk booking`. Types: `feat`, `fix`, `refactor`, `test`,
  `docs`, `chore`, `build`, `ci`.
- **Atomic and clean.** No `wip`, no `fix lint`, no "address review" dumps. Commit in
  meaningful units as you go, or tidy the branch with `git rebase -i` (rebasing onto
  `main`) before opening the PR. GitHub's *Rebase and merge* replays commits as-is — it
  will not clean them for you.
- Enforced per commit by a `commit-msg` hook (commitlint) and a CI check across the PR's
  commit range.

## Pull requests

- One story per PR; small enough to review in one sitting.
- The PR is the human review gate over the agent's work — review intent and domain
  correctness; the gates own formatting and conventions.
- Required to merge: a PR and **green CI** (build, tests, lint + boundaries, formatting).
- Merge with **Rebase and merge** — linear history, no merge commits, no squash. Delete
  the branch afterwards.

## Quality gates

Run before marking a story done; also enforced in CI:

```
pnpm nx affected -t lint test build      # TS/Angular + Nx module-boundary lint
dotnet build -warnaserror                # nullable on, analyzers on
dotnet test                              # unit + integration + architecture tests
dotnet format --verify-no-changes        # formatting
```

**FILL IN** the exact Nx target names once the workspace exists. A gate failure is never
resolved by suppressing an analyzer or skipping a test — fix the cause.

## Releases

`main` is always releasable. Releases are tagged on `main`; Nx release derives the
version and changelog from the Conventional Commits. No release branches unless a past
production version ever needs a parallel patch.

## Definition of Done

Per `CLAUDE.md`: every acceptance criterion has a test written *before* its
implementation that now passes; all gates green; ADRs and docs updated if conventions
changed; no unjustified suppressions or skipped tests.
