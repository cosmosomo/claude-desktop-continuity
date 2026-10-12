$ErrorActionPreference = 'Stop'
try {
    $configurationPath = Join-Path $PSScriptRoot 'config.local.json'
    if (-not (Test-Path -LiteralPath $configurationPath)) { & (Join-Path $PSScriptRoot 'scripts\continuity\Setup.ps1') }
    $application = Join-Path $PSScriptRoot 'dist\ClaudeDesktopContinuity.exe'
    Start-Process -FilePath $application -ArgumentList ('"' + $configurationPath + '"') -WorkingDirectory (Join-Path $PSScriptRoot 'dist') -WindowStyle Hidden
} catch {
    [Console]::Error.WriteLine($_.Exception.ToString())
    Read-Host 'Startup failed. Press Enter to close'
    exit 1
}
