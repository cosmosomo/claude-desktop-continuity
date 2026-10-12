$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$artifactsRoot = Join-Path $repositoryRoot 'artifacts'
$stageRoot = Join-Path $artifactsRoot ('package-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$packageRoot = Join-Path $stageRoot 'ClaudeDesktopContinuity-windows-x64'
if (Test-Path -LiteralPath $packageRoot) { throw "Package directory already exists: $packageRoot. Choose a clean checkout before packaging again." }
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'dist'),(Join-Path $packageRoot 'scripts\continuity'),(Join-Path $packageRoot 'licenses') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'docs\screenshots') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'docs\DESIGN.md') -Destination (Join-Path $packageRoot 'docs')
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'docs\screenshots\workspace.png') -Destination (Join-Path $packageRoot 'docs\screenshots')
foreach ($name in @('ClaudeDesktopContinuity.exe','switcher-cli.exe','switcher-tray.exe','first-run-guide.bmp')) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "dist\$name") -Destination (Join-Path $packageRoot 'dist')
}
foreach ($name in @('Start.cmd','Start.ps1','README.md','LICENSE','THIRD_PARTY.md','SECURITY.md','VALIDATION.md')) {
    Copy-Item -LiteralPath (Join-Path $repositoryRoot $name) -Destination $packageRoot
}
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'dist\Continuity.exe') -Destination $packageRoot
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'scripts\continuity\Setup.ps1') -Destination (Join-Path $packageRoot 'scripts\continuity')
Push-Location $repositoryRoot
try {
    $modules = go list -deps -f '{{if .Module}}{{.Module.Path}}|{{.Module.Dir}}{{end}}' . | Where-Object { $_ } | Sort-Object -Unique
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve module license locations.' }
    foreach ($module in $modules) {
        $parts = $module.Split('|'); if ($parts[0] -eq 'github.com/TinoA/claude-desktop-switcher') { continue }
        if ($parts.Length -ne 2 -or -not (Test-Path -LiteralPath $parts[1])) { throw "Missing module source: $($parts[0])" }
        if ($parts[0] -eq 'github.com/mattn/go-localereader') {
            # 该版本将 MIT 声明及作者放在 README，没有独立许可证文件；保留实际声明，不能凭空补造作者通知。
            $declaration = Join-Path $parts[1] 'README.md'
            if ((Get-Content -LiteralPath $declaration -Raw) -notmatch '(?s)## License\s+MIT\s+## Author') { throw 'go-localereader license declaration changed.' }
            $licenses = @(Get-Item -LiteralPath $declaration)
        } else {
            $licenses = @(Get-ChildItem -LiteralPath $parts[1] -File | Where-Object { $_.Name -match '^(LICENSE|LICENCE|COPYING|NOTICE)' })
        }
        if ($licenses.Count -eq 0) { throw "No license found for module: $($parts[0])" }
        $destination = Join-Path (Join-Path $packageRoot 'licenses') ($parts[0] -replace '[^a-zA-Z0-9.-]', '_')
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        foreach ($license in $licenses) { Copy-Item -LiteralPath $license.FullName -Destination $destination }
    }
    $goRoot = go env GOROOT
    if ($LASTEXITCODE -ne 0) { throw 'Cannot locate Go runtime license.' }
    Copy-Item -LiteralPath (Join-Path $goRoot 'LICENSE') -Destination (Join-Path $packageRoot 'licenses\Go-runtime-LICENSE')
} finally { Pop-Location }
$archive = Join-Path $artifactsRoot 'ClaudeDesktopContinuity-v0.2.0-windows-x64.zip'
Compress-Archive -LiteralPath $packageRoot -DestinationPath $archive -Force
Get-FileHash -LiteralPath $archive -Algorithm SHA256 | Format-List
