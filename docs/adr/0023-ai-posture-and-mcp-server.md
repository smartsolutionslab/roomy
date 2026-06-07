# 0023. AI posture: product features deferred, architecture AI-ready, MCP server for agent access

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

AI features are plausible for an attendance planner (a planning assistant, suggestions
from team patterns, natural-language queries, manager insights), and we also want Roomy
drivable by agents. We need a posture that keeps v1 lean without painting the architecture
into a corner, and that handles GDPR for any personal data sent to a model.

## Decision drivers

- Keep v1 scope minimal; don't build AI features before a use case justifies them.
- Don't preclude adding AI later cheaply.
- Agent-accessibility is on-brand (cf. Yumney) and cheap if it reuses the application
  layer.
- GDPR — attendance data is personal; an EU region / self-hostable model path matters.

## Decision

- **Product AI features are deferred** — not in v1.
- **The architecture stays AI-ready:** any future AI capability is an infrastructure
  adapter behind an owned application port, using `Microsoft.Extensions.AI` as the
  vendor-neutral seam, Azure OpenAI as the provider (Azure hosting), in an EU region.
  `domain`/`application` never depend on an AI SDK directly (ADR-0005). Adding AI later is
  an infra + composition change, not a rework.
- **Roomy exposes an MCP server** as a *driving adapter* onto the same application use
  cases (not a parallel implementation), authenticated through the existing auth model so
  agents perform the same operations as the REST client. Implemented with
  `ModelContextProtocol.AspNetCore`.
- **When AI features land**, two things get specified then: an **eval/guardrail layer** in
  the testing strategy (LLM output is non-deterministic — evals, not unit assertions), and
  **AI-UI patterns** (streaming, source citation, user correction, "AI-generated"
  labelling, fallback).

## Consequences

**Positive**
- v1 stays lean; AI is added later as an adapter, not a rework.
- The MCP server reuses the application layer — no duplicated logic.
- GDPR addressed via EU region, with a self-hosted-model path open later (prior
  local-inference interest).

**Negative / trade-offs**
- The MCP server is additional surface to secure and maintain.
- Deferring AI means the eval and AI-UI work is acknowledged but not yet done.

**Follow-ups**
- Define the AI application port when first needed.
- Add the eval/guardrail testing layer with the first AI feature.
- Build the MCP server (`ModelContextProtocol.AspNetCore`) as a driving adapter; decide
  v1 vs fast-follow.
- Authenticate agent/MCP access through the existing auth model.
