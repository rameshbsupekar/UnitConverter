# Unit Conversion API

ASP.NET Core REST API that converts numeric values between units of measurement (length, temperature, and weight/mass). Built for a real-world, team-maintainable structure with clean architecture and automated tests.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (preview; this solution targets `net10.0`)
- Optional: [Visual Studio 2022](https://visualstudio.microsoft.com/) or VS Code with C# Dev Kit

## Run locally

**Full steps (database setup, both services, seed users):** see **[`docs/ASPIRE-DEV-GUIDE.md`](docs/ASPIRE-DEV-GUIDE.md)**.

**Manual testing:** [docs/MANUAL-TESTING.md](docs/MANUAL-TESTING.md) (clone, setup, F5, Scalar steps).

From the repository root:

```powershell
dotnet restore
dotnet build
.\scripts\database\setup-local.ps1
```

**Recommended:** Follow [docs/MANUAL-TESTING.md](docs/MANUAL-TESTING.md) — clone in VS, run setup script, **F5** both APIs.

| Service | HTTPS (Scalar / manual tests) | HTTP |
|---------|-------------------------------|------|
| Units (catalog, convert, unit-definitions) | https://localhost:7100/scalar/v1 | http://localhost:5173 |
| User management (auth, JWT) | https://localhost:7180/scalar/v1 | http://localhost:5180 |

CLI (optional):

```powershell
dotnet run --project src/UnitConverter.UserManagement.Api --launch-profile https
dotnet run --project src/UnitConverter.UnitsDefinitions.Api --launch-profile https
```

**Service boundaries (no duplicate hosts):**

| Host (5180 / 7180) | Host (5173 / 7100) |
|--------------------|--------------------|
| `POST /api/v1/users`, `/sessions`, `/sessions/refresh` only | `GET /catalog/*`, `POST /unit-conversions`, `GET|POST|PUT|DELETE /unit-definitions` |
| No categories, units, or conversion routes | No users/sessions routes |

Sign in on **7180** (Scalar), then call unit-definitions on **7100** with `Authorization: Bearer …`.

SQLite files live under **`Data/`** (outside `src/`): `Data/UserManagement/auth.db`, `Data/UnitsMaster/catalog.db`. Schema is applied via **EF Core migrations** before you run the apps—not on first HTTP request.

### API documentation (Development)

Scalar is registered only when `ASPNETCORE_ENVIRONMENT=Development` (see each API `Program.cs`).

| Service | Scalar UI (HTTPS) | OpenAPI JSON |
|---------|-------------------|----------------|
| Units & conversion | https://localhost:7100/scalar/v1 | https://localhost:7100/openapi/v1.json |
| User management | https://localhost:7180/scalar/v1 | https://localhost:7180/openapi/v1.json |

**F5** uses the **`https`** launch profile (`launchBrowser` → Scalar). Plain `dotnet run` does not open a browser — open the HTTPS Scalar URLs above.

### Try the API (no login)

**List categories**

```bash
curl -s https://localhost:7100/api/v1/catalog/categories
```

**List units in a category** (by name or id `1` = length, `2` = mass, `3` = temperature)

```bash
curl -s "https://localhost:7100/api/v1/catalog/units?categoryName=length&page=1&pageSize=20"
```

**Convert length (meters → kilometers)**

```bash
curl -s -X POST https://localhost:7100/api/v1/unit-conversions ^
  -H "Content-Type: application/json" ^
  -d "{\"value\":1000,\"fromUnit\":\"m\",\"toUnit\":\"km\",\"category\":1}"
```

`category` is optional if `fromUnit` is unique in the catalog.

In Visual Studio / Rider, open `src/UnitConverter.UnitsDefinitions.Api/UnitConverter.API.http` for ready-made requests.

## API summary

### Conversion API (`UnitConverter.UnitsDefinitions.Api` — https://localhost:7100)

Public (no JWT): catalog and conversion. Admin routes require JWT (login on port 5180).

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/v1/catalog/categories` | List categories (id + name) |
| `GET` | `/api/v1/catalog/units` | List approved units (`category` or `categoryName`, paging) |
| `POST` | `/api/v1/unit-conversions` | Convert a value between two units |
| `GET` | `/api/v1/unit-definitions` | List unit definitions (**Admin**, **Employee**, or **Partner**) |
| `POST` | `/api/v1/unit-definitions` | Create a unit definition (**Admin**, **Employee**, or **Partner**; non-Admin → pending approval) |
| `PUT` | `/api/v1/unit-definitions/{id}` | Update display name and factor (**Admin**, **Employee**, or **Partner**) |
| `PUT` | `/api/v1/unit-definitions/{id}/approve` | Approve pending unit (**Admin** only) |
| `PUT` | `/api/v1/unit-definitions/{id}/reject` | Reject pending unit (**Admin** only) |
| `PUT` | `/api/v1/unit-definitions/{id}/admin-correction` | Admin correction regardless of submitter (**Admin** only) |
| `DELETE` | `/api/v1/unit-definitions/{id}` | Delete a unit definition (**Admin** only) |

### User management API (`UnitConverter.UserManagement.Api` — https://localhost:7180 / http://localhost:5180)

**Auth only** — this service does not expose catalog, conversion, or unit master-data endpoints.

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/v1/users` | Register a user account |
| `POST` | `/api/v1/sessions` | Sign in (JWT + refresh token) |
| `POST` | `/api/v1/sessions/refresh` | Refresh access token |

### Request body (`POST /api/v1/unit-conversions`)

```json
{
  "value": 1000,
  "fromUnit": "m",
  "toUnit": "km",
  "category": 1
}
```

Category ids: `1` = length, `2` = mass, `3` = temperature.

### Success response (`200 OK`)

```json
{
  "value": 1,
  "symbol": "km"
}
```

`symbol` is the target unit from the request (`toUnit`); input units are not repeated in the response.

Errors use RFC 7807 `ProblemDetails` (`type`, `title`, `detail`, `status`).

## Supported units (MVP)

Units are seeded by `.\scripts\database\setup-local.ps1` (see `UnitCatalogSeedData` in `UnitConverter.UnitsDefinitions.DataAccess`). Symbols are **case-insensitive**.

Development users (password `Test@12345`): `admin@unitconverter.local`, `employee@unitconverter.local`, `partner@unitconverter.local`, `public@unitconverter.local`.

| Category | Units |
|----------|--------|
| **Length** | `m`, `km`, `cm`, `ft`, `in` |
| **Weight** | `kg`, `g`, `lb`, `oz` |
| **Temperature** | `c`, `f`, `k` |

Temperature uses affine conversion (not simple factors). Length and weight convert via a base unit and linear factors.

## Solution structure

Two vertical slices under `src/`, plus shared libraries and `Data/` for SQLite files only:

```
Data/                                      ← databases only (gitignored .db)
  UserManagement/auth.db
  UnitsMaster/catalog.db

src/
  UnitConverter.Common/
  UnitConverter.Common.Contracts/

  UnitConverter.UserManagement/              ← domain + application (business)
  UnitConverter.UserManagement.Contracts/
  UnitConverter.UserManagement.DataAccess/   ← EF auth DB + migrations
  UnitConverter.UserManagement.Api/          ← identity, JWT, auth endpoints

  UnitConverter.UnitsDefinitions/            ← conversion engine + catalog rules
  UnitConverter.UnitsDefinitions.Contracts/
  UnitConverter.UnitsDefinitions.DataAccess/ ← EF catalog DB + migrations
  UnitConverter.UnitsDefinitions.Api/        ← conversion + catalog + admin APIs

tests/
  UnitConverter.Auth.Tests/                  → assembly: UserManagement.Api.Tests
  UnitConverter.Api.Tests/                   → assembly: UnitsDefinitions.Api.Tests
  UnitConverter.Domain.Tests/
  UnitConverter.Infrastructure.Tests/
  UnitConverter.Contracts.Tests/

tools/UnitConverter.DbSetup/                 ← migrate + seed (setup-local.ps1)
```

## Design decisions and trade-offs

| Decision | Rationale |
|----------|-----------|
| **Clean Architecture** | Domain has zero dependency on ASP.NET or EF; conversion logic is unit-tested without a web host. |
| **EF migrations + setup script** | Schema in source control; local DB created before APIs run. Seed via `DbSetup`, not on every app startup. |
| **`decimal` for values** | Avoids floating-point drift on financial-grade conversions; temperature round-trips are tested in domain tests. |
| **Single conversion endpoint** | Matches the challenge; catalog `GET` is optional for discoverability. |
| **.NET 10** | Latest SDK in use; pin to `net9.0` in `Directory.Build.props` if reviewers need only stable LTS. |

## Tests

```bash
dotnet test
```

- **Domain.Tests** — conversion engine (length, temperature).
- **Api.Tests** — HTTP integration via `WebApplicationFactory`.
- **Contracts / Infrastructure / Auth.Tests** — contracts and supporting services.

## Further documentation

| Doc | Purpose |
|-----|---------|
| [`docs/ASPIRE-DEV-GUIDE.md`](docs/ASPIRE-DEV-GUIDE.md) | **Local dev: DB setup, run both APIs, migrations** |
| [`Data/README.md`](Data/README.md) | SQLite paths and reset |
| [`docs/HLD.md`](docs/HLD.md) | Services, roles, endpoints |
| [`docs/DATABASE-SCHEMA.md`](docs/DATABASE-SCHEMA.md) | Tables and migration folders |
| [`docs/MVP.md`](docs/MVP.md) | Challenge scope vs future features |
| [`docs/CODE-STANDARDS.md`](docs/CODE-STANDARDS.md) | C# conventions |

## License

See repository settings (add a license file if publishing to GitHub).
