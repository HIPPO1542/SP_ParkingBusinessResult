param([ValidateRange(1, 65535)][int] $Port = 5069)

$ErrorActionPreference = 'Stop'
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:Authentication__Mode = 'Local'
$env:Authentication__UseUserSecrets = 'true'
$env:ParkingBusinessResult__DatabasePath = Join-Path $PSScriptRoot 'database/sqlite/parking_business_result.db'
$env:ParkingBusinessResult__ExportDirectory = Join-Path $PSScriptRoot 'Backend/ParkingBusinessResult.Api/App_Data/exports'

if (-not (Test-Path -LiteralPath $env:ParkingBusinessResult__DatabasePath)) {
    throw 'Database does not exist. Restore the approved database or apply migrations before starting.'
}

dotnet run --project (Join-Path $PSScriptRoot 'Backend/ParkingBusinessResult.Api') -c Release --no-launch-profile --urls "http://localhost:$Port"
exit $LASTEXITCODE
