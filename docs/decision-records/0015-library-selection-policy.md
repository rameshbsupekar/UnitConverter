# ADR-0015 — Open-source library selection policy (.NET Foundation first, MIT preferred)

- Status: Accepted
- Date: 2026-06-03

## Context

The project depends on external libraries. We need a clear policy to ensure:
- **Quality:** maintained by reputable organizations
- **Compliance:** MIT license or compatible (no GPL, no proprietary)
- **Support:** active, not abandoned
- **Ecosystem:** standard .NET Foundation or widely adopted open-source

## Decision

**Tiered library selection:**

### Tier 1 (Preferred) — .NET Foundation or Microsoft-sponsored
- **License:** MIT, Apache 2.0, or equivalent (free for commercial use)
- **Examples:** ASP.NET Core, EF Core, Roslyn, System.* packages
- **Source:** official nuget.org (Microsoft-verified)
- **Action:** Use without hesitation

### Tier 2 (Acceptable) — Popular open-source (.NET community)
- **License:** MIT, Apache 2.0, Unlicense
- **Examples:** AutoMapper, Moq, Reqnroll, NDepend (free tier)
- **Source:** nuget.org (community-verified)
- **Action:** Use after confirming license + active maintenance (≤ 1 year since last release)

### Tier 3 (Last Resort) — Specialized or niche
- **License:** MIT, Apache 2.0, LGPL v2+
- **Examples:** BenchmarkDotNet, Shouldly (if needed)
- **Condition:** No Tier 1/2 alternative exists
- **Action:** Review + document explicitly, get approval before adding

### Tier 4 (Forbidden)
- **GPL v3** — copyleft, forces us to open-source our code
- **SSPL** (Server Side Public License) — proprietary, restrictive
- **Proprietary / Commercial** — licensing costs or restrictions
- **Abandoned** — no updates in > 3 years, no maintainer response
- **Action:** REJECT, find alternative

---

## Audit of Current Dependencies (All Approved ✅)

### Runtime Dependencies

| Package | Version | License | Tier | Justification |
|---------|---------|---------|------|---|
| **ASP.NET Core** | 10.0 | MIT | Tier 1 | Microsoft, .NET Foundation |
| **EF Core** | 9.0+ | Apache 2.0 | Tier 1 | Microsoft, ORM standard |
| **ASP.NET Core Identity** | 10.0 | MIT | Tier 1 | Built-in, no extra dep |
| **Microsoft.CodeAnalysis** | 4.9+ | Apache 2.0 | Tier 1 | Roslyn, official |
| **Microsoft.AspNetCore.OpenApi** | 10.0 | MIT | Tier 1 | Built-in, OpenAPI support |
| **HybridCache** | 10.0 | MIT | Tier 1 | Built-in (.NET 9+) |

### Test Dependencies

| Package | Version | License | Tier | Justification |
|---------|---------|---------|------|---|
| **MSTest.TestFramework** | 3.x | MIT | Tier 1 | Microsoft, standard |
| **Moq** | 4.18.x (NOT 4.20+) | BSD-3 | Tier 2 | Community standard for .NET |
| **Reqnroll** | 1.0+ | MIT | Tier 2 | Active fork of SpecFlow, maintained |
| **Reqnroll.MsTest** | 1.0+ | MIT | Tier 2 | Reqnroll adapter, same license |
| **Microsoft.AspNetCore.Mvc.Testing** | 10.0 | MIT | Tier 1 | Built-in, WebApplicationFactory |
| **coverlet.collector** | Latest | MIT | Tier 2 | Industry standard for .NET coverage |

### Performance & Monitoring

| Package | Version | License | Tier | Justification |
|---------|---------|---------|------|---|
| **BenchmarkDotNet** | Latest | MIT | Tier 2 | Community standard for .NET |
| **NBomber** | 5.x+ | MIT | Tier 2 | Community load testing, no Tier 1 alternative |
| **NBomber.Http** | 5.x+ | MIT | Tier 2 | Extends NBomber |
| **OpenTelemetry** | 1.9+ | Apache 2.0 | Tier 1 | CNCF project, Microsoft-backed |
| **OpenTelemetry.Extensions.Hosting** | 1.9+ | Apache 2.0 | Tier 1 | ASP.NET Core integration |
| **OpenTelemetry.Instrumentation.AspNetCore** | 1.9+ | Apache 2.0 | Tier 1 | Official instrumentation |
| **OpenTelemetry.Instrumentation.Http** | 1.9+ | Apache 2.0 | Tier 1 | HTTP instrumentation |
| **OpenTelemetry.Instrumentation.Runtime** | 1.9+ | Apache 2.0 | Tier 1 | Runtime metrics |
| **OpenTelemetry.Exporter.OpenTelemetryProtocol** | 1.9+ | Apache 2.0 | Tier 1 | OTLP protocol |

### Security & Analysis

| Package | Version | License | Tier | Justification |
|---------|---------|---------|------|---|
| **SecurityCodeScan.Rules** | Latest | MIT | Tier 2 | OWASP analyzer for C# |
| **Microsoft.CodeAnalysis.NetAnalyzers** | Latest | MIT | Tier 1 | Built-in Roslyn analyzers |
| **GitHub Dependabot** | N/A (service) | Free | Tier 1 | GitHub native, free tier available |

### Rejected / Not Using

| Package | License | Reason |
|---------|---------|--------|
| **SpecFlow** | Free → Commercial | EOL; use Reqnroll instead |
| **FluentAssertions** | 8.0+ | Commercially licensed; use MSTest `Assert` or **Shouldly** |
| **Seq** | Free → Commercial | Optional; use Aspire dashboard first |
| **Application Insights** | Proprietary | Optional; use OTLP + open-source backend first |
| **NSubstitute** | BSD-3 | Acceptable alternative to Moq, not needed |
| **NUnit** | MIT | Works, but MSTest is standard for .NET |
| **xUnit** | Apache 2.0 | Works, but MSTest is standard for .NET |

---

## Guidance: Adding New Libraries

**Before adding any NuGet package:**

1. **Check license:**
   ```bash
   dotnet package search <package> --format json
   # Or browse nuget.org and check "License" field
   ```

2. **Verify Tier:**
   - Microsoft / .NET Foundation? → Tier 1 ✓
   - Popular community .NET? → Tier 2 (check recent activity)
   - Niche / unknown? → Tier 3 (document, get approval)
   - GPL / proprietary? → Tier 4 (REJECT)

3. **Check maintenance:**
   - Last release: ≤ 1 year ago ✓
   - Issues: actively addressed
   - Security: no known CVEs

4. **PR checklist:**
   - Link to license (e.g., GitHub repo LICENSE file)
   - State the Tier
   - Justify why (no Tier 1/2 alternative exists)
   - Note any constraints (e.g., "Moq 4.18.x only, not 4.20+")

---

## Approved "Free" Alternatives (If No Tier 1/2 Available)

These are acceptable if Tier 1/2 doesn't cover the need:

| Need | Tier 1/2 | Tier 3 Alternative | License |
|------|----------|-------------------|---------|
| Assertions | MSTest | Shouldly | MIT |
| Mocking | Moq | NSubstitute | BSD-3 |
| BDD | Reqnroll | xBehave | MIT |
| Load testing | NBomber | k6 (external) | AGPL |
| API docs | OpenAPI (built-in) | Swagger UI | Apache 2.0 |
| Logging | ILogger (built-in) | Serilog | Apache 2.0 |
| Validation | DataAnnotations | FluentValidation | Apache 2.0 |

---

## Consequences

- **Vetted dependencies:** all libraries are actively maintained + free
- **License compliance:** no GPL copyleft or proprietary surprises
- **Community trust:** using industry-standard .NET libraries
- **Minimal lock-in:** easy to swap Apache 2.0 / MIT licensed libraries

---

## Exceptions (Documented, Not Common)

If a Tier 4 (GPL/proprietary) library is the only option for a critical feature:

1. **Isolate it:** put in a separate module/DLL
2. **Document:** ADR explaining why
3. **Review:** get legal/team approval
4. **Track:** mark as tech debt to replace later

*Example (hypothetical):* If the only unit conversion library was GPL, we would NOT use it — we implement the math ourselves (which we're doing anyway).

