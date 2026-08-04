param(
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $rootDir "artifacts\debug"
}

$publishDirectory = Join-Path $OutputPath "WuGesture"
$archivePath = Join-Path $OutputPath "WuGesture-debug-windows-x64.zip"

if (Test-Path $OutputPath) {
    Remove-Item -LiteralPath $OutputPath -Recurse -Force
}

New-Item -ItemType Directory -Path $OutputPath | Out-Null

& (Join-Path $scriptDir "publish-app.ps1") -Configuration Debug -OutputPath $publishDirectory -RuntimeIdentifier "win-x64"
Compress-Archive -LiteralPath $publishDirectory -DestinationPath $archivePath -CompressionLevel Optimal -Force

Write-Host "Debug package complete:"
Write-Host "Archive: $archivePath"
