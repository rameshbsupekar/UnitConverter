# MVP — Technical challenge scope

This document separates **what you need to submit now** from **later platform work**.

## In scope (challenge deliverable)

- [x] `POST /api/conversions` — convert between two units
- [x] Length, temperature, weight categories
- [x] Unit data seeded at startup (hardcoded catalog)
- [x] ASP.NET Core Web API, runnable locally
- [x] README with run instructions and design notes
- [x] Team-friendly layout (Domain, Application, Infrastructure, tests)
- [x] `dotnet build` / `dotnet test` green

**Run:** `dotnet run --project src/UnitConverter.UnitsDefinitions.Api`

## Out of scope for MVP (defer)

| Feature | Where it lives later |
|---------|----------------------|
| User registration / JWT | `UnitConverter.Auth` |
| Unit submit / approve workflow | `AdminController` + `UnitConverter.Admin.*` |
| Public web UI | `UnitConverter.UI.Web` |
| Desktop UI | `UnitConverter.UI.Desktop` |
| Admin UI | `UnitConverter.Admin.UI` + `UnitConverter.Admin.BusinessLayer` |
| Hundreds of units / admin CRUD | Catalog service + migrations |
| Aspire AppHost / OpenTelemetry | Host `Program.cs` or shared extensions |

## Two-service vision (post-MVP)

```
┌─────────────────────┐     ┌──────────────────────────────┐
│ Auth / User Mgmt    │     │ Units + Conversion API      │
│ Auth DB             │     │ Catalog DB (ConverterDb)    │
│ register, login     │     │ POST /conversions           │
└─────────────────────┘     │ GET /catalog, Admin/*       │
                            └──────────────────────────────┘
         ▲                              ▲
         │                              │
   UnitConverter.UI.*          UnitConverter.Admin.*
```

See [`STATUS.md`](STATUS.md) for ordered next steps after the challenge is submitted.
