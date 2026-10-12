param([string]$ConfigPath)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$distributionRoot = Join-Path $repositoryRoot 'dist'
New-Item -ItemType Directory -Path $distributionRoot -Force | Out-Null
Push-Location $repositoryRoot
try {
    $env:GOOS = 'windows'; $env:GOARCH = 'amd64'; $env:CGO_ENABLED = '0'
    Write-Output 'Verifying Go dependencies and upstream account engine...'
    go mod verify
    if ($LASTEXITCODE -ne 0) { throw 'Module verification failed.' }
    go vet ./...
    if ($LASTEXITCODE -ne 0) { throw 'Go vet failed.' }
    go test ./...
    if ($LASTEXITCODE -ne 0) { throw 'Go tests failed.' }
    go build -trimpath -ldflags '-s -w -X github.com/TinoA/claude-desktop-switcher/cmd.Version=continuity-0.2.1' -o (Join-Path $distributionRoot 'switcher-cli.exe') .
    if ($LASTEXITCODE -ne 0) { throw 'Account engine build failed.' }
    go build -trimpath -ldflags '-s -w -H=windowsgui -X github.com/TinoA/claude-desktop-switcher/cmd.Version=continuity-0.2.1' -o (Join-Path $distributionRoot 'switcher-tray.exe') .
    if ($LASTEXITCODE -ne 0) { throw 'Tray build failed.' }
    Copy-Item -LiteralPath (Join-Path $repositoryRoot 'installer\assets\first-run-guide.bmp') -Destination $distributionRoot
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path -LiteralPath $compiler)) { throw "Missing .NET Framework compiler: $compiler" }
    $application = Join-Path $distributionRoot 'ClaudeDesktopContinuity.exe'
    & $compiler /nologo /target:winexe /platform:x64 "/out:$application" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $repositoryRoot 'app\SessionImporter.cs') (Join-Path $repositoryRoot 'app\Continuity.cs') (Join-Path $repositoryRoot 'app\WorkspaceUI.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Continuity application build failed.' }
    & $compiler /nologo /target:winexe /platform:x64 "/out:$(Join-Path $distributionRoot 'Continuity.exe')" /reference:System.Windows.Forms.dll (Join-Path $repositoryRoot 'app\Launcher.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Double-click launcher build failed.' }
    Copy-Item -LiteralPath (Join-Path $distributionRoot 'Continuity.exe') -Destination (Join-Path $repositoryRoot 'Continuity.exe') -Force
    & $compiler /nologo /target:exe /platform:x64 "/out:$(Join-Path $distributionRoot 'LocalIntegrationTests.exe')" "/reference:$application" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $repositoryRoot 'app\LocalIntegrationTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Local integration test build failed.' }
    # 仅编译；真实账号切换验收必须获得用户允许后单独执行，不能进入 CI 自动测试。
    & $compiler /nologo /target:exe /platform:x64 "/out:$(Join-Path $distributionRoot 'LiveAcceptance.exe')" "/reference:$application" /reference:System.Web.Extensions.dll (Join-Path $repositoryRoot 'app\LiveAcceptance.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Live acceptance harness build failed.' }
    & $compiler /nologo /target:exe /platform:x64 "/out:$(Join-Path $distributionRoot 'RenderWorkspace.exe')" "/reference:$application" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $repositoryRoot 'app\RenderWorkspace.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Workspace renderer build failed.' }
    if ($ConfigPath) {
        & (Join-Path $distributionRoot 'LocalIntegrationTests.exe') ([IO.Path]::GetFullPath($ConfigPath))
        if ($LASTEXITCODE -ne 0) { throw 'Local integration checks failed.' }
    }
    Write-Output 'Build completed. Run scripts/continuity/Setup.ps1 to create local configuration and a desktop shortcut.'
} finally { Pop-Location }
