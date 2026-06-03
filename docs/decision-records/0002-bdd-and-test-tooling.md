# ADR-0002 — BDD with Reqnroll; unit tests with MSTest + Moq

- Status: Accepted
- Date: 2026-06-03

## Context

The project requires both **TDD** (fast unit tests) and **BDD** (business-readable
scenarios) from the start. The team has chosen **MSTest** and **Moq** for unit testing.
We must pick a BDD framework and supporting libraries that are current and maintained.

## Decision

- **Unit testing:** MSTest (`Microsoft.NET.Test.Sdk` + `MSTest.TestFramework` + `MSTest.TestAdapter`).
- **Mocking:** Moq.
- **BDD:** **Reqnroll** with the `Reqnroll.MsTest` adapter, using Gherkin `.feature` files.
- **API integration:** `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`).
- **Assertions:** MSTest `Assert` by default.
- **Coverage:** coverlet + ReportGenerator.

## Rationale

- **Reqnroll over SpecFlow:** SpecFlow is **end-of-life / no longer maintained**; Reqnroll
  is its actively maintained open-source successor and is API-compatible with SpecFlow
  Gherkin/step definitions. It integrates cleanly with MSTest, satisfying the "BDD + MSTest"
  requirement.
- **Moq:** requested by the team and widely used. Pin a **stable version** (e.g. 4.18.x) to
  avoid the SponsorLink behaviour introduced in 4.20.0. NSubstitute is a viable alternative
  if Moq versioning becomes a concern.
- **MSTest:** team standard; first-class in the .NET SDK and CI.
- **Assertions caution:** `FluentAssertions` **v8+ is commercially licensed**. For this repo
  we default to MSTest `Assert`; if fluent style is desired, prefer **Shouldly** (free) or
  pin `FluentAssertions` 7.x. Documented so contributors don't accidentally adopt a paid lib.

## Consequences

- Two test styles coexist: fast unit/use-case tests (MSTest+Moq) and behaviour specs
  (Reqnroll). BDD step definitions can target the use case (fast) or the API (full-stack).
- Versions of Moq and any assertion lib are pinned and reviewed (supply-chain hygiene).

## Alternatives considered

- **xUnit / NUnit** — fine choices, but the team standardized on MSTest.
- **SpecFlow** — rejected: unmaintained / end-of-life.
- **Plain unit tests only (no BDD)** — rejected: requirement explicitly asks for BDD.
