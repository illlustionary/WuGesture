param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$projectPath = Join-Path $rootDir "src\WuGesture.App\WuGesture.App.csproj"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $rootDir "artifacts\publish\WuGesture"
}

$runningProcess = @(
    Get-Process -Name "WuGesture" -ErrorAction SilentlyContinue
)
if ($runningProcess) {
    $ids = ($runningProcess | Select-Object -ExpandProperty Id) -join ", "
    throw "WuGesture is still running. Close it before publishing. Process id(s): $ids"
}

Write-Host "Publishing WuGesture"
Write-Host "Configuration: $Configuration"
Write-Host "Output: $OutputPath"

if (Test-Path $OutputPath) {
    Get-ChildItem -Path $OutputPath -Force | Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

dotnet publish $projectPath -c $Configuration -o $OutputPath --self-contained false

Write-Host ""
Write-Host "Publish complete:"
Write-Host $OutputPath
