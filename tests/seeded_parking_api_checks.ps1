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
    Write-Output 'Seeded API returned five matching business results.'
}
finally {
    if ($null -ne $server -and -not $server.HasExited) { Stop-Process -Id $server.Id -Force; $server.WaitForExit() }
    if (Test-Path -LiteralPath $databasePath) { Remove-Item -LiteralPath $databasePath -Force }
    $backupPattern = [IO.Path]::GetFileNameWithoutExtension($databasePath) + '.backup-demo-*.db'
    Get-ChildItem -LiteralPath ([IO.Path]::GetTempPath()) -Filter $backupPattern -File | Remove-Item -Force
}
