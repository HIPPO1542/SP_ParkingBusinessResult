$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$testDir = Join-Path ([IO.Path]::GetTempPath()) ('parking-import-test-' + [guid]::NewGuid().ToString('N'))
$pythonPath = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$apiPath = if ($env:PARKING_API_DLL) { $env:PARKING_API_DLL } else { Join-Path $root 'Backend/ParkingBusinessResult.Api/bin/Debug/net10.0/ParkingBusinessResult.Api.dll' }
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
    & $pythonPath (Join-Path $root 'database/sqlite/migrate_visits_to_booking.py') --database $databasePath
    if ($LASTEXITCODE -ne 0) { throw 'Failed to migrate visits to booking.' }
    & $pythonPath -X utf8 (Join-Path $root 'tests/parking_booking_import_fixture.py') addresses $databasePath
    if ($LASTEXITCODE -ne 0) { throw 'Failed to prepare parking lot addresses.' }
    & $pythonPath (Join-Path $root 'tests/parking_booking_import_fixture.py') make $testDir
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
        try { $null = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/businessOperation"; $ready = $true; break } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) { throw 'API did not start.' }
    $loginBody = @{ username = 'visit-test-admin'; password = 'visit-test-password-only' } | ConvertTo-Json
    $login = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/login" -ContentType 'application/json' -Body $loginBody
    $token = $login.data.accessToken
    function Import-File($name, $bearer) {
        $filePath = Join-Path $testDir $name
        $raw = & curl.exe -sS -H "Authorization: Bearer $bearer" -F "file=@$filePath" "$baseUrl/gdci/parking/parkingLot/businessOperation/import"
        if ($LASTEXITCODE -ne 0) { throw 'Import request failed.' }
        try { return ($raw | ConvertFrom-Json) } catch { throw 'Import returned a non-JSON response.' }
    }
    $first = Import-File 'first.xlsx' $token
    if ($first.code -ne 0 -or $first.data.totalRows -ne 5 -or $first.data.successRows -ne 3 -or $first.data.errorRows -ne 2) { throw "Invalid initial import: $($first | ConvertTo-Json -Depth 5)" }
    if ($first.data.duplicateRows -ne 0 -or @($first.data.importedRows).Count -ne 3 -or $first.data.importedRows[0].parkingLotCode -ne 'DEMO-01' -or $first.data.importedRows[0].booking.licensePlate -ne '59A-1') { throw 'Imported rows are incorrect.' }
    if ($first.data.errors[0].rowNumber -ne 5 -or $first.data.errors[0].parkingLotCode -ne 'NO-SUCH-LOT' -or -not $first.data.errors[0].errorMessage -or $first.data.errors[1].parkingLotCode -ne 'DEMO-01') { throw 'Import error details are incorrect.' }
    if (@($first.data.importedIds).Count -ne 3) { throw 'Import did not return the saved IDs.' }
    $headers = @{ Authorization = "Bearer $token" }
    $parked = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking?status=PARKED" -Headers $headers
    $completed = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking?status=COMPLETED&pageSize=1" -Headers $headers
    if ($parked.data.totalCount -ne 1 -or $parked.data.items[0].licensePlate -ne '59A-1' -or $completed.data.totalCount -ne 2 -or @($completed.data.items).Count -ne 1) { throw 'Visit list status or paging failed.' }
    if ($parked.data.items[0].parkingLotCode -ne 'DEMO-01' -or $parked.data.items[0].parkingLotName -ne 'Bai xe gia lap 01') { throw 'Visit parking lot details are missing.' }
    $filters = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/filters" -Headers $headers
    if (@($filters.data.parkingLots | Where-Object { $_.code -like 'DEMO-*' }).Count -ne 5 -or $filters.data.vehicleTypes -notcontains 'MOTORBIKE') { throw 'Available filters are incorrect.' }
    $carType = [Uri]::EscapeDataString($parked.data.items[0].vehicleType)
    $combined = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking?parkingLotId=$lotId&status=PARKED&vehicleType=$carType" -Headers $headers
    if ($combined.data.totalCount -ne 1 -or $combined.data.items[0].licensePlate -ne '59A-1') { throw 'Combined visit filters failed.' }
    $noCombination = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking?parkingLotId=$lotId&status=COMPLETED&vehicleType=$carType" -Headers $headers
    if ($noCombination.data.totalCount -ne 0) { throw 'Combined filters returned an unrelated visit.' }
    $entryDay = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking?day=4&month=11&year=2026&status=PARKED&parkingLotId=$lotId&vehicleType=$carType" -Headers $headers
    if ($entryDay.data.totalCount -ne 1 -or $entryDay.data.items[0].licensePlate -ne '59A-1') { throw 'Combined date and visit filters failed.' }
    $yearOnly = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking?year=2026" -Headers $headers
    if ($yearOnly.data.totalCount -ne 3) { throw 'Year filter failed.' }
    foreach ($dateQuery in @('day=31&month=2', 'day=29&month=2&year=2025', 'year=0', 'month=13', 'dateField=invalid')) {
        $badDate = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking?$dateQuery" -Headers $headers
        if ($badDate.code -ne 7) { throw "Invalid list date accepted: $dateQuery" }
    }
    $invalidList = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking?status=unknown" -Headers $headers
    if ($invalidList.code -ne 7) { throw 'Invalid visit status was accepted.' }
    $validComparison = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/$($first.data.importedIds[1])/tariff-comparison?dayType=WEEKDAY" -Headers $headers
    if (-not $validComparison.data.canCompare -or $validComparison.data.expectedAmount -ne 5000 -or $validComparison.data.difference -ne 0) { throw 'Tariff comparison was incorrect.' }
    $openComparison = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/$($first.data.importedIds[0])/tariff-comparison?dayType=WEEKDAY" -Headers $headers
    if ($openComparison.data.canCompare -or $null -ne $openComparison.data.expectedAmount) { throw 'Open visit should not be compared.' }
    $holidayComparison = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/$($first.data.importedIds[1])/tariff-comparison?dayType=HOLIDAY" -Headers $headers
    if ($holidayComparison.data.canCompare) { throw 'Missing holiday tariff should not be guessed.' }
    $initialSummary = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/summary?reportingPeriod=2026-11" -Headers $headers
    if ($initialSummary.data.completedVehicleCount -ne 1 -or $initialSummary.data.collectedRevenue -ne 5000 -or $initialSummary.data.missingAmountCount -ne 0) { throw 'Initial visit summary is incorrect.' }
    foreach ($name in @('update.xlsx', 'blank_update.xlsx')) {
        $result = Import-File $name $token
        if ($result.code -ne 0 -or $result.data.successRows -ne 0 -or $result.data.errorRows -ne 1 -or $result.data.duplicateRows -ne 1 -or @($result.data.importedIds).Count -ne 0 -or -not $result.data.errors[0].errorMessage) { throw "Duplicate was not rejected: $name" }
    }
    $afterDuplicate = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking?status=PARKED" -Headers $headers
    if ($afterDuplicate.data.totalCount -ne 1 -or $afterDuplicate.data.items[0].id -ne $first.data.importedIds[0] -or $null -ne $afterDuplicate.data.items[0].collectedAmount) { throw 'Duplicate import changed the existing visit.' }
    & $pythonPath -X utf8 (Join-Path $root 'tests/parking_booking_import_fixture.py') complete $databasePath
    if ($LASTEXITCODE -ne 0) { throw 'Could not prepare completed visit for report checks.' }
    $exitDay = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking?day=5&month=11&year=2026&dateField=exitedAt&parkingLotId=$lotId" -Headers $headers
    if ($exitDay.data.totalCount -ne 1 -or $exitDay.data.items[0].licensePlate -ne '59A-1') { throw 'Exit date filter failed.' }
    $overnight = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/$($first.data.importedIds[0])/tariff-comparison?dayType=WEEKDAY" -Headers $headers
    if ($overnight.data.canCompare -or $null -ne $overnight.data.expectedAmount) { throw 'Overnight charge should not be guessed.' }
    $bad = Import-File 'invalid.xlsx' $token
    if ($bad.code -ne 7) { throw 'Invalid file was not rejected.' }
    $updatedSummary = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/summary?reportingPeriod=2026-11" -Headers $headers
    if ($updatedSummary.data.completedVehicleCount -ne 2 -or $updatedSummary.data.collectedRevenue -ne 35000 -or $updatedSummary.data.previousRevenue -ne 10000 -or $updatedSummary.data.growthRate -ne 250) { throw 'Updated visit summary or growth is incorrect.' }
    $secondLotId = ($filters.data.parkingLots | Where-Object { $_.code -eq 'DEMO-02' }).id
    $twoLots = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/summary?fromDate=2026-11-01&toDate=2026-11-30&parkingLotIds=$lotId&parkingLotIds=$secondLotId" -Headers $headers
    if ($twoLots.data.completedVehicleCount -ne 2 -or $twoLots.data.collectedRevenue -ne 35000 -or $twoLots.data.growthRate -ne 250) { throw 'Multiple-lot date filtering failed.' }
    $oneLot = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/summary?fromDate=2026-11-01&toDate=2026-11-30&parkingLotIds=$lotId" -Headers $headers
    if ($oneLot.data.completedVehicleCount -ne 1 -or $oneLot.data.collectedRevenue -ne 30000) { throw 'Single-lot date filtering failed.' }
    $invalidLot = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/summary?reportingPeriod=2026-11&parkingLotIds=not-a-guid" -Headers $headers
    if ($invalidLot.code -ne 7) { throw 'Invalid multiple-lot filter accepted.' }
    if (@($updatedSummary.data.topRevenueParkingLots).Count -ne 2 -or $updatedSummary.data.topRevenueParkingLots[0].parkingLot.code -ne 'DEMO-01' -or $updatedSummary.data.topRevenueParkingLots[0].percentage -ne 85.71) { throw 'Imported visit ranking incorrect.' }
    if (@($updatedSummary.data.revenueByDistrict).Count -ne 2 -or $updatedSummary.data.revenueByDistrict[0].revenue -ne 30000 -or $updatedSummary.data.revenueByDistrict[0].parkingLotsCount -ne 1) { throw 'District revenue grouping failed.' }
    $districtSummary = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/summary?reportingPeriod=2026-11&district=Qu%E1%BA%ADn%201" -Headers $headers
    if ($districtSummary.data.completedVehicleCount -ne 1 -or $districtSummary.data.collectedRevenue -ne 30000) { throw 'Visit district filter failed.' }
    $daySummary = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/summary?fromDate=2026-11-05&toDate=2026-11-05" -Headers $headers
    if ($daySummary.data.completedVehicleCount -ne 2 -or $daySummary.data.collectedRevenue -ne 35000) { throw 'Date range should include completed visits on the selected date.' }
    $invalidRange = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/summary?fromDate=2026-11-06&toDate=2026-11-05" -Headers $headers
    if ($invalidRange.code -ne 7) { throw 'Invalid date range was accepted.' }
    $previousMonth = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/summary?reportingPeriod=2026-10" -Headers $headers
    if ($previousMonth.data.completedVehicleCount -ne 1 -or $previousMonth.data.collectedRevenue -ne 10000 -or $null -ne $previousMonth.data.growthRate) { throw 'Wrong reporting month or unknown growth.' }
    $xlsxPath = Join-Path $testDir 'export.xlsx'
    $pdfPath = Join-Path $testDir 'export.pdf'
    $exportBody = @{ fileFormat = 'EXCEL'; reportingPeriod = '2026-11'; reportTitle = '=safe test' } | ConvertTo-Json
    Invoke-WebRequest -Method Post -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/export" -Headers $headers -ContentType 'application/json' -Body $exportBody -OutFile $xlsxPath
    $pdfBody = @{ fileFormat = 'PDF'; reportingPeriod = '2026-11'; reportTitle = 'Báo cáo lượt xe' } | ConvertTo-Json
    Invoke-WebRequest -Method Post -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/export" -Headers $headers -ContentType 'application/json' -Body $pdfBody -OutFile $pdfPath
    & $pythonPath -X utf8 (Join-Path $root 'tests/check_visit_export.py') $xlsxPath $pdfPath
    if ($LASTEXITCODE -ne 0) { throw 'Visit export content invalid.' }
    $rangeBody = @{ fileFormat = 'EXCEL'; fromDate = '2026-11-05'; toDate = '2026-11-05'; reportTitle = '=safe test' } | ConvertTo-Json
    Invoke-WebRequest -Method Post -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/export" -Headers $headers -ContentType 'application/json' -Body $rangeBody -OutFile $xlsxPath
    & $pythonPath -X utf8 (Join-Path $root 'tests/check_visit_export.py') $xlsxPath $pdfPath
    if ($LASTEXITCODE -ne 0) { throw 'Date range export content invalid.' }
    $invalidRangeBody = @{ fileFormat = 'EXCEL'; fromDate = '2026-11-06'; toDate = '2026-11-05' } | ConvertTo-Json
    $invalidRangeExport = Invoke-RestMethod -Method Post -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/export" -Headers $headers -ContentType 'application/json' -Body $invalidRangeBody
    if ($invalidRangeExport.code -ne 7) { throw 'Invalid date range export was accepted.' }
    $districtBody = @{ fileFormat = 'EXCEL'; reportingPeriod = '2026-11'; district = [uri]::UnescapeDataString('Qu%E1%BA%ADn%201') } | ConvertTo-Json
    Invoke-WebRequest -Method Post -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/export" -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($districtBody)) -OutFile $xlsxPath
    & $pythonPath (Join-Path $root 'tests/check_visit_district_export.py') $xlsxPath
    if ($LASTEXITCODE -ne 0) { throw 'Visit district export incorrect.' }
    $lotBody = @{ fileFormat = 'EXCEL'; fromDate = '2026-11-01'; toDate = '2026-11-30'; parkingLotIds = @($lotId) } | ConvertTo-Json
    Invoke-WebRequest -Method Post -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/export" -Headers $headers -ContentType 'application/json' -Body $lotBody -OutFile $xlsxPath
    & $pythonPath (Join-Path $root 'tests/check_visit_district_export.py') $xlsxPath
    if ($LASTEXITCODE -ne 0) { throw 'Selected-lot export incorrect.' }
    $account = @{ username = 'visit-operator'; password = 'visit-password-only'; role = 'ROLE_PARKING_OPERATOR'; parkingLotId = $lotId } | ConvertTo-Json
    $created = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/users" -Headers $headers -ContentType 'application/json' -Body $account
    if ($created.code -ne 0) { throw 'Could not create operator.' }
    $operatorBody = @{ username = 'visit-operator'; password = 'visit-password-only' } | ConvertTo-Json
    $operator = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/login" -ContentType 'application/json' -Body $operatorBody
    $scoped = Import-File 'scoped.xlsx' $operator.data.accessToken
    if ($scoped.code -ne 0 -or $scoped.data.successRows -ne 2 -or $scoped.data.errorRows -ne 1) { throw 'Import scope failed.' }
    $operatorHeaders = @{ Authorization = "Bearer $($operator.data.accessToken)" }
    $operatorFilters = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/filters" -Headers $operatorHeaders
    if (@($operatorFilters.data.parkingLots).Count -ne 1 -or $operatorFilters.data.parkingLots[0].id -ne $lotId -or $operatorFilters.data.vehicleTypes -contains 'MOTORBIKE') { throw 'Filter options exposed another parking lot.' }
    $operatorList = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking" -Headers $operatorHeaders
    if ($operatorList.data.totalCount -ne 3 -or @($operatorList.data.items | Where-Object { $_.parkingLotId -ne $lotId }).Count -ne 0) { throw 'Visit list access scope failed.' }
    $forbiddenComparison = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/$($first.data.importedIds[1])/tariff-comparison?dayType=WEEKDAY" -Headers $operatorHeaders
    if ($forbiddenComparison.data.canCompare -or $null -ne $forbiddenComparison.data.collectedAmount) { throw 'Tariff comparison exposed another parking lot.' }
    $operatorSummary = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/summary?reportingPeriod=2026-11" -Headers $operatorHeaders
    if ($operatorSummary.data.completedVehicleCount -ne 2 -or $operatorSummary.data.collectedRevenue -ne 30000 -or $operatorSummary.data.missingAmountCount -ne 1 -or $null -ne $operatorSummary.data.averageDailyRevenue) { throw 'Visit summary access scope or missing amount count failed.' }
    if ($null -ne $operatorSummary.data.topRevenueParkingLots) { throw 'Incomplete revenue should not produce a ranking.' }
    if ($null -ne $operatorSummary.data.revenueByDistrict) { throw 'Incomplete revenue should not produce district grouping.' }
    $incompleteExport = Invoke-RestMethod -Method Post -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/export" -Headers $headers -ContentType 'application/json' -Body $exportBody
    if ($incompleteExport.code -ne 7) { throw 'Export should reject completed visits without amounts.' }
    $operatorExport = Invoke-RestMethod -Method Post -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/export" -Headers $operatorHeaders -ContentType 'application/json' -Body $exportBody
    if ($operatorExport.code -ne 7) { throw 'Operator should not be permitted to export.' }
    & $pythonPath -X utf8 (Join-Path $root 'tests/parking_booking_import_fixture.py') check $databasePath
    if ($LASTEXITCODE -ne 0) { throw 'SQLite visit data incorrect.' }
    $unknownDistrict = Import-File 'unknown_district.xlsx' $token
    if ($unknownDistrict.data.successRows -ne 1) { throw 'Unknown district fixture import failed.' }
    $unknownLot = & $pythonPath -c "import sqlite3,sys; print(sqlite3.connect(sys.argv[1]).execute('select Id from DL_KTHT_baiDoXe where ma = ?',('DEMO-03',)).fetchone()[0])" $databasePath
    $unknownSummary = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/summary?reportingPeriod=2026-11&parkingLotId=$unknownLot" -Headers $headers
    if (@($unknownSummary.data.revenueByDistrict).Count -ne 1 -or $null -ne $unknownSummary.data.revenueByDistrict[0].district -or $unknownSummary.data.revenueByDistrict[0].revenue -ne 2000) { throw 'Unknown district should remain unclassified.' }
    $fractional = Import-File 'fractional.xlsx' $token
    $fractionalComparison = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/$($fractional.data.importedIds[0])/tariff-comparison?dayType=WEEKDAY" -Headers $headers
    if ($fractionalComparison.data.canCompare -or $null -ne $fractionalComparison.data.expectedAmount) { throw 'Partial-hour charge should not be guessed.' }
    $unclassified = Import-File 'unclassified.xlsx' $token
    if ($unclassified.code -ne 0 -or $unclassified.data.successRows -ne 1) { throw 'Unclassified payment import failed.' }
    $paymentSummary = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBooking/summary?reportingPeriod=2026-11" -Headers $headers
    if ($paymentSummary.data.collectedRevenue -lt 9000) { throw 'Collected amount should be included regardless of legacy payment column.' }
    $csvResult = Import-File 'visit.csv' $token
    if ($csvResult.code -ne 0 -or $csvResult.data.successRows -ne 1) { throw 'CSV import failed.' }
    $sixColumnCsv = Import-File 'six_columns.csv' $token
    if ($sixColumnCsv.code -ne 0 -or $sixColumnCsv.data.successRows -ne 1) { throw 'Six-column CSV import failed.' }
    if (Test-Path -LiteralPath (Join-Path $testDir 'visit.xls')) {
        $xlsResult = Import-File 'visit.xls' $token
        if ($xlsResult.code -ne 0 -or $xlsResult.data.successRows -ne 1) { throw 'XLS import failed.' }
        Write-Output 'Legacy XLS import passed.'
    } else { Write-Output 'Legacy XLS fixture not available; skipped.' }
    $duplicateRows = Import-File 'duplicate_rows.xlsx' $token
    if ($duplicateRows.code -ne 0 -or $duplicateRows.data.successRows -ne 1 -or $duplicateRows.data.errorRows -ne 1 -or $duplicateRows.data.duplicateRows -ne 1 -or @($duplicateRows.data.importedIds).Count -ne 1 -or $duplicateRows.data.errors[0].rowNumber -ne 3) { throw 'Duplicates within the same file were not rejected correctly.' }
    Write-Output 'Excel import, duplicate rejection, malformed data and access scope passed.'
}
finally {
    if ($null -ne $server -and -not $server.HasExited) { Stop-Process -Id $server.Id -Force; $server.WaitForExit() }
    if ((Split-Path -Parent $testDir) -ne [IO.Path]::GetTempPath().TrimEnd('\')) { throw 'Unexpected test directory.' }
    Get-ChildItem -LiteralPath $testDir -File | Remove-Item -Force
    Remove-Item -LiteralPath $testDir
}
