# Local setup (all manual tests)

From repo root: <your-local-repo-path>/UnitConverter

```powershell
dotnet restore
dotnet build
.\scripts\database\setup-local.ps1
```

Two terminals (`dotnet run` does **not** open the browser — use `dotnet watch run` or open Scalar manually):

```powershell
dotnet run --project src/UnitConverter.UserManagement.Api      # http://localhost:5180
dotnet run --project src/UnitConverter.UnitsDefinitions.Api    # http://localhost:5173
```

| API | HTTP | Scalar (Development only) |
|-----|------|---------------------------|
| User Management | 5180 | http://localhost:5180/scalar/v1 |
| Units | 5173 | http://localhost:5173/scalar/v1 |

Auto-open from CLI: `dotnet watch run --project src/UnitConverter.UnitsDefinitions.Api --launch-profile http`

Seeded password: **`Test@12345`**. Categories: `1`=length, `2`=mass, `3`=temperature.
