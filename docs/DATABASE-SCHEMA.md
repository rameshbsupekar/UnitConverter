# Database schema — normalization, indexes, constraints

SQLite files live under `Data/` (see [`Data/README.md`](../Data/README.md)). Schema is applied via **EF Core migrations** (`scripts/database/setup-local.ps1`).

## User management (`AuthDbContext`)

Normalized model (no repeating role names on users; assignments in junction table):

| Table | Primary key | Foreign keys |
|-------|-------------|--------------|
| `Users` | `Id` (bigint, not identity) | — |
| `Roles` | `Id` (int, not identity) | — |
| `UserRoles` | (`UserId`, `RoleId`) | `UserId` → `Users.Id` (CASCADE), `RoleId` → `Roles.Id` (RESTRICT) |
| `RefreshTokens` | `Id` (bigint, identity) | `UserId` → `Users.Id` (CASCADE) |
| `TokenBlacklists` | `Id` (bigint, identity) | — |

### Unique constraints

| Column(s) | Index name | Used by |
|-----------|------------|---------|
| `Users.Email` | `IX_Users_Email_Unique` | Login, register, `EmailExistsAsync` |
| `Roles.Name` | `IX_Roles_Name_Unique` | Seed, `GetByNameAsync` |
| `RefreshTokens.JwtId` | `IX_RefreshTokens_JwtId_Unique` | Token tracking |
| `RefreshTokens.TokenValue` | `IX_RefreshTokens_TokenValue_Unique` | `GetByTokenValueAsync` |
| `TokenBlacklists.JwtIdHash` | `IX_TokenBlacklists_JwtIdHash_Unique` | JWT revocation lookup |
| `UserRoles` (`UserId`, `RoleId`) | PK | One assignment per pair |

Emails are stored **lowercased** (`Email` value object) so the unique index is case-insensitive in practice.

### Non-unique indexes (WHERE / JOIN columns)

| Table | Index | Query pattern |
|-------|-------|----------------|
| `Users` | `IX_Users_IsActive` | Active check after email lookup |
| `UserRoles` | `IX_UserRoles_UserId` | Load roles for user |
| `UserRoles` | `IX_UserRoles_RoleId` | Role-side joins |
| `RefreshTokens` | `IX_RefreshTokens_UserId` | Tokens per user |
| `RefreshTokens` | `IX_RefreshTokens_UserId_IsRevoked` | Revoked filter per user |
| `RefreshTokens` | `IX_RefreshTokens_IsRevoked` | Revocation sweeps |
| `RefreshTokens` | `IX_RefreshTokens_ExpiresAt` | Expiry cleanup |
| `TokenBlacklists` | `IX_TokenBlacklists_TokenExpiresAt` | Purge expired blacklist rows |
| `TokenBlacklists` | `IX_TokenBlacklists_BlacklistedAt` | Time-ordered revocation audit |

## Unit master data (`ConverterDbContext`)

> **Proposed normalized schema (approval pending):** [`docs/schemas/UNIT-DEFINITIONS-SCHEMA.md`](schemas/UNIT-DEFINITIONS-SCHEMA.md) — `UnitCategories` + `UnitDefinitions` with FK. Do not implement until approved.

### Current (implemented)

Single table (MVP) — each unit is one row; categories are enum, not a separate lookup table:

| Table | Primary key | Foreign keys |
|-------|-------------|--------------|
| `UnitDefinitions` | `Id` (int, identity) | — |

### Unique constraints

| Column(s) | Index name | Used by |
|-----------|------------|---------|
| (`Category`, `Symbol`) | `IX_UnitDefinitions_Category_Symbol_Unique` | `FindBySymbolAsync`, `ExistsBySymbolAsync` |

Symbols are stored **lowercase** (domain + repository on insert).

### Non-unique indexes

| Table | Index | Query pattern |
|-------|-------|----------------|
| `UnitDefinitions` | `IX_UnitDefinitions_Category` | `GetByCategoryAsync`, admin list filter |

### Check constraints

| Constraint | Rule |
|------------|------|
| `CK_UnitDefinitions_FactorToBase_Positive` | `FactorToBase > 0` |

Temperature units may use factors that are not meaningful for conversion (affine formulas in domain); the check still requires a positive stored factor for consistency.

### Application-layer rules

- Duplicate symbol per category: blocked by unique index and `ExistsBySymbolAsync`
- Positive factor for length/weight: `UnitAdminService` before save

## Migrations

| Context | Folder |
|---------|--------|
| Auth | `src/UnitConverter.UserManagement.DataAccess/Migrations/` |
| Units | `src/UnitConverter.UnitsDefinitions.DataAccess/Migrations/` |

After model changes:

```powershell
.\scripts\database\add-migration.ps1 -Context auth -Name YourChange
.\scripts\database\add-migration.ps1 -Context units -Name YourChange
.\scripts\database\setup-local.ps1
```

## Cloud migration

Keep the same EF model; change `ConnectionStrings` only. PKs, FKs, unique indexes, and check constraints apply on SQL Server/PostgreSQL the same way.
