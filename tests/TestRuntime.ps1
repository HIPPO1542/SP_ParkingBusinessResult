function Resolve-ParkingPython {
    if ($env:PARKING_PYTHON) {
        return (Get-Command $env:PARKING_PYTHON -ErrorAction Stop).Source
    }
    $bundledPython = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
    if (Test-Path -LiteralPath $bundledPython) { return $bundledPython }
    return (Get-Command python -ErrorAction Stop).Source
}

$env:PYTHONUTF8 = '1'
