$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$databasePath = Join-Path ([IO.Path]::GetTempPath()) ("parking-seeded-api-" + [guid]::NewGuid().ToString('N') + '.db')
$pythonPath = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$apiPath = Join-Path $root 'Backend/ParkingBusinessResult.Api/bin/Debug/net10.0/ParkingBusinessResult.Api.dll'
$server = $null

try {
    & $pythonPath (Join-Path $root 'tests/prepare_sqlite_fixture.py') $databasePath ([guid]::NewGuid().ToString())
    if ($LASTEXITCODE -ne 0) { throw 'Failed to initialize test database.' }
    & $pythonPath (Join-Path $root 'database/sqlite/seed_development_parking_data.py') --database $databasePath --as-of '2026-11-05T12:00:00+07:00'
    if ($LASTEXITCODE -ne 0) { throw 'Failed to seed test database.' }

    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $port = $listener.LocalEndpoint.Port
    $listener.Stop()
    $baseUrl = "http://127.0.0.1:$port"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ParkingBusinessResult__DatabasePath = $databasePath
    $env:Authentication__Issuer = 'seed-test-issuer'
    $env:Authentication__Audience = 'seed-test-audience'
    $env:Authentication__SigningKey = 'seed-test-key-only-for-local-integration-tests'
    $env:Authentication__BootstrapUsername = 'seed-test-admin'
    $env:Authentication__BootstrapPassword = 'seed-test-password-only'
    $server = Start-Process -FilePath 'dotnet' -ArgumentList @('"' + $apiPath + '"', '--urls', $baseUrl) -WorkingDirectory $root -PassThru -WindowStyle Hidden

    $ready = $false
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        if ($server.HasExited) { throw 'API process exited during startup.' }
        try { $null = Invoke-RestMethod -Uri "$baseUrl/dev/business-results"; $ready = $true; break } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) { throw 'API did not start.' }
    $body = @{ username = 'seed-test-admin'; password = 'seed-test-password-only' } | ConvertTo-Json
    $login = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/login" -ContentType 'application/json' -Body $body
    if ($login.code -ne 0 -or -not $login.data.accessToken) { throw 'Login failed.' }
    $headers = @{ Authorization = "Bearer $($login.data.accessToken)" }
    $response = Invoke-RestMethod -Uri "$baseUrl/dev/business-results" -Headers $headers
    if ($response.code -ne 0 -or @($response.data).Count -ne 5) { throw 'Expected five business results.' }
    foreach ($result in $response.data) {
        if ($result.completedVehicleCount -ne 30 -or $result.revenue -ne 555000 -or $result.reportingPeriod -ne '2026-11') {
            throw 'Unexpected business totals in API response.'
        }
    }
    $report = Invoke-RestMethod -Uri "$baseUrl/dev/business-reports?reportingPeriod=2026-11" -Headers $headers
    if ($report.code -ne 0 -or @($report.data).Count -ne 5) { throw 'Expected five report records.' }
    $summary = Invoke-RestMethod -Uri "$baseUrl/dev/business-reports/summary?reportingPeriod=2026-11" -Headers $headers
    if ($summary.data.totalRevenue -ne 2775000 -or $summary.data.totalVehicleCount -ne 150 -or $summary.data.averageRevenuePerVehicle -ne 18500) { throw 'Unexpected report summary.' }
    $missingPeriod = Invoke-RestMethod -Uri "$baseUrl/dev/business-reports/summary" -Headers $headers
    if ($missingPeriod.code -ne 7) { throw 'A report period must be required.' }
    $emptyPeriod = Invoke-RestMethod -Uri "$baseUrl/dev/business-reports/summary?reportingPeriod=other" -Headers $headers
    if ($emptyPeriod.data.totalRevenue -ne 0 -or $emptyPeriod.data.totalVehicleCount -ne 0) { throw 'Empty periods should have zero totals.' }
    $first = $report.data[0]
    $second = $report.data[1]
    $scoped = Invoke-RestMethod -Uri "$baseUrl/dev/business-reports/summary?reportingPeriod=2026-11&parkingLotId=$($first.parkingLotId)" -Headers $headers
    if ($scoped.data.totalRevenue -ne 555000 -or $scoped.data.totalVehicleCount -ne 30) { throw 'Parking lot filter failed.' }
    $account = @{ username = 'report-operator'; password = 'report-password-only'; role = 'ROLE_PARKING_OPERATOR'; parkingLotId = $first.parkingLotId } | ConvertTo-Json
    $created = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/users" -Headers $headers -ContentType 'application/json' -Body $account
    if ($created.code -ne 0) { throw 'Could not create scoped account.' }
    $operatorBody = @{ username = 'report-operator'; password = 'report-password-only' } | ConvertTo-Json
    $operator = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/login" -ContentType 'application/json' -Body $operatorBody
    $operatorHeaders = @{ Authorization = "Bearer $($operator.data.accessToken)" }
    $operatorReport = Invoke-RestMethod -Uri "$baseUrl/dev/business-reports?reportingPeriod=2026-11" -Headers $operatorHeaders
    if (@($operatorReport.data).Count -ne 1 -or $operatorReport.data[0].parkingLotId -ne $first.parkingLotId) { throw 'Operator report scope failed.' }
    $forbiddenLot = Invoke-RestMethod -Uri "$baseUrl/dev/business-reports/summary?reportingPeriod=2026-11&parkingLotId=$($second.parkingLotId)" -Headers $operatorHeaders
    if ($forbiddenLot.data.totalRevenue -ne 0) { throw 'Operator accessed another parking lot.' }
    $deleted = Invoke-RestMethod -Method Delete -Uri "$baseUrl/dev/business-results/$($first.id)" -Headers $headers
    if ($deleted.code -ne 0) { throw 'Failed to soft-delete test record.' }
    $afterDelete = Invoke-RestMethod -Uri "$baseUrl/dev/business-reports/summary?reportingPeriod=2026-11" -Headers $headers
    if ($afterDelete.data.totalRevenue -ne 2220000 -or $afterDelete.data.totalVehicleCount -ne 120) { throw 'Deleted record was included in report.' }
    Write-Output 'Seeded API returned five matching business results.'
    Write-Output 'Report list, summary, period and lot filters, access scope, and soft delete checks passed.'
}
finally {
    if ($null -ne $server -and -not $server.HasExited) { Stop-Process -Id $server.Id -Force; $server.WaitForExit() }
    if (Test-Path -LiteralPath $databasePath) { Remove-Item -LiteralPath $databasePath -Force }
    $backupPattern = [IO.Path]::GetFileNameWithoutExtension($databasePath) + '.backup-demo-*.db'
    Get-ChildItem -LiteralPath ([IO.Path]::GetTempPath()) -Filter $backupPattern -File | Remove-Item -Force
}
