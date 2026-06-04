# Local development guide (Aspire / TestAspire)

Step-by-step workflow for running **UnitConverter** on a new machine. Databases are created **before** you start the APIs, using **EF Core Code First migrations** and an explicit setup tool—not `EnsureCreated` on application startup.

## Prerequisites

| Requirement | Notes |
|-------------|--------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Solution targets `net10.0` |
| Git | Clone this repository |
| PowerShell | For `scripts/database/*.ps1` (Windows); use equivalent `dotnet` commands on Linux/macOS below |

Optional: [Visual Studio 2022](https://visualstudio.microsoft.com/) or VS Code with C# Dev Kit.

## Architecture (local MVP)

| Service | Project | URL (HTTPS profile) | SQLite file |
|---------|---------|---------------------|-------------|
| User management | `src/UnitConverter.UserManagement.Api` | https://localhost:7180 | `Data/UserManagement/auth.db` |
| Units & conversion | `src/UnitConverter.UnitsDefinitions.Api` | https://localhost:7100 | `Data/UnitsMaster/catalog.db` |

JWT is issued by Auth; the Conversion API validates the same `Jwt` settings (`Secret`, `Issuer`, `Audience`).

**Route ownership:** `UserManagement.Api` exposes **only** `/api/v1/users`, `/sessions`, and `/sessions/refresh`. Catalog, `/unit-conversions`, and `/unit-definitions` live on `UnitsDefinitions.Api` (port **5173** / **7100**). Calling unit routes on the auth port returns **404**.

> **Aspire AppHost** is planned (orchestration + dashboard). For now, run the two APIs in separate terminals. When AppHost is added, run it instead of the two `dotnet run` commands below—it should call the same database setup script first.

## First-time setup

From the **repository root** (`UnitConverter/`, where `UnitConverter.slnx` lives):

### 1. Restore and build

```powershell
dotnet restore
dotnet build
```

### 2. Install EF CLI (once per machine)

The repo pins the tool in `.config/dotnet-tools.json`:

```powershell
dotnet tool restore
```

### 3. Create databases (migrations + seed)

**Required before first API run.**

```powershell
.\scripts\database\setup-local.ps1
```

This runs `tools/UnitConverter.DbSetup`, which:

1. Ensures `Data/UserManagement/` and `Data/UnitsMaster/` exist
2. Applies all EF migrations to `auth.db` and `catalog.db`
3. Seeds development roles, test users, and the unit catalog (idempotent)

**Linux / macOS** (same tool, from repo root):

```bash
dotnet run --project tools/UnitConverter.DbSetup/UnitConverter.DbSetup.csproj -- --seed
```

### 4. Run tests (optional)

```powershell
dotnet test
```

Integration tests use isolated temp databases; they do not use your `Data/*.db` files.

### 5. Start the services

Use **two terminals**:

```powershell
dotnet run --project src/UnitConverter.UserManagement.Api
```

```powershell
dotnet run --project src/UnitConverter.UnitsDefinitions.Api
```

If migrations were not applied, startup **fails** with a message pointing to `setup-local.ps1`.

### 6. Try the APIs

- Auth: `src/UnitConverter.Auth/API/` — register/login samples in your HTTP client
- Conversion: `src/UnitConverter.UnitsDefinitions.Api/UnitConverter.API.http`

In Development, OpenAPI JSON is at `/openapi/v1.json` on each host.

## Development seed data

Password for all test users: **`Test@12345`**

| Email | Role |
|-------|------|
| admin@unitconverter.local | Admin |
| employee@unitconverter.local | Employee |
| partner@unitconverter.local | Partner |
| public@unitconverter.local | Public |

Unit catalog symbols are seeded into `catalog.db` (length, weight, temperature). See [`docs/HLD.md`](HLD.md) for role access rules.

## After pulling new migrations

When a teammate adds EF migrations, update your local databases:

```powershell
.\scripts\database\setup-local.ps1
```

Schema only (no seed):

```powershell
dotnet run --project tools/UnitConverter.DbSetup/UnitConverter.DbSetup.csproj -- --migrate-only
```

## Adding a migration (schema change)

From repo root, with `dotnet tool restore` already done:

```powershell
.\scripts\database\add-migration.ps1 -Context units -Name YourChangeName
.\scripts\database\add-migration.ps1 -Context auth -Name YourChangeName
```

Commit the generated files under:

- `src/UnitConverter.UnitsDefinitions.DataAccess/Migrations/`
- `src/UnitConverter.UserManagement.DataAccess/Migrations/`

Review the migration SQL before merging (see [Microsoft EF nullable guidance](https://github.com/dotnet/skills/blob/main/plugins/dotnet-upgrade/skills/migrate-nullable-references/references/ef-core.md) in [dotnet/skills](https://github.com/dotnet/skills)).

## Reset local databases

1. Stop Auth and Api
2. Delete `Data/UserManagement/auth.db` and `Data/UnitsMaster/catalog.db` (and `-wal` / `-shm` if present)
3. Run `.\scripts\database\setup-local.ps1` again

## Configuration

| Setting | Location | Purpose |
|---------|----------|---------|
| `ConnectionStrings:UserManagement` | Auth `appsettings.json` | Empty → SQLite under `Data/UserManagement/` |
| `ConnectionStrings:UnitsMasterData` | Api `appsettings.json` | Empty → SQLite under `Data/UnitsMaster/` |
| `Jwt:*` | Auth + Api `appsettings.json` | Must match between services |

Cloud deployments: set connection strings in configuration; run migrations as part of deploy (same `DbSetup` or `dotnet ef database update`).

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Startup error: pending migrations | `.\scripts\database\setup-local.ps1` |
| Startup error: cannot connect to database | Run setup script; check `Data/` folders exist |
| `dotnet ef` not found | `dotnet tool restore` from repo root |
| Login fails after reset | Re-run setup with `--seed` (default in `setup-local.ps1`) |
| Port already in use | Change URLs in `Properties/launchSettings.json` |

## Related docs

| Doc | Purpose |
|-----|---------|
| [`Data/README.md`](../Data/README.md) | Database paths, gitignore, migration workflow |
| [`docs/MICROSERVICES-HLD.md`](MICROSERVICES-HLD.md) | Services, roles, endpoints |
| [`docs/DATABASE-SCHEMA.md`](DATABASE-SCHEMA.md) | Tables, indexes, constraints |
| [`docs/schemas/UNIT-DEFINITIONS-SCHEMA.md`](schemas/UNIT-DEFINITIONS-SCHEMA.md) | **Proposed** unit catalog schema (approve before EF change) |
| [`README.md`](../README.md) | API summary and solution overview |

## Future: .NET Aspire AppHost

When `UnitConverter.AppHost` is added:

1. Keep **database setup** as a pre-step (or an AppHost resource that runs `DbSetup` once)
2. AppHost will start Auth + Api + optional Aspire dashboard for OpenTelemetry
3. This guide’s database section stays the same; only the “start services” step moves to `dotnet run --project src/UnitConverter.AppHost`
