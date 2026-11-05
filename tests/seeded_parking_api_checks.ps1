$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$databasePath = Join-Path ([IO.Path]::GetTempPath()) ("parking-seeded-api-" + [guid]::NewGuid().ToString('N') + '.db')
$pythonPath = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$apiPath = Join-Path $root 'Backend/ParkingBusinessResult.Api/bin/Debug/net10.0/ParkingBusinessResult.Api.dll'
$server = $null
$exportPath = Join-Path ([IO.Path]::GetTempPath()) ("parking-report-" + [guid]::NewGuid().ToString('N') + '.xlsx')
$pdfPath = Join-Path ([IO.Path]::GetTempPath()) ("parking-report-" + [guid]::NewGuid().ToString('N') + '.pdf')
$exportDirectory = Join-Path ([IO.Path]::GetTempPath()) ("parking-exports-" + [guid]::NewGuid().ToString('N'))

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
    $env:ParkingBusinessResult__ExportDirectory = $exportDirectory
    $env:Authentication__Issuer = 'seed-test-issuer'
    $env:Authentication__Audience = 'seed-test-audience'
    $env:Authentication__SigningKey = 'seed-test-key-only-for-local-integration-tests'
    $env:Authentication__BootstrapUsername = 'seed-test-admin'
    $env:Authentication__BootstrapPassword = 'seed-test-password-only'
    $server = Start-Process -FilePath 'dotnet' -ArgumentList @('"' + $apiPath + '"', '--urls', $baseUrl) -WorkingDirectory $root -PassThru -WindowStyle Hidden

    $ready = $false
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        if ($server.HasExited) { throw 'API process exited during startup.' }
        try { $null = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/businessOperation"; $ready = $true; break } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) { throw 'API did not start.' }
    $body = @{ username = 'seed-test-admin'; password = 'seed-test-password-only' } | ConvertTo-Json
    $login = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/login" -ContentType 'application/json' -Body $body
    if ($login.code -ne 0 -or -not $login.data.accessToken) { throw 'Login failed.' }
    $headers = @{ Authorization = "Bearer $($login.data.accessToken)" }
    function Save-BusinessExport($requestBody, $requestHeaders, $destination, $expectedCount) {
        $result = Invoke-RestMethod -Method Post -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport/export" -Headers $requestHeaders -ContentType 'application/json' -Body $requestBody
        if ($result.code -ne 0 -or $result.data.recordCount -ne $expectedCount -or -not $result.data.id -or -not $result.data.fileUrl -or -not $result.data.createdById -or -not $result.data.createdBy) { throw 'Invalid export response.' }
        foreach ($field in @('fileName', 'fileFormat', 'fileSize', 'createdAt', 'updatedAt', 'updatedBy', 'updatedById')) {
            if ($null -eq $result.data.PSObject.Properties[$field]) { throw "Missing export field: $field" }
        }
        $script:lastExportUrl = $result.data.fileUrl
        Invoke-WebRequest -Uri "$baseUrl$($result.data.fileUrl)" -Headers $requestHeaders -OutFile $destination
        if ((Get-Item -LiteralPath $destination).Length -ne $result.data.fileSize) { throw 'Export size does not match the downloaded file.' }
    }
    try {
        $null = Invoke-WebRequest -Uri "$baseUrl/dev/business-results" -Headers $headers -UseBasicParsing
        throw 'The old business-results route should not be mapped.'
    } catch [System.Net.WebException] {
        if ([int]$_.Exception.Response.StatusCode -ne 404) { throw }
    }
    $response = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/businessOperation" -Headers $headers
    if ($response.code -ne 0 -or @($response.data).Count -ne 5) { throw 'Expected five business results.' }
    foreach ($result in $response.data) {
        if ($result.completedVehicleCount -ne 30 -or $result.revenue -ne 555000 -or $result.reportingPeriod -ne '2026-11') {
            throw 'Unexpected business totals in API response.'
        }
    }
    $report = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport?reportingPeriod=2026-11" -Headers $headers
    if ($report.code -ne 0 -or @($report.data).Count -ne 5) { throw 'Expected five report records.' }
    foreach ($item in $report.data) {
        if (-not $item.id -or -not $item.parkingLot.id -or -not $item.parkingLot.code -or -not $item.parkingLot.name -or -not $item.parkingLot.address -or -not $item.operatorName) { throw 'Missing mapped report metadata.' }
        if ($item.revenue -ne 555000 -or $item.vehicleCount -ne 30 -or $item.averageRevenuePerVehicle -ne 18500) { throw 'Incorrect report values.' }
        foreach ($field in @('createdAt', 'createdBy', 'createdById', 'updatedAt', 'updatedBy', 'updatedById')) {
            if ($null -eq $item.PSObject.Properties[$field]) { throw "Missing audit field: $field" }
        }
    }
    $nameSearch = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport?reportingPeriod=2026-11&keyword=Bai%20xe%20gia%20lap%2001" -Headers $headers
    if ($nameSearch.code -ne 0 -or @($nameSearch.data).Count -ne 1) { throw 'Search by parking lot name failed.' }
    $addressSearch = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport?reportingPeriod=2026-11&keyword=dia%20chi%20gia%20lap" -Headers $headers
    if ($addressSearch.code -ne 0 -or @($addressSearch.data).Count -ne 5) { throw 'Search by parking lot address failed.' }
    $emptySearch = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport?reportingPeriod=2026-11&keyword=not-found" -Headers $headers
    if ($emptySearch.code -ne 0 -or @($emptySearch.data).Count -ne 0) { throw 'Search should return no matching results.' }
    $page1 = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport?reportingPeriod=2026-11&pageNumber=1&pageSize=2" -Headers $headers
    $page2 = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport?reportingPeriod=2026-11&pageNumber=2&pageSize=2" -Headers $headers
    $page3 = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport?reportingPeriod=2026-11&pageNumber=3&pageSize=2" -Headers $headers
    if (@($page1.data).Count -ne 2 -or @($page2.data).Count -ne 2 -or @($page3.data).Count -ne 1 -or $page1.data[0].id -eq $page2.data[0].id) { throw 'Report pagination failed.' }
    foreach ($query in @('pageNumber=0', 'pageSize=101', 'pageSize=invalid', 'fromDate=2026-11-01')) {
        $invalid = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport?reportingPeriod=2026-11&$query" -Headers $headers
        if ($invalid.code -ne 7) { throw "Invalid or unsupported query was not rejected: $query" }
    }
    $unsupportedSummary = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport/summary?reportingPeriod=2026-11&fromDate=2026-11-01" -Headers $headers
    if ($unsupportedSummary.code -ne 7) { throw 'Unsupported summary filter was not rejected.' }
    $districtReport = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport?reportingPeriod=2026-11&district=dia%20chi%20gia%20lap" -Headers $headers
    if (@($districtReport.data).Count -ne 5) { throw 'District filter failed.' }
    $districtEmpty = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport/summary?reportingPeriod=2026-11&district=not-found" -Headers $headers
    if ($districtEmpty.data.totalRevenue -ne 0) { throw 'District filter returned unrelated results.' }
    $summary = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport/summary?reportingPeriod=2026-11" -Headers $headers
    if ($summary.data.totalRevenue -ne 2775000 -or $summary.data.totalVehicleCount -ne 150 -or $summary.data.averageRevenuePerVehicle -ne 18500) { throw 'Unexpected report summary.' }
    if ($summary.data.averageDailyRevenue -ne 92500 -or $null -ne $summary.data.growthRate) { throw 'Unexpected daily average or growth rate.' }
    if (@($summary.data.revenueByDistrict).Count -ne 1 -or $summary.data.revenueByDistrict[0].district -ne $null -or $summary.data.revenueByDistrict[0].parkingLotsCount -ne 5 -or $summary.data.revenueByDistrict[0].revenue -ne 2775000) { throw 'Unexpected district summary.' }
    if ($summary.data.reportingPeriod -ne '2026-11' -or @($summary.data.topRevenueParkingLots).Count -ne 5) { throw 'Expected five top parking lots.' }
    foreach ($topLot in $summary.data.topRevenueParkingLots) {
        if ($topLot.revenue -ne 555000 -or $topLot.vehicleCount -ne 30 -or $topLot.percentage -ne 20 -or -not $topLot.parkingLot.name -or -not $topLot.parkingLot.code) { throw 'Unexpected top parking lot details.' }
    }
    $missingPeriod = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport/summary" -Headers $headers
    if ($missingPeriod.code -ne 7) { throw 'A report period must be required.' }
    $emptyPeriod = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport/summary?reportingPeriod=other" -Headers $headers
    if ($emptyPeriod.data.totalRevenue -ne 0 -or $emptyPeriod.data.totalVehicleCount -ne 0) { throw 'Empty periods should have zero totals.' }
    $first = $report.data[0]
    $second = $report.data[1]
    $exportBody = @{ fileFormat = 'EXCEL'; reportingPeriod = '2026-11'; reportTitle = '=test formula is text' } | ConvertTo-Json
    Save-BusinessExport $exportBody $headers $exportPath 5
    & $pythonPath (Join-Path $root 'tests/check_business_report_xlsx.py') $exportPath 5 2775000
    if ($LASTEXITCODE -ne 0) { throw 'Excel report content verification failed.' }
    $pdfBody = @{ fileFormat = 'PDF'; reportingPeriod = '2026-11'; reportTitle = 'Báo cáo kết quả kinh doanh' } | ConvertTo-Json
    Save-BusinessExport $pdfBody $headers $pdfPath 5
    & $pythonPath (Join-Path $root 'tests/check_business_report_pdf.py') $pdfPath 5 '2,775,000.00'
    if ($LASTEXITCODE -ne 0) { throw 'PDF report content verification failed.' }
    if ($env:PARKING_REPORT_PDF_CAPTURE) { Copy-Item -LiteralPath $pdfPath -Destination $env:PARKING_REPORT_PDF_CAPTURE }
    $unsupportedBody = @{ fileFormat = 'TXT'; reportingPeriod = '2026-11' } | ConvertTo-Json
    $unsupported = Invoke-RestMethod -Method Post -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport/export" -Headers $headers -ContentType 'application/json' -Body $unsupportedBody
    if ($unsupported.code -ne 7) { throw 'Unsupported format should be rejected.' }
    $unknownBody = @{ fileFormat = 'EXCEL'; reportingPeriod = '2026-11'; fromDate = '2026-11-01' } | ConvertTo-Json
    $unknown = Invoke-RestMethod -Method Post -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport/export" -Headers $headers -ContentType 'application/json' -Body $unknownBody
    if ($unknown.code -ne 7) { throw 'Unsupported filters should be rejected.' }
    $districtExportBody = @{ fileFormat = 'EXCEL'; reportingPeriod = '2026-11'; reportTitle = '=test formula is text'; district = 'dia chi gia lap' } | ConvertTo-Json
    Save-BusinessExport $districtExportBody $headers $exportPath 5
    & $pythonPath (Join-Path $root 'tests/check_business_report_xlsx.py') $exportPath 5 2775000
    if ($LASTEXITCODE -ne 0) { throw 'District Excel report verification failed.' }
    $scoped = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport/summary?reportingPeriod=2026-11&parkingLotId=$($first.parkingLot.id)" -Headers $headers
    if ($scoped.data.totalRevenue -ne 555000 -or $scoped.data.totalVehicleCount -ne 30) { throw 'Parking lot filter failed.' }
    if (@($scoped.data.topRevenueParkingLots).Count -ne 1 -or $scoped.data.topRevenueParkingLots[0].percentage -ne 100) { throw 'Scoped ranking failed.' }
    $account = @{ username = 'report-operator'; password = 'report-password-only'; role = 'ROLE_PARKING_OPERATOR'; parkingLotId = $first.parkingLot.id } | ConvertTo-Json
    $created = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/users" -Headers $headers -ContentType 'application/json' -Body $account
    if ($created.code -ne 0) { throw 'Could not create scoped account.' }
    $operatorBody = @{ username = 'report-operator'; password = 'report-password-only' } | ConvertTo-Json
    $operator = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/login" -ContentType 'application/json' -Body $operatorBody
    $operatorHeaders = @{ Authorization = "Bearer $($operator.data.accessToken)" }
    try {
        $null = Invoke-WebRequest -Uri "$baseUrl$script:lastExportUrl" -Headers $operatorHeaders -UseBasicParsing
        throw 'Another user could download this report.'
    } catch [System.Net.WebException] {
        if ([int]$_.Exception.Response.StatusCode -ne 404) { throw }
    }
    $operatorExport = Invoke-RestMethod -Method Post -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport/export" -Headers $operatorHeaders -ContentType 'application/json' -Body $exportBody
    if ($operatorExport.code -ne 7) { throw 'Operator must not export reports.' }
    $operatorReport = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport?reportingPeriod=2026-11" -Headers $operatorHeaders
    if (@($operatorReport.data).Count -ne 1 -or $operatorReport.data[0].parkingLot.id -ne $first.parkingLot.id) { throw 'Operator report scope failed.' }
    $operatorSearch = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport?reportingPeriod=2026-11&keyword=dia%20chi%20gia%20lap" -Headers $operatorHeaders
    if (@($operatorSearch.data).Count -ne 1 -or $operatorSearch.data[0].parkingLot.id -ne $first.parkingLot.id) { throw 'Search exposed results outside operator scope.' }
    $forbiddenLot = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport/summary?reportingPeriod=2026-11&parkingLotId=$($second.parkingLot.id)" -Headers $operatorHeaders
    if ($forbiddenLot.data.totalRevenue -ne 0) { throw 'Operator accessed another parking lot.' }
    if (@($forbiddenLot.data.topRevenueParkingLots).Count -ne 0) { throw 'Operator accessed another parking lot ranking.' }
    $districtBody = @{ username = 'report-district'; password = 'report-password-only'; role = 'ROLE_DISTRICT_ADMIN'; parkingLotId = $first.parkingLot.id } | ConvertTo-Json
    $districtCreated = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/users" -Headers $headers -ContentType 'application/json' -Body $districtBody
    if ($districtCreated.code -ne 0) { throw 'Could not create district account.' }
    $districtLoginBody = @{ username = 'report-district'; password = 'report-password-only' } | ConvertTo-Json
    $district = Invoke-RestMethod -Method Post -Uri "$baseUrl/dev/auth/login" -ContentType 'application/json' -Body $districtLoginBody
    $districtHeaders = @{ Authorization = "Bearer $($district.data.accessToken)" }
    Save-BusinessExport $exportBody $districtHeaders $exportPath 1
    & $pythonPath (Join-Path $root 'tests/check_business_report_xlsx.py') $exportPath 1 555000
    if ($LASTEXITCODE -ne 0) { throw 'Scoped Excel report verification failed.' }
    Save-BusinessExport $pdfBody $districtHeaders $pdfPath 1
    & $pythonPath (Join-Path $root 'tests/check_business_report_pdf.py') $pdfPath 1 '555,000.00'
    if ($LASTEXITCODE -ne 0) { throw 'Scoped PDF report verification failed.' }
    $deleted = Invoke-RestMethod -Method Delete -Uri "$baseUrl/gdci/parking/parkingLot/businessOperation/$($first.id)" -Headers $headers
    if ($deleted.code -ne 0) { throw 'Failed to soft-delete test record.' }
    $afterDelete = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport/summary?reportingPeriod=2026-11" -Headers $headers
    if ($afterDelete.data.totalRevenue -ne 2220000 -or $afterDelete.data.totalVehicleCount -ne 120) { throw 'Deleted record was included in report.' }
    Save-BusinessExport $exportBody $headers $exportPath 4
    & $pythonPath (Join-Path $root 'tests/check_business_report_xlsx.py') $exportPath 4 2220000
    if ($LASTEXITCODE -ne 0) { throw 'Soft-deleted record was exported.' }
    $updatedBody = @{ revenue = 700000; completedVehicleCount = 30; note = 'ranking test' } | ConvertTo-Json
    $updated = Invoke-RestMethod -Method Put -Uri "$baseUrl/gdci/parking/parkingLot/businessOperation/$($second.id)" -Headers $headers -ContentType 'application/json' -Body $updatedBody
    if ($updated.code -ne 0) { throw 'Could not prepare ranking test.' }
    $ranked = Invoke-RestMethod -Uri "$baseUrl/gdci/parking/parkingLot/parkingBusinessReport/summary?reportingPeriod=2026-11" -Headers $headers
    if ($ranked.data.topRevenueParkingLots[0].parkingLot.id -ne $second.parkingLot.id -or $ranked.data.topRevenueParkingLots[0].percentage -ne 29.6) { throw 'Ranking order or percentage incorrect.' }
    Write-Output 'Seeded API returned five matching business results.'
    Write-Output 'Report list, summary, period and lot filters, access scope, and soft delete checks passed.'
}
finally {
    if ($null -ne $server -and -not $server.HasExited) { Stop-Process -Id $server.Id -Force; $server.WaitForExit() }
    if (Test-Path -LiteralPath $databasePath) { Remove-Item -LiteralPath $databasePath -Force }
    if (Test-Path -LiteralPath $exportPath) { Remove-Item -LiteralPath $exportPath -Force }
    if (Test-Path -LiteralPath $pdfPath) { Remove-Item -LiteralPath $pdfPath -Force }
    if (Test-Path -LiteralPath $exportDirectory) {
        $resolvedExportDirectory = (Resolve-Path -LiteralPath $exportDirectory).Path
        $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
        if (-not $resolvedExportDirectory.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Export test directory is outside temp.' }
        Remove-Item -LiteralPath $resolvedExportDirectory -Recurse -Force
    }
    $backupPattern = [IO.Path]::GetFileNameWithoutExtension($databasePath) + '.backup-demo-*.db'
    Get-ChildItem -LiteralPath ([IO.Path]::GetTempPath()) -Filter $backupPattern -File | Remove-Item -Force
}
