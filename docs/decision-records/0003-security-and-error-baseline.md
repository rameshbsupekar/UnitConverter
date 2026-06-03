# ADR-0003 — Security & error-handling baseline from day one

- Status: Accepted
- Date: 2026-06-03

## Context

Even though v1 is a public, read-only conversion API with no user data, the project follows
"secure by design" (informed by *Beyond ASP.NET Core Security*). Retrofitting security and
consistent error handling later is costly and error-prone.

## Decision

Establish a baseline now, with hooks ready to enable as features grow:

1. **Validate at the edge** — reject malformed requests; unknown units → `404`; cross-category
   conversions → `422`.
2. **Consistent errors** — map domain exceptions centrally to RFC 7807 `ProblemDetails`;
   **never** expose stack traces; include a `traceId`/correlation id.
3. **Transport** — enforce HTTPS; OpenAPI document is the contract (Swagger UI dev-only).
4. **Abuse resistance (hooks)** — `AddRateLimiter`, request size limits, and **CORS** for
   browser clients; enable per environment.
5. **AuthZ (deferred)** — when user-specific features arrive, use **policy-based
   authorization**; keep authentication and authorization separate (ASP.NET Core guidance).
6. **Supply chain** — pin dependency versions; run `dotnet list package --vulnerable` /
   audit in CI.

## Rationale

- Cheap to design early, expensive to bolt on later.
- Clear, non-leaky errors improve client DX and avoid information disclosure.
- Policy-based authZ scales better than scattered role checks.

## Consequences

- Slightly more setup in `Program.cs` (middleware, options) before first release.
- Error mapping centralized, so controllers stay thin (reinforces ADR-0001).

## Alternatives considered

- **Add security later** — rejected; violates secure-by-design and creates rework.
- **Per-endpoint try/catch** — rejected; duplicates logic and leaks details. Use central
  exception handling instead.
