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
$webView2RuntimeIdentifier = if ([string]::IsNullOrWhiteSpace($RuntimeIdentifier)) { "win-x64" } else { $RuntimeIdentifier }

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $rootDir "artifacts\publish\WuGesture"
}

$runningProcess = @(Get-Process -Name "WuGesture" -ErrorAction SilentlyContinue)
if ($runningProcess) {
    $ids = ($runningProcess | Select-Object -ExpandProperty Id) -join ", "
    Write-Host "Stopping running WuGesture process(es): $ids"
    $runningProcess | Stop-Process -Force
    $runningProcess | Wait-Process
}

Write-Host "Publishing WuGesture application output"
Write-Host "Configuration: $Configuration"
Write-Host "Output: $OutputPath"
Write-Host "WebView2 runtime: $webView2RuntimeIdentifier"

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

$webSourcePath = Join-Path $rootDir "dist\web"
$webTargetPath = Join-Path $OutputPath "Web\dist"
if (-not (Test-Path $webSourcePath)) {
    throw "Web frontend output was not found at $webSourcePath."
}

if (Test-Path $webTargetPath) {
    Get-ChildItem -Path $webTargetPath -Force | Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $webTargetPath -Force | Out-Null
}

Copy-Item -Path (Join-Path $webSourcePath "*") -Destination $webTargetPath -Recurse -Force

$webView2LoaderSourcePath = Join-Path $OutputPath "runtimes\$webView2RuntimeIdentifier\native\WebView2Loader.dll"
$webView2LoaderTargetPath = Join-Path $OutputPath "WebView2Loader.dll"
if (-not (Test-Path $webView2LoaderSourcePath)) {
    throw "WebView2Loader.dll for $webView2RuntimeIdentifier was not found at $webView2LoaderSourcePath."
}

Copy-Item -LiteralPath $webView2LoaderSourcePath -Destination $webView2LoaderTargetPath -Force

Write-Host ""
Write-Host "Publish complete:"
Write-Host $OutputPath
