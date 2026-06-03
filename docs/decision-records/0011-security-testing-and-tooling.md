# ADR-0011 — Security testing & tooling baseline

- Status: Accepted
- Date: 2026-06-03

## Context

We need a comprehensive security posture: static code analysis, dependency scanning, pen testing,
and OWASP compliance checking. This must be automated in CI and integrated with the Aspire
dashboard for runtime visibility.

## Decision

Implement a **four-layer security strategy**:

### Layer 1: Static Code Analysis (SAST)

**Tools:**
- `Microsoft.CodeAnalysis.NetAnalyzers` (built-in, no-cost)
- `SecurityCodeScan.Rules` NuGet (detects injection, hardcoded secrets, weak crypto)
- `NDepend` (optional; visualizes architecture, dead code, violations)

**CI gate:** `dotnet build` fails on rule violations (configurable severity).

**Rules to enable (SonarQube-equivalent):**
- `CA1806` — unused method results
- `CA2000` — dispose IDisposable
- `CA2213` — dispose IDisposable members
- `CA2215` — Dispose calls base
- `SCS0021` — hardcoded password
- `SCS0035` — weak crypto (MD5, SHA1)

### Layer 2: Dependency Scanning (SCA)

**Tools:**
- `dotnet list package --vulnerable` (built-in, runs locally)
- `GitHub Dependabot` (free, auto-opens PRs for updates) — enable in repo settings
- `Snyk` (cloud, more aggressive; free tier available)
- `OWASP Dependency-Check` (open-source)

**CI gate:** fail on critical/high vulnerabilities (configurable for medium).

**Frequency:** on commit (fast, cached) + nightly deep scan.

### Layer 3: Pen Testing / OWASP Top 10 (DAST)

**Tools:**
- `OWASP ZAP` (free, spidering + scanning) — run in CI against a staging instance
- **NBomber** (already in-repo) — fuzz payloads (e.g., SQL injection patterns in unit names)
- **Reqnroll BDD tests** (already in-repo) — explicit security scenarios (`@security` tag)

**Example Reqnroll scenario:**
```gherkin
@api @security
Scenario: SQL injection in unit name is rejected
    Given I am authenticated as an employee
    When I POST a new unit with name "'; DROP TABLE Units; --"
    Then the response status is 400
    And the error message contains "Invalid unit name"
```

**CI gate:** security feature tests must pass; ZAP WARN+ findings trigger review.

### Layer 4: Compliance & Audit

**Checklist:** `docs/security/OWASP-top-10-checklist.md` tracks coverage:
- A01:2021 – Broken Access Control → policy-based authz ✅
- A02:2021 – Cryptographic Failures → HTTPS + encrypted secrets ✅
- A03:2021 – Injection → parameterized queries (EF Core) ✅
- A04:2021 – Insecure Design → threat model ✅
- etc.

**Threat model:** `docs/security/threat-model.md` identifies top risks and mitigations.

**Audit trail:** soft deletes + `CreatedBy`, `ModifiedBy`, `CreatedAt`, `ModifiedAt` on all entities.

### Runtime Visibility (Aspire dashboard)

- Monitor **error spikes** (logs, traces) → potential attacks
- Watch **conversion.errors.count** metric → invalid inputs being rejected correctly
- Check **latency histograms** → DoS detection (sudden spike in slow requests)

## Rationale

- **Layered defense:** static + dependency + runtime + manual checks catch different risks.
- **Automated gates:** CI fails on violations; security is not optional.
- **OWASP alignment:** industry-standard threat model; auditors recognize it.
- **Developer friction:** security is built-in, not an afterthought.

## Consequences

- **CI overhead:** dependency scan (~10s), SAST (included in build), ZAP scan (~2min) — budget for longer pipelines.
- **False positives:** SAST/SCA tools sometimes flag benign code; maintain an exception list.
- **Ongoing maintenance:** rules + tools evolve; keep NuGet packages updated.
- **Security training:** team must understand the rules and when to override (never without review).

## Alternatives considered

- **No automated testing** — rejected; manual security reviews are unreliable at scale.
- **Single tool (e.g., Snyk only)** — rejected; layered coverage is more robust.
- **Ignore low/medium findings** — rejected; today's low is tomorrow's exploit; track all.
