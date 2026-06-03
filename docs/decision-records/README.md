# Architecture Decision Records (ADRs)

Short, immutable records of significant decisions and their rationale.
Format: lightweight [MADR](https://adr.github.io/madr/)-style. New decisions get a new
numbered file; superseded ADRs are marked, not deleted.

| ADR | Title | Status |
|-----|-------|--------|
| [0001](0001-clean-architecture.md) | Clean Architecture with a dedicated Domain | Accepted |
| [0002](0002-bdd-and-test-tooling.md) | BDD with Reqnroll; unit tests with MSTest + Moq | Accepted |
| [0003](0003-security-and-error-baseline.md) | Security & error-handling baseline from day one | Accepted |
| [0004](0004-observability-caching-performance.md) | Observability, logging, caching & performance testing | Accepted |
| [0005](0005-stateless-cloud-ready-deployment.md) | Stateless, horizontally scalable, cloud-ready deployment | Accepted |
| [0006](0006-unit-management-workflow.md) | Unit management workflow & approval state machine | Accepted |
| [0007](0007-persistence-and-repository.md) | Persistence layer & repository pattern (multi-DB support) | Accepted |
| [0008](0008-authorization-and-roles.md) | Authorization & role-based access control (policy-based) | Accepted |
| [0009](0009-razor-pages-and-web-components.md) | Web UI: Razor Pages + modern web components (Lit/Shoelace) | Accepted |
| [0010](0010-c-sharp-interceptors.md) | C# interceptors for cross-cutting logging & telemetry (source generator) | Accepted |
| [0011](0011-security-testing-and-tooling.md) | Security testing & tooling (SAST/SCA/DAST/OWASP/BDD) | Accepted |
| [0012](0012-solid-dry-cqrs-kiss.md) | SOLID, DRY, CQRS, KISS as guiding principles (enforcement checklist) | Accepted |
| [0013](0013-partner-api-key-authentication.md) | Partner API key authentication & programmatic access | Accepted |
| [0014](0014-bdd-tooling-reqnroll.md) | BDD tooling: Reqnroll (maintained SpecFlow) in separate project | Accepted |
| [0015](0015-library-selection-policy.md) | Open-source library selection (.NET Foundation first, MIT preferred) | Accepted |
