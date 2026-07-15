param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "",
    [string]$Version = "",
    [string]$RuntimeIdentifier = "",
    [switch]$SelfContained
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
    Write-Host "Stopping running WuGesture process(es): $ids"
    $runningProcess | Stop-Process -Force
    $runningProcess | Wait-Process
}

Write-Host "Publishing WuGesture"
Write-Host "Configuration: $Configuration"
Write-Host "Output: $OutputPath"

if (Test-Path $OutputPath) {
    Get-ChildItem -Path $OutputPath -Force | Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

$publishArguments = @(
    "publish",
    $projectPath,
    "-c", $Configuration,
    "-o", $OutputPath
)

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $publishArguments += "-p:Version=$Version"
}

if (-not [string]::IsNullOrWhiteSpace($RuntimeIdentifier)) {
    $publishArguments += "-r", $RuntimeIdentifier
    if ($SelfContained) {
        $publishArguments += "--self-contained"
    }
}

dotnet @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host ""
Write-Host "Publish complete:"
Write-Host $OutputPath
