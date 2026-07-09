param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$projectPath = Join-Path $rootDir "src\MyGesture.App\MyGesture.App.csproj"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $rootDir "artifacts\publish\WuGesture"
}

$runningProcess = @(
    Get-Process -Name "WuGesture" -ErrorAction SilentlyContinue
    Get-Process -Name "MyGesture.App" -ErrorAction SilentlyContinue
)
if ($runningProcess) {
    $ids = ($runningProcess | Select-Object -ExpandProperty Id) -join ", "
    throw "Wu Gesture is still running. Close it before publishing. Process id(s): $ids"
}

Write-Host "Publishing Wu Gesture"
Write-Host "Configuration: $Configuration"
Write-Host "Output: $OutputPath"

dotnet publish $projectPath -c $Configuration -o $OutputPath --self-contained false

Write-Host ""
Write-Host "Publish complete:"
Write-Host $OutputPath
