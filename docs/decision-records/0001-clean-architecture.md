# ADR-0001 — Clean Architecture with a dedicated Domain

- Status: Accepted
- Date: 2026-06-03

## Context

We need a unit converter that supports Length, Temperature, and Weight now, and **any
number** of categories later, consumed over a REST API by web/JS/mobile clients. We also
want the conversion logic testable from the first commit (TDD/BDD).

## Decision

Adopt **Clean Architecture** with four layers and inward-only dependencies:
`Domain ← Application ← Infrastructure/Api`. Conversion logic lives in a framework-free
`UnitConverter.Domain`; the ASP.NET Core project is a thin adapter (composition root).

## Rationale

- **Testability:** a pure domain runs in milliseconds with no web host or DI container.
- **Extensibility (Open/Closed):** new units/categories are data + a registered rule; the
  API surface does not change. Aligns with the requirement "support any number of units".
- **Replaceable edges:** the unit catalog can move from code → JSON → DB behind a port
  (`IUnitCatalog`) without touching use cases (YAGNI: start in-memory).
- Consistent with Microsoft guidance: keep request handling thin, push logic into services.

## Consequences

- More projects/boundaries than a single Web API project (acceptable for the learning goal
  of "code like a pro").
- A mapping step between domain records and API DTOs (intentional: don't leak the domain).
- Clear place for every concern, which makes reviews and onboarding easier.

## Alternatives considered

- **Single Web API project with services** — simpler, but logic tends to leak into
  controllers and tests need the web stack. Rejected for the stated learning/extensibility goals.
- **Vertical slice architecture** — good option; deferred to keep the layered model explicit
  for teaching. Can be revisited if slices grow.
