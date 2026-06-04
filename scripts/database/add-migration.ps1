# Adds a new EF Core migration. Usage:
#   .\scripts\database\add-migration.ps1 -Context units -Name AddFoo
#   .\scripts\database\add-migration.ps1 -Context auth -Name AddBar
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("units", "auth")]
    [string] $Context,

    [Parameter(Mandatory = $true)]
    [string] $Name
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location $repoRoot

dotnet tool restore | Out-Null

if ($Context -eq "units") {
    dotnet ef migrations add $Name `
        --project src/UnitConverter.UnitsDefinitions.DataAccess/UnitConverter.UnitsDefinitions.DataAccess.csproj `
        --context ConverterDbContext `
        --output-dir Migrations
}
else {
    dotnet ef migrations add $Name `
        --project src/UnitConverter.UserManagement.DataAccess/UnitConverter.UserManagement.DataAccess.csproj `
        --context AuthDbContext `
        --output-dir Migrations
}
