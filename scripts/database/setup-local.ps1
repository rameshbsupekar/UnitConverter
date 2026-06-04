# Creates/updates local SQLite databases under Data/ (schema via EF migrations + optional seed).
# Run once per machine (or after pulling new migrations) before starting the APIs.
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $repoRoot

Write-Host "UnitConverter: applying migrations and seeding local databases..." -ForegroundColor Cyan
dotnet run --project tools/UnitConverter.DbSetup/UnitConverter.DbSetup.csproj -- --seed
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done. Start services with:" -ForegroundColor Green
Write-Host "  dotnet run --project src/UnitConverter.UserManagement.Api"
Write-Host "  dotnet run --project src/UnitConverter.UnitsDefinitions.Api"
