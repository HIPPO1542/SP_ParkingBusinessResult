$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$testDir = Join-Path ([IO.Path]::GetTempPath()) ('parking-import-test-' + [guid]::NewGuid().ToString('N'))
$pythonPath = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$apiPath = Join-Path $root 'Backend/ParkingBusinessResult.Api/bin/Debug/net10.0/ParkingBusinessResult.Api.dll'
$server = $null
New-Item -ItemType Directory -Path $testDir | Out-Null
$databasePath = Join-Path $testDir 'test.db'

try {
    & $pythonPath (Join-Path $root 'tests/prepare_sqlite_fixture.py') $databasePath ([guid]::NewGuid().ToString())
    if ($LASTEXITCODE -ne 0) { throw 'Failed to initialize database.' }
    & $pythonPath (Join-Path $root 'database/sqlite/seed_development_parking_data.py') --database $databasePath --as-of '2026-11-05T12:00:00+07:00'
    if ($LASTEXITCODE -ne 0) { throw 'Failed to seed database.' }
    & $pythonPath (Join-Path $root 'database/sqlite/apply_parking_visit_migration.py') --database $databasePath
    if ($LASTEXITCODE -ne 0) { throw 'Failed to apply visits migration.' }
    & $pythonPath (Join-Path $root 'tests/parking_visit_import_fixture.py') make $testDir
    if ($LASTEXITCODE -ne 0) { throw 'Failed to prepare Excel files.' }
    $lotId = & $pythonPath -c "import sqlite3,sys; print(sqlite3.connect(sys.argv[1]).execute('select Id from DL_KTHT_baiDoXe where ma = ?',('DEMO-01',)).fetchone()[0])" $databasePath
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start(); $port = $listener.LocalEndpoint.Port; $listener.Stop()
    $baseUrl = "http://127.0.0.1:$port"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ParkingBusinessResult__DatabasePath = $databasePath
    $env:Authentication__Issuer = 'visit-test-issuer'
    $env:Authentication__Audience = 'visit-test-audience'
    $env:Authentication__SigningKey = 'visit-test-key-only-for-local-integration-tests'
    $env:Authentication__BootstrapUsername = 'visit-test-admin'
    $env:Authentication__BootstrapPassword = 'visit-test-password-only'
    $server = Start-Process -FilePath 'dotnet' -ArgumentList @('"' + $apiPath + '"', '--urls', $baseUrl) -WorkingDirectory $root -PassThru -WindowStyle Hidden
    $ready = $false
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        if ($server.HasExited) { throw 'API exited during startup.' }
        try { $null = Invoke-RestMethod -Uri "$baseUrl/dev/business-results"; $ready = $true; break } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) { throw 'API did not start.' }
    $loginBody = @{ username = 'visit-test-admin'; password = 'visit-test-password-only' } | ConvertTo-Json
    $login = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/login" -ContentType 'application/json' -Body $loginBody
    $token = $login.data.accessToken
    function Import-File($name, $bearer) {
        $filePath = Join-Path $testDir $name
        $raw = & curl.exe -sS -H "Authorization: Bearer $bearer" -F "file=@$filePath" "$baseUrl/dev/parking-visits/import"
        if ($LASTEXITCODE -ne 0) { throw 'Import request failed.' }
        try { return ($raw | ConvertFrom-Json) } catch { throw 'Import returned a non-JSON response.' }
    }
    $first = Import-File 'first.xlsx' $token
    if ($first.code -ne 0 -or $first.data.totalRows -ne 5 -or $first.data.successRows -ne 3 -or $first.data.errorRows -ne 2) { throw "Invalid initial import: $($first | ConvertTo-Json -Depth 5)" }
    if (@($first.data.importedIds).Count -ne 3) { throw 'Import did not return the saved IDs.' }
    $headers = @{ Authorization = "Bearer $token" }
    $validComparison = Invoke-RestMethod -Uri "$baseUrl/dev/parking-visits/$($first.data.importedIds[1])/tariff-comparison?dayType=WEEKDAY" -Headers $headers
    if (-not $validComparison.data.canCompare -or $validComparison.data.expectedAmount -ne 5000 -or $validComparison.data.difference -ne 0) { throw 'Tariff comparison was incorrect.' }
    $openComparison = Invoke-RestMethod -Uri "$baseUrl/dev/parking-visits/$($first.data.importedIds[0])/tariff-comparison?dayType=WEEKDAY" -Headers $headers
    if ($openComparison.data.canCompare -or $null -ne $openComparison.data.expectedAmount) { throw 'Open visit should not be compared.' }
    $holidayComparison = Invoke-RestMethod -Uri "$baseUrl/dev/parking-visits/$($first.data.importedIds[1])/tariff-comparison?dayType=HOLIDAY" -Headers $headers
    if ($holidayComparison.data.canCompare) { throw 'Missing holiday tariff should not be guessed.' }
    $initialSummary = Invoke-RestMethod -Uri "$baseUrl/dev/parking-visits/summary?reportingPeriod=2026-11" -Headers $headers
    if ($initialSummary.data.completedVehicleCount -ne 1 -or $initialSummary.data.collectedRevenue -ne 5000 -or $initialSummary.data.missingAmountCount -ne 0) { throw 'Initial visit summary is incorrect.' }
    foreach ($name in @('update.xlsx', 'blank_update.xlsx')) {
        $result = Import-File $name $token
        if ($result.code -ne 0 -or $result.data.successRows -ne 1) { throw "Update failed: $name" }
        if ($result.data.importedIds[0] -ne $first.data.importedIds[0]) { throw 'Repeated import changed the visit ID.' }
    }
    $overnight = Invoke-RestMethod -Uri "$baseUrl/dev/parking-visits/$($first.data.importedIds[0])/tariff-comparison?dayType=WEEKDAY" -Headers $headers
    if ($overnight.data.canCompare -or $null -ne $overnight.data.expectedAmount) { throw 'Overnight charge should not be guessed.' }
    $bad = Import-File 'invalid.xlsx' $token
    if ($bad.code -ne 7) { throw 'Invalid file was not rejected.' }
    $updatedSummary = Invoke-RestMethod -Uri "$baseUrl/dev/parking-visits/summary?reportingPeriod=2026-11" -Headers $headers
    if ($updatedSummary.data.completedVehicleCount -ne 2 -or $updatedSummary.data.collectedRevenue -ne 35000 -or $updatedSummary.data.previousRevenue -ne 10000 -or $updatedSummary.data.growthRate -ne 250) { throw 'Updated visit summary or growth is incorrect.' }
    $daySummary = Invoke-RestMethod -Uri "$baseUrl/dev/parking-visits/summary?fromDate=2026-11-05&toDate=2026-11-05" -Headers $headers
    if ($daySummary.data.completedVehicleCount -ne 2 -or $daySummary.data.collectedRevenue -ne 35000) { throw 'Date range should include completed visits on the selected date.' }
    $invalidRange = Invoke-RestMethod -Uri "$baseUrl/dev/parking-visits/summary?fromDate=2026-11-06&toDate=2026-11-05" -Headers $headers
    if ($invalidRange.code -ne 7) { throw 'Invalid date range was accepted.' }
    $previousMonth = Invoke-RestMethod -Uri "$baseUrl/dev/parking-visits/summary?reportingPeriod=2026-10" -Headers $headers
    if ($previousMonth.data.completedVehicleCount -ne 1 -or $previousMonth.data.collectedRevenue -ne 10000 -or $null -ne $previousMonth.data.growthRate) { throw 'Wrong reporting month or unknown growth.' }
    $xlsxPath = Join-Path $testDir 'export.xlsx'
    $pdfPath = Join-Path $testDir 'export.pdf'
    $exportBody = @{ fileFormat = 'EXCEL'; reportingPeriod = '2026-11'; reportTitle = '=safe test' } | ConvertTo-Json
    Invoke-WebRequest -Method Post -Uri "$baseUrl/dev/parking-visits/export" -Headers $headers -ContentType 'application/json' -Body $exportBody -OutFile $xlsxPath
    $pdfBody = @{ fileFormat = 'PDF'; reportingPeriod = '2026-11'; reportTitle = 'Báo cáo lượt xe' } | ConvertTo-Json
    Invoke-WebRequest -Method Post -Uri "$baseUrl/dev/parking-visits/export" -Headers $headers -ContentType 'application/json' -Body $pdfBody -OutFile $pdfPath
    & $pythonPath -X utf8 (Join-Path $root 'tests/check_visit_export.py') $xlsxPath $pdfPath
    if ($LASTEXITCODE -ne 0) { throw 'Visit export content invalid.' }
    $account = @{ username = 'visit-operator'; password = 'visit-password-only'; role = 'ROLE_PARKING_OPERATOR'; parkingLotId = $lotId } | ConvertTo-Json
    $created = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/users" -Headers $headers -ContentType 'application/json' -Body $account
    if ($created.code -ne 0) { throw 'Could not create operator.' }
    $operatorBody = @{ username = 'visit-operator'; password = 'visit-password-only' } | ConvertTo-Json
    $operator = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/login" -ContentType 'application/json' -Body $operatorBody
    $scoped = Import-File 'scoped.xlsx' $operator.data.accessToken
    if ($scoped.code -ne 0 -or $scoped.data.successRows -ne 2 -or $scoped.data.errorRows -ne 1) { throw 'Import scope failed.' }
    $operatorHeaders = @{ Authorization = "Bearer $($operator.data.accessToken)" }
    $forbiddenComparison = Invoke-RestMethod -Uri "$baseUrl/dev/parking-visits/$($first.data.importedIds[1])/tariff-comparison?dayType=WEEKDAY" -Headers $operatorHeaders
    if ($forbiddenComparison.data.canCompare -or $null -ne $forbiddenComparison.data.collectedAmount) { throw 'Tariff comparison exposed another parking lot.' }
    $operatorSummary = Invoke-RestMethod -Uri "$baseUrl/dev/parking-visits/summary?reportingPeriod=2026-11" -Headers $operatorHeaders
    if ($operatorSummary.data.completedVehicleCount -ne 2 -or $operatorSummary.data.collectedRevenue -ne 30000 -or $operatorSummary.data.missingAmountCount -ne 1 -or $null -ne $operatorSummary.data.averageDailyRevenue) { throw 'Visit summary access scope or missing amount count failed.' }
    $incompleteExport = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/parking-visits/export" -Headers $headers -ContentType 'application/json' -Body $exportBody
    if ($incompleteExport.code -ne 7) { throw 'Export should reject completed visits without amounts.' }
    $operatorExport = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/parking-visits/export" -Headers $operatorHeaders -ContentType 'application/json' -Body $exportBody
    if ($operatorExport.code -ne 7) { throw 'Operator should not be permitted to export.' }
    & $pythonPath -X utf8 (Join-Path $root 'tests/parking_visit_import_fixture.py') check $databasePath
    if ($LASTEXITCODE -ne 0) { throw 'SQLite visit data incorrect.' }
    $fractional = Import-File 'fractional.xlsx' $token
    $fractionalComparison = Invoke-RestMethod -Uri "$baseUrl/dev/parking-visits/$($fractional.data.importedIds[0])/tariff-comparison?dayType=WEEKDAY" -Headers $headers
    if ($fractionalComparison.data.canCompare -or $null -ne $fractionalComparison.data.expectedAmount) { throw 'Partial-hour charge should not be guessed.' }
    Write-Output 'Excel import, updating, malformed data and access scope passed.'
}
finally {
    if ($null -ne $server -and -not $server.HasExited) { Stop-Process -Id $server.Id -Force; $server.WaitForExit() }
    if ((Split-Path -Parent $testDir) -ne [IO.Path]::GetTempPath().TrimEnd('\')) { throw 'Unexpected test directory.' }
    Get-ChildItem -LiteralPath $testDir -File | Remove-Item -Force
    Remove-Item -LiteralPath $testDir
}
