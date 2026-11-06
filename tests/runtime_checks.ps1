$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'TestRuntime.ps1')
$pythonPath = Resolve-ParkingPython
$apiPath = if ($env:PARKING_API_DLL) { $env:PARKING_API_DLL } else { Join-Path $root 'Backend/ParkingBusinessResult.Api/bin/Debug/net10.0/ParkingBusinessResult.Api.dll' }
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ('parking-runtime-' + [guid]::NewGuid().ToString('N'))
$databasePath = Join-Path $testDirectory 'runtime.db'
$server = $null
New-Item -ItemType Directory -Path $testDirectory | Out-Null
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$baseUrl = "http://127.0.0.1:$port"
$businessPath = '/gdci/parking/parkingLot/businessOperation'

function Start-RuntimeServer([string] $environment) {
    $env:ASPNETCORE_ENVIRONMENT = $environment
    $process = Start-Process -FilePath 'dotnet' -ArgumentList @('"' + $apiPath + '"', '--urls', $baseUrl) -WorkingDirectory $root -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $testDirectory "$environment.log") -RedirectStandardError (Join-Path $testDirectory "$environment.err")
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ($process.HasExited) { throw "API exited during $environment startup. Logs: $testDirectory" }
        try {
            $live = Invoke-WebRequest -Uri "$baseUrl/health/live" -UseBasicParsing
            if ($live.StatusCode -eq 200) { return $process }
        } catch { Start-Sleep -Milliseconds 250 }
    }
    Stop-Process -Id $process.Id -Force
    throw "API did not start in $environment."
}

function Assert-BusinessError($response) {
    $body = $response.Content | ConvertFrom-Json
    if ($response.StatusCode -ne 200 -or $body.code -ne 7 -or $null -ne $body.data) { throw 'Error contract differs from HTTP 200 / code 7 / data null.' }
    if (@($body.PSObject.Properties.Name).Count -ne 3 -or -not ($body.PSObject.Properties.Name -contains 'data')) { throw 'Error envelope must have exactly code, message and data.' }
    if (-not $response.Headers['X-Request-Id']) { throw 'Missing request correlation header.' }
    if ($response.Content -match 'SqliteException|StackTrace|SELECT |System\.|runtime.db') { throw 'Internal error details leaked to the caller.' }
}

try {
    & $pythonPath (Join-Path $root 'tests/prepare_sqlite_fixture.py') $databasePath ([guid]::NewGuid().ToString())
    if ($LASTEXITCODE -ne 0) { throw 'Failed to create runtime database.' }
    & $pythonPath -c "import sqlite3,sys,pathlib; db=sqlite3.connect(sys.argv[1]); db.executescript(pathlib.Path(sys.argv[2]).read_text(encoding='utf-8')); db.close()" $databasePath (Join-Path $root 'database/sqlite/007_booking_table.sql')
    if ($LASTEXITCODE -ne 0) { throw 'Failed to prepare booking schema.' }
    $env:ParkingBusinessResult__DatabasePath = $databasePath
    $env:ParkingBusinessResult__ExportDirectory = Join-Path $testDirectory 'exports'
    $env:Authentication__Issuer = 'runtime-local'
    $env:Authentication__Mode = 'Local'
    $env:Authentication__Audience = 'runtime-api'
    $env:Authentication__SigningKey = 'runtime-check-signing-key-at-least-32-bytes'
    $env:Authentication__BootstrapUsername = 'runtime-admin'
    $env:Authentication__BootstrapPassword = 'runtime-password-only'
    $server = Start-RuntimeServer 'Development'
    Assert-BusinessError (Invoke-WebRequest -Uri "$baseUrl$businessPath" -UseBasicParsing)
    $login = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -ContentType 'application/json' -Body '{"username":"runtime-admin","password":"runtime-password-only"}'
    if ($login.code -ne 0) { throw 'Runtime login failed.' }
    $headers = @{ Authorization = "Bearer $($login.data.accessToken)" }
    Assert-BusinessError (Invoke-WebRequest -Uri "$baseUrl$businessPath" -Headers $headers -Method Post -ContentType 'application/json' -Body '{' -UseBasicParsing)
    Assert-BusinessError (Invoke-WebRequest -Uri "$baseUrl$businessPath/not-a-guid" -Headers $headers -UseBasicParsing)
    Assert-BusinessError (Invoke-WebRequest -Uri "$baseUrl$businessPath" -Headers $headers -Method Patch -UseBasicParsing)
    Assert-BusinessError (Invoke-WebRequest -Uri "$baseUrl/gdci/unknown-route" -Headers $headers -UseBasicParsing)
    $ready = Invoke-WebRequest -Uri "$baseUrl/health/ready" -UseBasicParsing
    if ($ready.StatusCode -ne 200) { throw 'Ready storage was not healthy.' }

    & $pythonPath -c "import sqlite3,sys; db=sqlite3.connect(sys.argv[1]); db.execute('ALTER TABLE SP_datTruocChoDoXe RENAME TO unavailable_booking'); db.commit(); db.close()" $databasePath
    if ($LASTEXITCODE -ne 0) { throw 'Failed to simulate unavailable storage.' }
    Assert-BusinessError (Invoke-WebRequest -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking" -Headers $headers -UseBasicParsing)
    try {
        $null = Invoke-WebRequest -Uri "$baseUrl/health/ready" -UseBasicParsing
        throw 'Unavailable storage was reported as ready.'
    } catch [System.Net.WebException] {
        if ([int]$_.Exception.Response.StatusCode -ne 503) { throw }
    }
    & $pythonPath -c "import sqlite3,sys; db=sqlite3.connect(sys.argv[1]); db.execute('ALTER TABLE unavailable_booking RENAME TO SP_datTruocChoDoXe'); db.commit(); db.close()" $databasePath
    if ($LASTEXITCODE -ne 0) { throw 'Failed to restore isolated storage.' }
    Stop-Process -Id $server.Id -Force
    $server.WaitForExit()

    $server = Start-RuntimeServer 'Production'
    $page = Invoke-WebRequest -Uri "$baseUrl/" -UseBasicParsing
    if ($page.StatusCode -ne 200 -or $page.Content -notmatch '<html') { throw 'Production frontend is unavailable.' }
    $login = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -ContentType 'application/json' -Body '{"username":"runtime-admin","password":"runtime-password-only"}'
    if ($login.code -ne 0) { throw 'Existing account could not log in to Production.' }
    $headers = @{ Authorization = "Bearer $($login.data.accessToken)" }
    $list = Invoke-RestMethod -Uri "$baseUrl$businessPath" -Headers $headers
    if ($list.code -ne 0) { throw 'Authenticated Production business route failed.' }
    Stop-Process -Id $server.Id -Force
    $server.WaitForExit()

    # Authority is optional. Missing-token requests never contact this placeholder IdP.
    $env:Authentication__Mode = 'Authority'
    $env:Authentication__Authority = 'https://identity.example.invalid'
    $server = Start-RuntimeServer 'Production'
    Assert-BusinessError (Invoke-WebRequest -Uri "$baseUrl$businessPath" -UseBasicParsing)
    foreach ($path in @('/', '/auth/login')) {
        try {
            $null = Invoke-WebRequest -Uri "$baseUrl$path" -UseBasicParsing
            throw "Local sign-in route is exposed in Authority mode: $path"
        } catch [System.Net.WebException] {
            if ([int]$_.Exception.Response.StatusCode -ne 404) { throw }
        }
    }
    $ready = Invoke-WebRequest -Uri "$baseUrl/health/ready" -UseBasicParsing
    if ($ready.StatusCode -ne 200) { throw 'Production database was not ready.' }
    Write-Output 'Runtime checks passed: errors, request ID, readiness, Production UI/login/business access and optional Authority isolation.'
}
finally {
    if ($null -ne $server -and -not $server.HasExited) { Stop-Process -Id $server.Id -Force; $server.WaitForExit() }
    $resolvedTarget = [IO.Path]::GetFullPath($testDirectory)
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $resolvedTarget.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase) -or (Split-Path $resolvedTarget -Leaf) -notlike 'parking-runtime-*') { throw 'Unsafe cleanup target.' }
    Remove-Item -LiteralPath $resolvedTarget -Recurse -Force
}
