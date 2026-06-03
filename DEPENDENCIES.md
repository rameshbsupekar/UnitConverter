# Dependencies & License Compliance

**Policy:** .NET Foundation + MIT license preferred; all free and open-source.

**See** [`docs/decision-records/0015-library-selection-policy.md`](decision-records/0015-library-selection-policy.md) for the full policy.

---

## Current Dependencies (All Approved ✅)

### Core Framework (Tier 1 — Microsoft / .NET Foundation)

```
ASP.NET Core 10.0                           [MIT]
Entity Framework Core 9.0+                  [Apache 2.0]
ASP.NET Core Identity 10.0                  [MIT]
HybridCache (.NET 9+)                       [MIT]
Microsoft.AspNetCore.OpenApi 10.0           [MIT]
Microsoft.CodeAnalysis 4.9+                 [Apache 2.0]
Microsoft.AspNetCore.Mvc.Testing 10.0       [MIT]
Microsoft.CodeAnalysis.NetAnalyzers         [MIT]
```

### Resilience & Reliability (Tier 1 — Microsoft / .NET Foundation)

```
Microsoft.Extensions.Resilience 10.0+       [MIT]           (Polly v8-based general resilience)
Microsoft.Extensions.Http.Resilience 10.0+  [MIT]           (Polly v8-based HTTP resilience)
Microsoft.AspNetCore.RateLimiting 10.0       [MIT]           (Native ASP.NET Core rate limiting)
```

### Testing (Tier 1 + Tier 2)

```
MSTest.TestFramework 3.x                    [MIT]           (Microsoft)
Moq 4.18.x                                  [BSD-3]         (Community — NOT 4.20+)
Reqnroll 1.0+                               [MIT]           (Community fork of SpecFlow)
Reqnroll.MsTest 1.0+                        [MIT]           (Community)
coverlet.collector                          [MIT]           (Community coverage)
```

### Observability (Tier 1 — CNCF / Microsoft-backed)

```
OpenTelemetry 1.9+                          [Apache 2.0]    (CNCF)
OpenTelemetry.Extensions.Hosting 1.9+       [Apache 2.0]    (CNCF)
OpenTelemetry.Instrumentation.AspNetCore    [Apache 2.0]    (CNCF)
OpenTelemetry.Instrumentation.Http          [Apache 2.0]    (CNCF)
OpenTelemetry.Instrumentation.Runtime       [Apache 2.0]    (CNCF)
OpenTelemetry.Exporter.OpenTelemetryProtocol [Apache 2.0]   (CNCF)
```

### Performance (Tier 2 — Community, no Tier 1 alternative)

```
BenchmarkDotNet                             [MIT]           (Community, industry standard)
NBomber 5.x+                                [MIT]           (Community, only .NET load test)
NBomber.Http 5.x+                           [MIT]           (Community)
```

### Security & Analysis (Tier 2)

```
SecurityCodeScan.Rules                      [MIT]           (Community OWASP analyzer)
GitHub Dependabot                           [Free]          (GitHub native SCA)
```

### Optional (If Added)

```
Shouldly (assertions alternative)           [MIT]           (Tier 3, replace if FluentAssertions used)
Serilog (structured logging)                [Apache 2.0]    (Tier 2, alternative to ILogger)
FluentValidation                            [Apache 2.0]    (Tier 2, alternative to DataAnnotations)
NDepend (code analysis)                     [Proprietary]   (Free tier available)
Seq (centralized logging)                   [Proprietary]   (Free tier; optional)
Application Insights                        [Proprietary]   (Azure service; optional)
```

---

## License Summary

| License | Count | Examples |
|---------|-------|----------|
| **MIT** | 12+ | OpenTelemetry, Moq, Reqnroll, BenchmarkDotNet, NBomber |
| **Apache 2.0** | 5+ | EF Core, CodeAnalysis, OpenTelemetry ecosystem |
| **BSD-3** | 1 | Moq |
| **Free (GitHub)** | 1 | Dependabot |

**Commercial/Proprietary:** 0 required, 3 optional (NDepend, Seq, AppInsights — all free tiers available)

---

## Adding New Libraries

**Checklist before adding to a `.csproj`:**

- [ ] License is MIT, Apache 2.0, or equivalent (free for commercial use)
- [ ] Last release: ≤ 1 year ago (actively maintained)
- [ ] No known security CVEs
- [ ] Tier 1/2 (or document Tier 3 exception)
- [ ] PR includes license link + justification

---

## Audit Tools

### Check licenses locally

```bash
# List all packages with licenses
dotnet list package --format json | grep -i license

# Or inspect nuget.org directly (web browser)
# Go to https://www.nuget.org/packages/<PackageName>
# Check "License" field and GitHub repo
```

### Monitor for vulnerabilities

```bash
# Built-in vulnerability check
dotnet list package --vulnerable

# Or use Dependabot (GitHub free feature)
# Settings → Code security and analysis → Enable Dependabot
```

---

## Compliant: Yes ✅

All current and planned dependencies are:
- ✅ MIT, Apache 2.0, or BSD-3 license
- ✅ Free for commercial use
- ✅ Actively maintained
- ✅ Open-source

