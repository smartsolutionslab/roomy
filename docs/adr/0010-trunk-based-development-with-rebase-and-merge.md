# 0010. Trunk-based development with rebase-and-merge and per-commit Conventional Commits

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

Roomy is built primarily by AI agents under human direction, with one story per spec and
a fresh agent context per story. `main` must stay green and releasable, and Nx release
derives versions and changelog from commit history. We need a branching model that gives
the human a review checkpoint over agent output without the divergence of long-lived
branches, and a commit history clean enough to drive automated releasing — while keeping
the granular per-step history (no squashing).

## Decision drivers

- Keep `main` green, releasable, and linear.
- The PR is the human review gate over agent-generated changes.
- A meaningful commit history that feeds Nx release's changelog and versioning.
- Avoid ceremony that only pays off for multi-team coordination.
- Preserve granular history — squashing is explicitly not wanted.

## Considered options

- **Trunk-based, short-lived single-story branches, rebase-and-merge, no squash.**
- Trunk-based with squash-merge — rejected: discards the granular history.
- GitFlow (`develop`/`release`/`hotfix`) — rejected: ceremony for a coordination problem
  a one-human-plus-agents project does not have.
- Commit straight to trunk, no branches — rejected: loses the PR review checkpoint over
  agent output.

## Decision

Trunk-based development with **short-lived, single-story branches** off `main`. `main` is
protected: merge requires a PR and green CI. Branches merge with **rebase-and-merge** for
a linear history — **no squash, no merge commits**. Because every commit lands on `main`
as-is and feeds the changelog, **every commit is a clean, atomic Conventional Commit**;
contributors commit cleanly as they go or tidy the branch with `git rebase -i` before the
PR. Conventional Commits are enforced per commit (commitlint `commit-msg` hook + a CI
check across the PR's commit range). Branches are deleted on merge. Releases are tagged on
`main` via Nx release. Large work hides unfinished behaviour behind a feature flag rather
than living on a long branch. Hotfixes use the same flow with an expedited PR.

## Consequences

**Positive**
- `main` stays green, releasable, and linear; the changelog comes straight from commits.
- The PR is the review checkpoint; CI is the safety net.
- Granular red/green/refactor history is preserved for archaeology.
- Branch lifetime tracks a story, reinforcing the one-story-one-context rule.

**Negative / trade-offs**
- Commit-hygiene cost moves up front: clean commits as you go, or `rebase -i` before the
  PR. Agents must be instructed to produce clean Conventional Commits.
- Rebase-and-merge replays commits (new SHAs); contributors must be comfortable rebasing.
- The linear graph does not visually group commits by story — mitigated by the PR record,
  issue references, and Conventional Commit scopes.

**Follow-ups**
- Configure branch protection on `main` (required PR + required status checks).
- Add commitlint with a `commit-msg` hook (husky or lefthook) and a CI commit-range check.
- Document the flow in `CONTRIBUTING.md`; update the commit rule in `CLAUDE.md`.
- Configure Nx release to read Conventional Commits for versioning and changelog.
