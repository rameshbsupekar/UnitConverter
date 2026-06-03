# ADR-0007 — Persistence layer & repository pattern

- Status: Accepted
- Date: 2026-06-03

## Context

The domain and business logic must remain independent of the database technology. We want to
start with **SQLite** (local dev), support **SQL Server / PostgreSQL** in cloud, and keep the
door open to **Oracle** or others in the future.

## Decision

Use **Repository pattern** + **Entity Framework Core 9.0+**:

- Define **interfaces** in Application (`IUnitRepository`, `IUnitApprovalRepository`).
- Implement against a **DbContext** in Infrastructure (`ApplicationDbContext`).
- **One DbContext definition** (model + migrations); swap **connection strings** by environment.
- Use **EF Core migrations per database** (SQLite, SQL Server, PostgreSQL) stored in a unified
  `Migrations/` folder or organized by provider.
- All repository methods are **async/await** (no sync-over-async anti-pattern).
- **Soft deletes** for audit: `IsDeleted` / `DeletedAt` fields, filtered by queries (not DB-level).

## Rationale

- Repository pattern decouples domain from persistence; easier to test (mock/in-memory impl).
- EF Core is the .NET ORM standard; supports all target DBs natively.
- Async throughout prevents thread-pool starvation on high-concurrency workloads.
- Soft deletes preserve the full history for audit/compliance.

## Consequences

- DbContext must be registered in DI (`AddDbContextFactory` + `AddScoped<DbContext>`).
- Every schema change requires new migrations; consider migration versioning.
- Performance tuning per DB (indexes, query hints); use `.AsNoTracking()` for read-heavy paths.
- Secrets (connection strings) in configuration, never code.

## Alternatives considered

- **Micro-ORM (Dapper)** — more control, less magic. Chosen EF Core for simplicity and EF Core 9+ performance.
- **Multiple DbContext types per database** — rejected; one DbContext + connection string swap is cleaner.
- **No ORM (raw SQL)** — rejected; loses compile-time safety and portability.
