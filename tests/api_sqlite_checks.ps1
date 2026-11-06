$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$databasePath = Join-Path ([System.IO.Path]::GetTempPath()) ("parking-api-" + [guid]::NewGuid().ToString('N') + '.db')
$fixturePath = Join-Path $repositoryRoot 'tests/prepare_sqlite_fixture.py'
$apiPath = if ($env:PARKING_API_DLL) { $env:PARKING_API_DLL } else { Join-Path $repositoryRoot 'Backend/ParkingBusinessResult.Api/bin/Debug/net10.0/ParkingBusinessResult.Api.dll' }
. (Join-Path $PSScriptRoot 'TestRuntime.ps1')
$pythonPath = Resolve-ParkingPython
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousDatabasePath = $env:ParkingBusinessResult__DatabasePath
$previousIssuer = $env:Authentication__Issuer
$previousAudience = $env:Authentication__Audience
$previousSigningKey = $env:Authentication__SigningKey
$previousBootstrapUsername = $env:Authentication__BootstrapUsername
$previousBootstrapPassword = $env:Authentication__BootstrapPassword
$server = $null
$lotId = [guid]::NewGuid().ToString()

function New-TestToken([string] $role, [string] $lotId = '', [int] $minutes = 5) {
    $header = @{ alg = 'HS256'; typ = 'JWT' } | ConvertTo-Json -Compress
    $payload = @{ iss = $env:Authentication__Issuer; aud = $env:Authentication__Audience; sub = 'test-user'; role = $role; exp = [DateTimeOffset]::UtcNow.AddMinutes($minutes).ToUnixTimeSeconds() }
    if ($lotId) { $payload.parking_lot_id = $lotId }
    $encode = { param($bytes) [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') }
    $head = & $encode ([Text.Encoding]::UTF8.GetBytes($header))
    $body = & $encode ([Text.Encoding]::UTF8.GetBytes(($payload | ConvertTo-Json -Compress)))
    $unsigned = "$head.$body"
    $hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($env:Authentication__SigningKey))
    try { $signature = & $encode ($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($unsigned))) } finally { $hmac.Dispose() }
    return "$unsigned.$signature"
}

try {
    & $pythonPath $fixturePath $databasePath $lotId
    if ($LASTEXITCODE -ne 0) { throw 'Failed to initialize test database.' }

    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $port = $listener.LocalEndpoint.Port
    $listener.Stop()
    $baseUrl = "http://127.0.0.1:$port/gdci/parking/parkingLot/businessOperation"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ParkingBusinessResult__DatabasePath = $databasePath
    $env:Authentication__Issuer = 'test-issuer'
    $env:Authentication__Audience = 'test-audience'
    $env:Authentication__SigningKey = 'test-signing-key-only-for-local-integration-tests'
    $env:Authentication__BootstrapUsername = 'test-admin'
    $env:Authentication__BootstrapPassword = 'test-admin-password-only'
    $server = Start-Process -FilePath 'dotnet' -ArgumentList @('"' + $apiPath + '"', '--urls', "http://127.0.0.1:$port") -WorkingDirectory $repositoryRoot -PassThru -WindowStyle Hidden

    $ready = $false
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        if ($server.HasExited) { throw 'API process exited during startup.' }
        try { $unauthenticated = Invoke-RestMethod -Uri $baseUrl; $ready = $true; break } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) { throw 'API did not start.' }

    if ($unauthenticated.code -ne 7) { throw 'An unauthenticated request was accepted.' }
    $authBaseUrl = "http://127.0.0.1:$port/auth"
    $incorrectLogin = @{ username = 'test-admin'; password = 'incorrect-password' } | ConvertTo-Json
    if ((Invoke-RestMethod -Method Post -Uri "$authBaseUrl/login" -ContentType 'application/json' -Body $incorrectLogin).code -ne 7) { throw 'Incorrect credentials were accepted.' }
    $adminLogin = @{ username = 'test-admin'; password = 'test-admin-password-only' } | ConvertTo-Json
    $login = Invoke-RestMethod -Method Post -Uri "$authBaseUrl/login" -ContentType 'application/json' -Body $adminLogin
    if ($login.code -ne 0 -or -not $login.data.accessToken) { throw 'Administrator login failed.' }
    $expiredHeaders = @{ Authorization = "Bearer $(New-TestToken 'ROLE_ADMIN' '' -1)" }
    if ((Invoke-RestMethod -Uri $baseUrl -Headers $expiredHeaders).code -ne 7) { throw 'An expired token was accepted.' }
    $forgedHeaders = @{ Authorization = "Bearer $((New-TestToken 'ROLE_ADMIN') + 'x')" }
    if ((Invoke-RestMethod -Uri $baseUrl -Headers $forgedHeaders).code -ne 7) { throw 'A token with an invalid signature was accepted.' }
    $badRoleHeaders = @{ Authorization = "Bearer $(New-TestToken 'ROLE_VIEWER')" }
    if ((Invoke-RestMethod -Uri $baseUrl -Headers $badRoleHeaders).code -ne 7) { throw 'An invalid role was accepted.' }
    $headers = @{ Authorization = "Bearer $($login.data.accessToken)" }
    $account = @{ username = 'test-operator'; password = 'operator-password-test'; role = 'ROLE_PARKING_OPERATOR'; parkingLotId = $lotId } | ConvertTo-Json
    if ((Invoke-RestMethod -Method Post -Uri "$authBaseUrl/users" -Headers $headers -ContentType 'application/json' -Body $account).code -ne 0) { throw 'Operator creation failed.' }
    if ((Invoke-RestMethod -Method Post -Uri "$authBaseUrl/users" -Headers $headers -ContentType 'application/json' -Body $account).code -ne 7) { throw 'Duplicate account was accepted.' }
    $operatorLogin = @{ username = 'test-operator'; password = 'operator-password-test' } | ConvertTo-Json
    $operatorToken = Invoke-RestMethod -Method Post -Uri "$authBaseUrl/login" -ContentType 'application/json' -Body $operatorLogin
    if ($operatorToken.code -ne 0) { throw 'Operator login failed.' }
    $operatorHeaders = @{ Authorization = "Bearer $($operatorToken.data.accessToken)" }
    if ((Invoke-RestMethod -Method Post -Uri "$authBaseUrl/users" -Headers $operatorHeaders -ContentType 'application/json' -Body $account).code -ne 7) { throw 'Operator was allowed to create users.' }
    $request = @{ parkingLotId = $lotId; reportingPeriod = '2026-10'; revenue = 200; completedVehicleCount = 2; note = 'Test' } | ConvertTo-Json
    $unknownLotRequest = @{ parkingLotId = [guid]::NewGuid().ToString(); reportingPeriod = '2026-10'; revenue = 200; completedVehicleCount = 2; note = 'Test' } | ConvertTo-Json
    if ((Invoke-RestMethod -Method Post -Uri $baseUrl -Headers $headers -ContentType 'application/json' -Body $unknownLotRequest).code -ne 7) { throw 'Unknown parking lot was accepted.' }
    $created = Invoke-RestMethod -Method Post -Uri $baseUrl -Headers $headers -ContentType 'application/json' -Body $request
    if ($created.code -ne 0 -or $created.data.revenue -ne 200) { throw 'Create failed.' }
    $id = $created.data.id
    $duplicate = Invoke-RestMethod -Method Post -Uri $baseUrl -Headers $headers -ContentType 'application/json' -Body $request
    if ($duplicate.code -ne 7) { throw 'Duplicate was not rejected.' }
    $invalidUpdate = @{ revenue = -1; completedVehicleCount = 3; note = 'Invalid' } | ConvertTo-Json
    if ((Invoke-RestMethod -Method Put -Uri "$baseUrl/$id" -Headers $headers -ContentType 'application/json' -Body $invalidUpdate).code -ne 7) { throw 'Invalid revenue was accepted.' }
    if ((Invoke-RestMethod -Uri "$baseUrl/$id" -Headers $headers).data.revenue -ne 200) { throw 'Invalid update changed saved data.' }
    $update = @{ revenue = 300; completedVehicleCount = 3; note = 'Updated' } | ConvertTo-Json
    $otherHeaders = @{ Authorization = "Bearer $(New-TestToken 'ROLE_PARKING_OPERATOR' ([guid]::NewGuid().ToString()))" }
    if ((Invoke-RestMethod -Uri "$baseUrl/$id" -Headers $otherHeaders).code -ne 7) { throw 'Access to an unassigned parking lot was allowed.' }
    if (@((Invoke-RestMethod -Uri $baseUrl -Headers $otherHeaders).data).Count -ne 0) { throw 'List included records from an unassigned parking lot.' }
    $adminList = Invoke-RestMethod -Uri $baseUrl -Headers $headers
    $assignedList = Invoke-RestMethod -Uri $baseUrl -Headers $operatorHeaders
    if (@($adminList.data).Count -ne 1 -or @($assignedList.data).Count -ne 1 -or $assignedList.data[0].id -ne $id) { throw 'Administrator or assigned parking-lot list access was changed.' }
    if ((Invoke-RestMethod -Method Post -Uri $baseUrl -Headers $otherHeaders -ContentType 'application/json' -Body $request).code -ne 7) { throw 'Creating a result for an unassigned parking lot was allowed.' }
    if ((Invoke-RestMethod -Method Put -Uri "$baseUrl/$id" -Headers $otherHeaders -ContentType 'application/json' -Body $update).code -ne 7) { throw 'Updating a result for an unassigned parking lot was allowed.' }
    if ((Invoke-RestMethod -Method Delete -Uri "$baseUrl/$id" -Headers $otherHeaders).code -ne 7) { throw 'Deleting a result for an unassigned parking lot was allowed.' }
    if ((Invoke-RestMethod -Uri "$baseUrl/$id" -Headers $operatorHeaders).code -ne 0) { throw 'Access to the assigned parking lot was denied.' }
    $updated = Invoke-RestMethod -Method Put -Uri "$baseUrl/$id" -Headers $headers -ContentType 'application/json' -Body $update
    if ($updated.code -ne 0 -or $updated.data.revenue -ne 300) { throw 'Update failed.' }
    $loaded = Invoke-RestMethod -Uri "$baseUrl/$id" -Headers $headers
    if ($loaded.code -ne 0 -or $loaded.data.completedVehicleCount -ne 3) { throw 'Read failed.' }
    $deleted = Invoke-RestMethod -Method Delete -Uri "$baseUrl/$id" -Headers $headers
    if ($deleted.code -ne 0) { throw 'Delete failed.' }
    & $pythonPath -c "import sqlite3,sys; row=sqlite3.connect(sys.argv[1]).execute('SELECT idNguoiTao,idNguoiCapNhat,nguoiTao,nguoiCapNhat,ngayTao,ngayCapNhat FROM SP_ketQuaKinhDoanhBaiDoXe WHERE id=?',(sys.argv[2],)).fetchone(); assert row and row[0] and row[0]==row[1] and row[2:4]==('test-admin','test-admin') and row[4] and row[5], 'Authenticated actor audit was not persisted'" $databasePath $id
    if ($LASTEXITCODE -ne 0) { throw 'CRUD audit check failed.' }
    $missing = Invoke-RestMethod -Uri "$baseUrl/$id" -Headers $headers
    if ($missing.code -ne 7) { throw 'Deleted record still exists.' }
    $null = Invoke-RestMethod -Method Post -Uri "$authBaseUrl/login" -ContentType 'application/json' -Body $incorrectLogin
    $null = Invoke-RestMethod -Method Post -Uri "$authBaseUrl/login" -ContentType 'application/json' -Body $incorrectLogin
    if ((Invoke-RestMethod -Method Post -Uri "$authBaseUrl/login" -ContentType 'application/json' -Body $adminLogin).code -ne 7) { throw 'Login rate limit was not enforced.' }
    Stop-Process -Id $server.Id -Force
    $server.WaitForExit()
    $env:Authentication__BootstrapPassword = 'different-bootstrap-password'
    $server = Start-Process -FilePath 'dotnet' -ArgumentList @('"' + $apiPath + '"', '--urls', "http://127.0.0.1:$port") -WorkingDirectory $repositoryRoot -PassThru -WindowStyle Hidden
    $restarted = $false
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        if ($server.HasExited) { throw 'API process exited during restart.' }
        try { $null = Invoke-RestMethod -Uri $baseUrl; $restarted = $true; break } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $restarted) { throw 'API did not restart.' }
    if ((Invoke-RestMethod -Method Post -Uri "$authBaseUrl/login" -ContentType 'application/json' -Body $adminLogin).code -ne 0) { throw 'Existing account did not survive restart.' }
    Write-Output 'Login, JWT, role, parking-lot scope, rate limit and SQLite CRUD checks passed.'
}
finally {
    if ($null -ne $server -and -not $server.HasExited) {
        Stop-Process -Id $server.Id -Force
        $server.WaitForExit()
    }
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:ParkingBusinessResult__DatabasePath = $previousDatabasePath
    $env:Authentication__Issuer = $previousIssuer
    $env:Authentication__Audience = $previousAudience
    $env:Authentication__SigningKey = $previousSigningKey
    $env:Authentication__BootstrapUsername = $previousBootstrapUsername
    $env:Authentication__BootstrapPassword = $previousBootstrapPassword
    if (Test-Path -LiteralPath $databasePath) { Remove-Item -LiteralPath $databasePath -Force }
}
