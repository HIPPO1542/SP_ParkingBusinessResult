$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$databasePath = Join-Path ([System.IO.Path]::GetTempPath()) ("parking-api-" + [guid]::NewGuid().ToString('N') + '.db')
$schemaPath = Join-Path $repositoryRoot 'database/sqlite/001_create_business_result.sql'
$apiPath = Join-Path $repositoryRoot 'src/ParkingBusinessResult.Api/bin/Debug/net10.0/ParkingBusinessResult.Api.dll'
$pythonPath = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousDatabasePath = $env:ParkingBusinessResult__DatabasePath
$server = $null

try {
    & $pythonPath -c 'import pathlib,sqlite3,sys; sqlite3.connect(sys.argv[1]).executescript(pathlib.Path(sys.argv[2]).read_bytes().decode())' $databasePath $schemaPath
    if ($LASTEXITCODE -ne 0) { throw 'Failed to initialize test database.' }

    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $port = $listener.LocalEndpoint.Port
    $listener.Stop()
    $baseUrl = "http://127.0.0.1:$port/dev/business-results"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ParkingBusinessResult__DatabasePath = $databasePath
    $server = Start-Process -FilePath 'dotnet' -ArgumentList @('"' + $apiPath + '"', '--urls', "http://127.0.0.1:$port") -WorkingDirectory $repositoryRoot -PassThru -WindowStyle Hidden

    $ready = $false
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        if ($server.HasExited) { throw 'API process exited during startup.' }
        try { $null = Invoke-RestMethod -Uri $baseUrl; $ready = $true; break } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) { throw 'API did not start.' }

    $request = @{ parkingLotId = [guid]::NewGuid().ToString(); reportingPeriod = '2026-10'; revenue = 200; completedVehicleCount = 2; note = 'Test' } | ConvertTo-Json
    $created = Invoke-RestMethod -Method Post -Uri $baseUrl -ContentType 'application/json' -Body $request
    if ($created.code -ne 0 -or $created.data.revenue -ne 200) { throw 'Create failed.' }
    $id = $created.data.id
    $duplicate = Invoke-RestMethod -Method Post -Uri $baseUrl -ContentType 'application/json' -Body $request
    if ($duplicate.code -ne 7) { throw 'Duplicate was not rejected.' }
    $update = @{ revenue = 300; completedVehicleCount = 3; note = 'Updated' } | ConvertTo-Json
    $updated = Invoke-RestMethod -Method Put -Uri "$baseUrl/$id" -ContentType 'application/json' -Body $update
    if ($updated.code -ne 0 -or $updated.data.revenue -ne 300) { throw 'Update failed.' }
    $loaded = Invoke-RestMethod -Uri "$baseUrl/$id"
    if ($loaded.code -ne 0 -or $loaded.data.completedVehicleCount -ne 3) { throw 'Read failed.' }
    $deleted = Invoke-RestMethod -Method Delete -Uri "$baseUrl/$id"
    if ($deleted.code -ne 0) { throw 'Delete failed.' }
    $missing = Invoke-RestMethod -Uri "$baseUrl/$id"
    if ($missing.code -ne 7) { throw 'Deleted record still exists.' }
    Write-Output 'SQLite API create, duplicate, update, read and delete checks passed.'
}
finally {
    if ($null -ne $server -and -not $server.HasExited) {
        Stop-Process -Id $server.Id -Force
        $server.WaitForExit()
    }
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:ParkingBusinessResult__DatabasePath = $previousDatabasePath
    if (Test-Path -LiteralPath $databasePath) { Remove-Item -LiteralPath $databasePath -Force }
}
