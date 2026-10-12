$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$distributionRoot = Join-Path $repositoryRoot 'dist'
$desktopRoot = Join-Path $env:APPDATA 'Claude'
$projectsRoot = Join-Path $env:USERPROFILE '.claude\projects'
$desktopLog = Join-Path $env:LOCALAPPDATA 'Claude\logs\main.log'
foreach ($required in @((Join-Path $distributionRoot 'ClaudeDesktopContinuity.exe'), (Join-Path $desktopRoot 'config.json'), $projectsRoot, $desktopLog)) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Required path missing: $required. Build the app, sign into Claude Desktop and open Code first." }
}
$configurationPath = Join-Path $repositoryRoot 'config.local.json'
if (Test-Path -LiteralPath $configurationPath) { throw "Configuration already exists: $configurationPath. It has not been overwritten." }
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Claude账号与会话.lnk'
$configuration = [ordered]@{
    desktopMode = 'official'; projectsRoot = $projectsRoot; officialDesktopRoot = $desktopRoot
    officialLogPath = $desktopLog; gatewayDesktopRoot = $null; gatewayLogPath = $null; ccSwitchSettingsPath = $null
    initialProjectPath = $null; headBytes = 262144; tailBytes = 2097152; previewBytes = 8388608
    previewMessageCount = 12; previewCharsPerMessage = 1600; maxSessions = 200; activeSessionHours = 12
    timeZoneId = [TimeZoneInfo]::Local.Id; importTimeoutSeconds = 45; pollIntervalMs = 500; switchTimeoutSeconds = 90
    logPath = (Join-Path $distributionRoot 'continuity.events.jsonl')
    compilerPath = (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe')
    shortcutPath = $shortcutPath; switcherCliPath = (Join-Path $distributionRoot 'switcher-cli.exe'); switcherGuiPath = (Join-Path $distributionRoot 'switcher-tray.exe')
}
$ccSwitchPath = Join-Path $env:USERPROFILE '.cc-switch\settings.json'
if (Test-Path -LiteralPath $ccSwitchPath) {
    $providerSettings = Get-Content -LiteralPath $ccSwitchPath -Raw | ConvertFrom-Json
    if (-not $providerSettings.currentProviderClaudeDesktop) { throw 'CC Switch settings exist but no Desktop provider is selected.' }
    $configuration.desktopMode = 'ccswitch'; $configuration.ccSwitchSettingsPath = $ccSwitchPath
    $gatewayRoot = Join-Path $env:LOCALAPPDATA 'Claude-3p'
    $gatewayLog = Join-Path $gatewayRoot 'logs\main.log'
    if ((Test-Path -LiteralPath (Join-Path $gatewayRoot 'config.json')) -and (Test-Path -LiteralPath $gatewayLog)) {
        $configuration.gatewayDesktopRoot = $gatewayRoot; $configuration.gatewayLogPath = $gatewayLog
    }
}
[IO.File]::WriteAllText($configurationPath, ($configuration | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $repositoryRoot 'Continuity.exe'
$shortcut.Arguments = ''
$shortcut.WorkingDirectory = $repositoryRoot
$shortcut.Description = '选择会话、预览对话、切换账号并继续原生 Claude Desktop'
$shortcut.Save()
Write-Output "Created: $shortcutPath"
Write-Output "Configured Desktop mode: $($configuration.desktopMode). Read README for Gateway configuration."
