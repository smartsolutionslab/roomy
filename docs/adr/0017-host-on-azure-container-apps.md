# 0017. Host on Azure, with Azure Container Apps as the compute target

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

We need a hosting target for the microservices system (services, gateway, broker,
identity, per-service Postgres). The choice is between self-hosting and a managed cloud;
managed Azure was chosen, partly to offset the operational burden the microservices
topology imposes.

## Decision drivers

- Managed infrastructure to reduce ops for a solo-plus-agents team running many services.
- A smooth deployment path from the .NET Aspire composition.
- Autoscaling and managed ingress.
- GDPR — satisfiable via an EU region and data-residency controls.

## Considered options

- **Azure with Azure Container Apps (ACA)** as the compute target.
- Azure Kubernetes Service (AKS) — more control, more ops.
- Azure App Service — weaker fit for many containerized services.

## Decision

Host on **Azure**, with **Azure Container Apps** as the primary compute target: .NET
Aspire deploys to ACA natively via `azd`, it is managed/serverless with per-service
autoscaling and ingress, and it maps cleanly onto the Aspire app composition. PostgreSQL
is provided by **Azure Database for PostgreSQL (Flexible Server)**, with a database per
service (ADR-0014). Deploy in an **EU region** for GDPR data residency. **AKS** remains
the fallback if fine-grained orchestration control becomes necessary.

Two stack interactions follow from moving to Azure:
- **Message broker:** resolved in ADR-0015 — RabbitMQ stays the default (run as a
  container on ACA), with Azure Service Bus and AWS SQS/SNS selectable by configuration,
  so neither cloud's broker is locked in.
- **Identity:** Keycloak (ADR-0013) runs as a container on ACA; Entra External ID is the
  managed-native alternative if reconsidered.

## Consequences

**Positive**
- Managed infrastructure reduces the ops burden of running many services; Aspire → ACA is
  a smooth path; autoscaling included.
- GDPR satisfiable via EU region and residency controls.

**Negative / trade-offs**
- Lock-in toward Azure-native services and pricing; less control than self-hosting.
- No managed RabbitMQ, forcing the broker interaction above.

**Follow-ups**
- Resolve the broker (RabbitMQ on ACA vs Azure Service Bus) — next decision.
- Provision Azure Database for PostgreSQL per service; select the EU region.
- Deploy via `azd` from the Aspire app host.
- Revisit Keycloak-on-ACA vs Entra External ID.
