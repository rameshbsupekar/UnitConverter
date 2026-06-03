# ADR-0005 — Stateless, horizontally scalable, cloud-ready deployment

- Status: Accepted
- Date: 2026-06-03

## Context

The service should be deployable to the cloud in future, behind a **load balancer**, with
**caching** that remains correct across multiple instances, and should be validated under
**load/stress testing**. We want these properties designed-in, not retrofitted.

## Decision

Keep the API **stateless** and make scale/deploy concerns **configuration-driven**:

- **No server-side session/affinity.** Conversion is pure compute; any instance can serve any
  request. The load balancer round-robins; **no sticky sessions**.
- **Health checks** via `ServiceDefaults` (Aspire) for LB probes and Kubernetes
  liveness/readiness.
- **Caching:** per-instance L1 (`HybridCache`/`IMemoryCache`) is the default and is correct
  because the unit catalog is **immutable and identical** on every node. Reserve an **L2
  distributed cache (Redis)** for a future dynamic catalog — enabled by configuration, no
  redesign.
- **12-factor config:** OTLP endpoint, cache TTLs, rate limits, CORS via configuration so a
  single container image runs in every environment.
- **Containerized:** `dotnet publish` OCI images; Aspire `AppHost` models topology and can emit
  manifests (Azure Container Apps / AKS / Kubernetes).
- **Autoscaling:** scale on CPU or the emitted `unitconverter.*` metrics (HPA/KEDA).
- **Proxy awareness:** enable `UseForwardedHeaders`; require the LB to pass W3C `traceparent`.
- **Validation:** load, stress, spike, and soak tests (NBomber/k6) gate capacity claims
  before rollout (LLD §12).

## Rationale

- Statelessness is the cheapest, most reliable path to horizontal scale; designing it in costs
  nothing now and avoids a painful rewrite later.
- Per-instance caching of immutable data avoids premature distributed-cache complexity (YAGNI)
  while leaving a clean upgrade seam (HybridCache L1+L2).
- Config-driven exporters/limits keep one artifact promotable across local → cloud.

## Consequences

- Must avoid introducing hidden per-instance state (in-memory counters that imply affinity,
  local file writes, etc.). Anything stateful goes to an external store.
- A future dynamic catalog requires choosing an L2 store and an invalidation strategy.
- Forwarded-headers/TLS-termination config must be set correctly behind a proxy.

## Alternatives considered

- **Sticky sessions / in-process state** — rejected; defeats easy horizontal scale.
- **Distributed cache from day one** — rejected as premature; immutable catalog doesn't need it.
- **Serverless (per-request functions)** — viable for spiky traffic; cold-start and per-call
  cost trade-offs make a containerized always-warm service the default. Revisit per workload.
