$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$databasePath = Join-Path ([IO.Path]::GetTempPath()) ("parking-newman-" + [guid]::NewGuid().ToString('N') + '.db')
$fixturePath = Join-Path $root 'tests/prepare_sqlite_fixture.py'
$collectionPath = Join-Path $root 'postman/ParkingBusinessResult.postman_collection.json'
$apiPath = Join-Path $root 'Backend/ParkingBusinessResult.Api/bin/Debug/net10.0/ParkingBusinessResult.Api.dll'
$pythonPath = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$lotId = '00000000-0000-4000-8000-000000000002'
$server = $null

try {
    & $pythonPath $fixturePath $databasePath $lotId
    if ($LASTEXITCODE -ne 0) { throw 'Failed to initialize isolated SQLite fixture.' }
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $port = $listener.LocalEndpoint.Port
    $listener.Stop()
    $baseUrl = "http://127.0.0.1:$port"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ParkingBusinessResult__DatabasePath = $databasePath
    $env:Authentication__Issuer = 'postman-test-issuer'
    $env:Authentication__Audience = 'postman-test-audience'
    $env:Authentication__SigningKey = 'postman-test-only-random-key-at-least-32-bytes'
    $env:Authentication__BootstrapUsername = 'postman-test-admin'
    $env:Authentication__BootstrapPassword = 'postman-test-password-only'
    $server = Start-Process -FilePath 'dotnet' -ArgumentList @('"' + $apiPath + '"', '--urls', $baseUrl) -WorkingDirectory $root -PassThru -WindowStyle Hidden

    $ready = $false
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        if ($server.HasExited) { throw 'API process exited during startup.' }
        try { $null = Invoke-RestMethod -Uri "$baseUrl/dev/business-results"; $ready = $true; break } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) { throw 'API did not start.' }

    & npx --yes newman run $collectionPath --env-var "baseUrl=$baseUrl" --env-var 'adminUsername=postman-test-admin' --env-var 'adminPassword=postman-test-password-only' --env-var 'operatorUsername=postman-test-operator' --env-var 'operatorPassword=postman-test-operator-password' --bail failure
    if ($LASTEXITCODE -ne 0) { throw 'Postman collection failed.' }

    $loginBody = @{ username = 'postman-test-admin'; password = 'postman-test-password-only' } | ConvertTo-Json
    $token = (Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/login" -ContentType 'application/json' -Body $loginBody).data.accessToken
    $headers = @{ Authorization = "Bearer $token" }
    $deletedId = & $pythonPath (Join-Path $root 'tests/read_postman_deleted_record.py') $databasePath
    if ($LASTEXITCODE -ne 0) { throw 'The deleted record was not retained in SQLite.' }
    if ((Invoke-RestMethod -Uri "$baseUrl/dev/business-results/$deletedId" -Headers $headers).code -ne 7) { throw 'Deleted record remains visible.' }
    if ((Invoke-RestMethod -Method Delete -Uri "$baseUrl/dev/business-results/$deletedId" -Headers $headers).code -ne 7) { throw 'Repeated delete did not fail.' }
    $deletedUpdate = @{ revenue = 10; completedVehicleCount = 1; note = 'deleted' } | ConvertTo-Json
    if ((Invoke-RestMethod -Method Put -Uri "$baseUrl/dev/business-results/$deletedId" -Headers $headers -ContentType 'application/json' -Body $deletedUpdate).code -ne 7) { throw 'Deleted record was updated.' }
    $replacementBody = @{ parkingLotId = $lotId; reportingPeriod = '2026-10'; revenue = 900; completedVehicleCount = 3; note = 'replacement' } | ConvertTo-Json
    if ((Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/business-results" -Headers $headers -ContentType 'application/json' -Body $replacementBody).code -ne 0) { throw 'Could not recreate a result after soft deletion.' }
    $originalBody = @{ parkingLotId = $lotId; reportingPeriod = '2026-11'; revenue = 1200; completedVehicleCount = 4; note = 'baseline' } | ConvertTo-Json
    $created = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/business-results" -Headers $headers -ContentType 'application/json' -Body $originalBody
    if ($created.code -ne 0) { throw 'Failed to create isolated record for body validation.' }
    $id = $created.data.id
    $documentedBody = @{ totalCheckIn = 4; totalCheckOut = 4; averageOccupancyRate = 50; peakOccupancyRate = 60; totalRevenue = 1200; cashRevenue = 500; electronicRevenue = 700; operatingExpenses = 0; notes = 'from specification' } | ConvertTo-Json
    $update = Invoke-RestMethod -Method Put -Uri "$baseUrl/dev/business-results/$id" -Headers $headers -ContentType 'application/json' -Body $documentedBody
    $unchanged = Invoke-RestMethod -Uri "$baseUrl/dev/business-results/$id" -Headers $headers
    if ($update.code -ne 7 -or $unchanged.data.revenue -ne 1200 -or $unchanged.data.completedVehicleCount -ne 4) {
        throw "An unsupported documented body was not safely rejected: code=$($update.code), revenue=$($unchanged.data.revenue), count=$($unchanged.data.completedVehicleCount)."
    }
    $missingBody = @{ revenue = 0; note = 'missing vehicle count' } | ConvertTo-Json
    if ((Invoke-RestMethod -Method Put -Uri "$baseUrl/dev/business-results/$id" -Headers $headers -ContentType 'application/json' -Body $missingBody).code -ne 7) { throw 'Missing required field was accepted.' }
    $zeroBody = @{ revenue = 0; completedVehicleCount = 0; note = 'explicit zero values' } | ConvertTo-Json
    if ((Invoke-RestMethod -Method Put -Uri "$baseUrl/dev/business-results/$id" -Headers $headers -ContentType 'application/json' -Body $zeroBody).code -ne 0) { throw 'Valid explicit zero values were rejected.' }
    Write-Output 'Unsupported and incomplete bodies rejected; explicit zeros accepted; soft-deleted data retained and hidden.'
}
finally {
    if ($null -ne $server -and -not $server.HasExited) { Stop-Process -Id $server.Id -Force; $server.WaitForExit() }
    if (Test-Path -LiteralPath $databasePath) { Remove-Item -LiteralPath $databasePath -Force }
}
