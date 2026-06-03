# Milestone 0: Solution Scaffold — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Scaffold the complete .NET solution structure with all projects (Domain, Application, Infrastructure, API, Web, BDD tests) and wire them into the .slnx, ready for implementation.

**Architecture:** Create a Clean Architecture skeleton with inbound-only dependencies (Domain → Application → Infrastructure/Api/Web). All projects reference each other correctly. Gherkin features are in place, ready to drive TDD.

**Tech Stack:** .NET 10, Reqnroll + MSTest, EF Core 9, ASP.NET Core 10, OpenTelemetry.

---

## File Structure Overview

```
UnitConverter/
├── src/
│   ├── UnitConverter.Domain/
│   │   └── UnitConverter.Domain.csproj
│   ├── UnitConverter.Application/
│   │   └── UnitConverter.Application.csproj
│   ├── UnitConverter.Infrastructure/
│   │   └── UnitConverter.Infrastructure.csproj
│   ├── UnitConverter.Api/
│   │   ├── UnitConverter.Api.csproj
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── UnitConverter.Web/
│   │   └── UnitConverter.Web.csproj
│   └── UnitConverter.ServiceDefaults/
│       └── UnitConverter.ServiceDefaults.csproj
├── tests/
│   ├── UnitConverter.Domain.Tests/
│   ├── UnitConverter.Application.Tests/
│   ├── UnitConverter.Api.Tests/
│   ├── UnitConverter.Bdd.Tests/
│   │   ├── Features/
│   │   │   ├── Conversions.feature ✅ (already created)
│   │   │   ├── UnitManagement.feature ✅ (already created)
│   │   │   ├── ApprovalWorkflow.feature ✅ (already created)
│   │   │   ├── Authorization.feature ✅ (already created)
│   │   │   └── Security.feature ✅ (already created)
│   │   ├── StepDefinitions/ (stub)
│   │   └── Hooks/ (stub)
│   └── UnitConverter.Load.Tests/
├── perf/
│   └── UnitConverter.Benchmarks/
├── UnitConverter.slnx (updated)
├── .editorconfig (new)
└── DEPENDENCIES.md ✅ (already created)
```

---

## Task 1: Clean Up Repository Root

**Files:**
- Delete: `IUnitConverter` (stray 0-byte file)

**Steps:**

- [ ] **Step 1: Verify the stray file exists**

Run: `ls -la | grep IUnitConverter` (or `Get-Item IUnitConverter` on Windows)

Expected: File exists at repository root

- [ ] **Step 2: Delete the file**

Run: `rm IUnitConverter` (or `Remove-Item IUnitConverter` on Windows)

- [ ] **Step 3: Verify deletion**

Run: `git status`

Expected: `IUnitConverter` shown as deleted (if tracked) or no longer present

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "chore: remove stray IUnitConverter file"
```

---

## Task 2: Create Solution Directory Structure

**Files:**
- Create: `src/` directory
- Create: `tests/` directory
- Create: `perf/` directory
- Create: `docs/superpowers/plans/` directory

**Steps:**

- [ ] **Step 1: Verify current directory**

Run: `pwd` (or `pwd` on Windows PowerShell)

Expected: Output shows `/c-Users-supekar-Source-repos-mylearning-aspnercore-TestAspire-UnitConverter` or similar

- [ ] **Step 2: Create directories**

Run: `mkdir -p src tests perf docs/superpowers/plans`

- [ ] **Step 3: Verify structure**

Run: `ls -la`

Expected: Directories `src/`, `tests/`, `perf/`, `docs/` exist

---

## Task 3: Move Existing Projects Under `src/`

**Files:**
- Move: `UnitConverter.API/` → `src/UnitConverter.Api/`
- Move: `UnitConverter.Domain/` → `src/UnitConverter.Domain/`
- Rename: `UnitConverter.Api.csproj` to match casing (`.Api` not `.API`)

**Steps:**

- [ ] **Step 1: Verify existing projects**

Run: `ls -la | grep -i unitconverter`

Expected: See `UnitConverter.API`, `UnitConverter.Domain`

- [ ] **Step 2: Move API project**

Run: `mv UnitConverter.API src/UnitConverter.Api`

- [ ] **Step 3: Move Domain project**

Run: `mv UnitConverter.Domain src/UnitConverter.Domain`

- [ ] **Step 4: Verify moves**

Run: `ls -la src/`

Expected: Both `UnitConverter.Api/` and `UnitConverter.Domain/` present

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: move existing projects under src/ directory"
```

---

## Task 4: Create Source Layer Projects (Application, Infrastructure, Web, ServiceDefaults)

**Files:**
- Create: `src/UnitConverter.Application/UnitConverter.Application.csproj`
- Create: `src/UnitConverter.Infrastructure/UnitConverter.Infrastructure.csproj`
- Create: `src/UnitConverter.Web/UnitConverter.Web.csproj`
- Create: `src/UnitConverter.ServiceDefaults/UnitConverter.ServiceDefaults.csproj`

**Steps:**

- [ ] **Step 1: Create Application project**

Run: `dotnet new classlib -n UnitConverter.Application -o src/UnitConverter.Application`

- [ ] **Step 2: Create Infrastructure project**

Run: `dotnet new classlib -n UnitConverter.Infrastructure -o src/UnitConverter.Infrastructure`

- [ ] **Step 3: Create Web project (Razor Pages)**

Run: `dotnet new webapp -n UnitConverter.Web -o src/UnitConverter.Web`

(This creates a web project with Pages directory; we'll customize later)

- [ ] **Step 4: Create ServiceDefaults project**

Run: `dotnet new classlib -n UnitConverter.ServiceDefaults -o src/UnitConverter.ServiceDefaults`

- [ ] **Step 5: Verify all created**

Run: `ls -la src/`

Expected: Four new directories

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: create Application, Infrastructure, Web, ServiceDefaults projects"
```

---

## Task 5: Create Test Layer Projects

**Files:**
- Create: `tests/UnitConverter.Domain.Tests/UnitConverter.Domain.Tests.csproj`
- Create: `tests/UnitConverter.Application.Tests/UnitConverter.Application.Tests.csproj`
- Create: `tests/UnitConverter.Api.Tests/UnitConverter.Api.Tests.csproj`
- Create: `tests/UnitConverter.Bdd.Tests/UnitConverter.Bdd.Tests.csproj`
- Create: `tests/UnitConverter.Load.Tests/UnitConverter.Load.Tests.csproj`

**Steps:**

- [ ] **Step 1: Create Domain tests**

Run: `dotnet new mstest -n UnitConverter.Domain.Tests -o tests/UnitConverter.Domain.Tests`

- [ ] **Step 2: Create Application tests**

Run: `dotnet new mstest -n UnitConverter.Application.Tests -o tests/UnitConverter.Application.Tests`

- [ ] **Step 3: Create API tests**

Run: `dotnet new mstest -n UnitConverter.Api.Tests -o tests/UnitConverter.Api.Tests`

- [ ] **Step 4: Create BDD tests (Reqnroll)**

Run: `dotnet new classlib -n UnitConverter.Bdd.Tests -o tests/UnitConverter.Bdd.Tests`

(We'll add Reqnroll NuGet in the next task; for now, create as class library)

- [ ] **Step 5: Create Load tests**

Run: `dotnet new mstest -n UnitConverter.Load.Tests -o tests/UnitConverter.Load.Tests`

- [ ] **Step 6: Verify all created**

Run: `ls -la tests/`

Expected: Five test directories

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: create test project structure (Domain, Application, Api, Bdd, Load)"
```

---

## Task 6: Create Performance Projects

**Files:**
- Create: `perf/UnitConverter.Benchmarks/UnitConverter.Benchmarks.csproj`

**Steps:**

- [ ] **Step 1: Create Benchmarks project**

Run: `dotnet new classlib -n UnitConverter.Benchmarks -o perf/UnitConverter.Benchmarks`

- [ ] **Step 2: Verify created**

Run: `ls -la perf/`

Expected: `UnitConverter.Benchmarks/` directory

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: create performance benchmarking project"
```

---

## Task 7: Wire Project References (Inbound Dependencies Only)

**Files:**
- Modify: `src/UnitConverter.Application/UnitConverter.Application.csproj` (ref Domain)
- Modify: `src/UnitConverter.Infrastructure/UnitConverter.Infrastructure.csproj` (ref Application)
- Modify: `src/UnitConverter.Api/UnitConverter.Api.csproj` (ref Application, Infrastructure)
- Modify: `src/UnitConverter.Web/UnitConverter.Web.csproj` (ref Application, Infrastructure)
- Modify: `src/UnitConverter.ServiceDefaults/UnitConverter.ServiceDefaults.csproj` (no refs from domain/app/infra)
- Modify: All test projects (ref relevant source project)

**Steps:**

- [ ] **Step 1: Wire Application → Domain**

Run: `dotnet add src/UnitConverter.Application reference src/UnitConverter.Domain`

- [ ] **Step 2: Wire Infrastructure → Application**

Run: `dotnet add src/UnitConverter.Infrastructure reference src/UnitConverter.Application`

- [ ] **Step 3: Wire API → Application, Infrastructure**

Run: `dotnet add src/UnitConverter.Api reference src/UnitConverter.Application src/UnitConverter.Infrastructure`

- [ ] **Step 4: Wire Web → Application, Infrastructure**

Run: `dotnet add src/UnitConverter.Web reference src/UnitConverter.Application src/UnitConverter.Infrastructure`

- [ ] **Step 5: Wire Domain tests → Domain**

Run: `dotnet add tests/UnitConverter.Domain.Tests reference src/UnitConverter.Domain`

- [ ] **Step 6: Wire Application tests → Application, Domain**

Run: `dotnet add tests/UnitConverter.Application.Tests reference src/UnitConverter.Application src/UnitConverter.Domain`

- [ ] **Step 7: Wire API tests → Api, Infrastructure, Application**

Run: `dotnet add tests/UnitConverter.Api.Tests reference src/UnitConverter.Api src/UnitConverter.Infrastructure src/UnitConverter.Application`

- [ ] **Step 8: Wire BDD tests → Application, Api (for WebApplicationFactory)**

Run: `dotnet add tests/UnitConverter.Bdd.Tests reference src/UnitConverter.Application src/UnitConverter.Api`

- [ ] **Step 9: Wire Load tests → Api**

Run: `dotnet add tests/UnitConverter.Load.Tests reference src/UnitConverter.Api`

- [ ] **Step 10: Wire Benchmarks → Domain, Application**

Run: `dotnet add perf/UnitConverter.Benchmarks reference src/UnitConverter.Domain src/UnitConverter.Application`

- [ ] **Step 11: Verify references**

Run: `dotnet build` (should compile without errors)

Expected: BUILD SUCCESSFUL

- [ ] **Step 12: Commit**

```bash
git add -A
git commit -m "feat: wire project references (inbound-only dependencies per Clean Architecture)"
```

---

## Task 8: Add NuGet Packages to Test Projects

**Files:**
- Modify: `tests/UnitConverter.Application.Tests/UnitConverter.Application.Tests.csproj` (add Moq)
- Modify: `tests/UnitConverter.Bdd.Tests/UnitConverter.Bdd.Tests.csproj` (add Reqnroll)
- Modify: `tests/UnitConverter.Api.Tests/UnitConverter.Api.Tests.csproj` (add WebApplicationFactory, coverlet)
- Modify: `tests/UnitConverter.Load.Tests/UnitConverter.Load.Tests.csproj` (add NBomber)
- Modify: `perf/UnitConverter.Benchmarks/UnitConverter.Benchmarks.csproj` (add BenchmarkDotNet)

**Steps:**

- [ ] **Step 1: Add Moq to Application tests**

Run: `dotnet add tests/UnitConverter.Application.Tests package Moq --version 4.18.4`

(Pin 4.18.x to avoid SponsorLink in 4.20+)

- [ ] **Step 2: Add Reqnroll to BDD tests**

Run: `dotnet add tests/UnitConverter.Bdd.Tests package Reqnroll --version 1.0.0`

Run: `dotnet add tests/UnitConverter.Bdd.Tests package Reqnroll.MsTest --version 1.0.0`

- [ ] **Step 3: Add WebApplicationFactory to API tests**

Run: `dotnet add tests/UnitConverter.Api.Tests package Microsoft.AspNetCore.Mvc.Testing`

- [ ] **Step 4: Add coverage to API tests**

Run: `dotnet add tests/UnitConverter.Api.Tests package coverlet.collector`

- [ ] **Step 5: Add NBomber to Load tests**

Run: `dotnet add tests/UnitConverter.Load.Tests package NBomber`

Run: `dotnet add tests/UnitConverter.Load.Tests package NBomber.Http`

- [ ] **Step 6: Add BenchmarkDotNet to Benchmarks**

Run: `dotnet add perf/UnitConverter.Benchmarks package BenchmarkDotNet`

- [ ] **Step 7: Verify builds**

Run: `dotnet build`

Expected: BUILD SUCCESSFUL

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: add NuGet test dependencies (Moq, Reqnroll, WebApplicationFactory, NBomber, BenchmarkDotNet)"
```

---

## Task 9: Copy Gherkin Features to BDD Project

**Files:**
- Copy: `tests/UnitConverter.Bdd.Tests/Features/Conversions.feature` ✅ (already created)
- Copy: `tests/UnitConverter.Bdd.Tests/Features/UnitManagement.feature` ✅ (already created)
- Copy: `tests/UnitConverter.Bdd.Tests/Features/ApprovalWorkflow.feature` ✅ (already created)
- Copy: `tests/UnitConverter.Bdd.Tests/Features/Authorization.feature` ✅ (already created)
- Copy: `tests/UnitConverter.Bdd.Tests/Features/Security.feature` ✅ (already created)

**Steps:**

- [ ] **Step 1: Verify Gherkin files exist in temp location**

Run: `ls -la tests/UnitConverter.Bdd.Tests/Features/`

Expected: 5 `.feature` files exist

(If not, they were created earlier in the project; skip to Step 2)

- [ ] **Step 2: Verify all feature files are in place**

Run: `git status | grep -i feature`

Expected: See Conversions.feature, UnitManagement.feature, etc. (untracked)

- [ ] **Step 3: Create stub directories**

Run: `mkdir -p tests/UnitConverter.Bdd.Tests/StepDefinitions tests/UnitConverter.Bdd.Tests/Hooks`

- [ ] **Step 4: Create appsettings.json for BDD tests**

Create file: `tests/UnitConverter.Bdd.Tests/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

- [ ] **Step 5: Commit**

```bash
git add tests/UnitConverter.Bdd.Tests/Features/
git add tests/UnitConverter.Bdd.Tests/appsettings.json
git commit -m "feat: add Gherkin feature files (Conversions, UnitManagement, Approval, Authorization, Security)"
```

---

## Task 10: Update Solution File (.slnx)

**Files:**
- Modify: `UnitConverter.slnx`

**Steps:**

- [ ] **Step 1: Read current .slnx**

Run: `cat UnitConverter.slnx`

Expected: Currently only references `UnitConverter.API/UnitConverter.API.csproj`

- [ ] **Step 2: Backup current .slnx**

Run: `cp UnitConverter.slnx UnitConverter.slnx.bak`

- [ ] **Step 3: Update .slnx to include all projects**

Replace entire content with:

```xml
<Solution>
  <Project Path="src/UnitConverter.Domain/UnitConverter.Domain.csproj" />
  <Project Path="src/UnitConverter.Application/UnitConverter.Application.csproj" />
  <Project Path="src/UnitConverter.Infrastructure/UnitConverter.Infrastructure.csproj" />
  <Project Path="src/UnitConverter.Api/UnitConverter.Api.csproj" />
  <Project Path="src/UnitConverter.Web/UnitConverter.Web.csproj" />
  <Project Path="src/UnitConverter.ServiceDefaults/UnitConverter.ServiceDefaults.csproj" />

  <Project Path="tests/UnitConverter.Domain.Tests/UnitConverter.Domain.Tests.csproj" />
  <Project Path="tests/UnitConverter.Application.Tests/UnitConverter.Application.Tests.csproj" />
  <Project Path="tests/UnitConverter.Api.Tests/UnitConverter.Api.Tests.csproj" />
  <Project Path="tests/UnitConverter.Bdd.Tests/UnitConverter.Bdd.Tests.csproj" />
  <Project Path="tests/UnitConverter.Load.Tests/UnitConverter.Load.Tests.csproj" />

  <Project Path="perf/UnitConverter.Benchmarks/UnitConverter.Benchmarks.csproj" />
</Solution>
```

- [ ] **Step 4: Verify .slnx is valid**

Run: `dotnet sln`

Expected: Lists all projects (or at least doesn't error)

- [ ] **Step 5: Verify solution loads**

Run: `dotnet build`

Expected: BUILD SUCCESSFUL (or shows warnings only for projects with no code)

- [ ] **Step 6: Commit**

```bash
git add UnitConverter.slnx
git commit -m "feat: update solution file to include all projects (src, tests, perf)"
```

---

## Task 11: Create .editorconfig (Code Style)

**Files:**
- Create: `.editorconfig`

**Steps:**

- [ ] **Step 1: Create .editorconfig**

Create file: `.editorconfig`

```ini
root = true

# C# files
[*.cs]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
insert_final_newline = true
trim_trailing_whitespace = true

# Code style
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true
csharp_new_line_between_query_expression_clauses = true

# Indentation preferences
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_indent_labels = one_less_than_current

# Wrapping preferences
csharp_wrap_after_declaration_in_heading = false

# C# coding style rules
csharp_prefer_braces = true
csharp_prefer_simple_using_statement = true

# Null checking preferences
csharp_style_conditional_delegate_call = true

# var preferences
csharp_style_var_for_built_in_types = false
csharp_style_var_when_type_is_apparent = true
csharp_style_var_elsewhere = false

# Naming conventions
dotnet_naming_style.pascal_case_style.required_prefix = 
dotnet_naming_style.pascal_case_style.required_suffix = 
dotnet_naming_style.pascal_case_style.word_separator = 
dotnet_naming_style.pascal_case_style.capitalization = pascal_case

dotnet_naming_rule.types_should_be_pascal_case.severity = suggestion
dotnet_naming_rule.types_should_be_pascal_case.symbols = type_symbols
dotnet_naming_rule.types_should_be_pascal_case.style = pascal_case_style

dotnet_naming_symbols.type_symbols.applicable_kinds = class,struct,interface,enum,delegate
dotnet_naming_symbols.type_symbols.applicable_accessibilities = public,internal,private,protected,protected_internal
```

- [ ] **Step 2: Verify .editorconfig created**

Run: `cat .editorconfig`

Expected: File exists and is readable

- [ ] **Step 3: Commit**

```bash
git add .editorconfig
git commit -m "chore: add .editorconfig for consistent code style (C# formatting)"
```

---

## Task 12: Final Verification & Commit Summary

**Files:**
- Verify: All projects compile
- Verify: Solution structure is correct
- Verify: Gherkin features are in place

**Steps:**

- [ ] **Step 1: Clean build**

Run: `dotnet clean && dotnet build`

Expected: BUILD SUCCESSFUL (warnings OK if projects are empty)

- [ ] **Step 2: List solution structure**

Run: `dotnet sln list`

Expected: 12 projects listed (6 src + 5 tests + 1 perf)

- [ ] **Step 3: Verify Gherkin files**

Run: `find tests/UnitConverter.Bdd.Tests/Features -name "*.feature" | wc -l`

Expected: 5 feature files

- [ ] **Step 4: Verify directory tree**

Run: `tree -I 'bin|obj|.git' -L 2`

Expected: Clean structure matching earlier diagram (or use `ls -laR`)

- [ ] **Step 5: Final commit message**

```bash
git log --oneline -12
```

Expected: See commits for:
- Cleanup (remove stray file)
- Create directories
- Move projects
- Create new projects
- Wire references
- Add NuGet packages
- Add Gherkin features
- Update solution file
- Add .editorconfig

- [ ] **Step 6: Git status**

Run: `git status`

Expected: "On branch main / On branch develop / nothing to commit, working tree clean"

---

## Verification Checklist

Before declaring Milestone 0 complete:

- [ ] No uncommitted changes (`git status` is clean)
- [ ] Solution builds without errors (`dotnet build` succeeds)
- [ ] All 12 projects are in .slnx
- [ ] Clean Architecture dependency order (Domain → Application → Infrastructure/Api/Web)
- [ ] 5 Gherkin feature files in place (`tests/UnitConverter.Bdd.Tests/Features/`)
- [ ] Test projects have required NuGet packages (Moq, Reqnroll, WebApplicationFactory, NBomber, BenchmarkDotNet)
- [ ] .editorconfig present at repository root
- [ ] No stray `IUnitConverter` file
- [ ] Existing projects moved under `src/`

---

## Rollback Plan (If Needed)

If something goes wrong during Milestone 0:

```bash
# Option 1: Revert to last known good state
git log --oneline
git reset --hard <commit-hash>

# Option 2: Restore from backup (if within this session)
cp UnitConverter.slnx.bak UnitConverter.slnx
git checkout src/ tests/ perf/
```

---

## Post-Milestone 0: Next Steps

Once Milestone 0 is complete, proceed to **Milestone 1: Domain Layer (TDD)**

- Implement `Unit`, `Quantity`, `UnitCategory` domain records
- Implement `IConversionRule` + `AffineConversionRule`
- Write unit tests: "1000m → 1km", "0°C → 32°F", incompatible units
- All tests green before moving to Milestone 2

