param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^v\d+(\.\d+){1,3}(-[0-9A-Za-z.-]+)?$")]
    [string]$Version,
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$packageVersion = $Version.Substring(1)

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $rootDir "artifacts\release"
}

$publishDirectory = Join-Path $OutputPath "WuGesture"
$archivePath = Join-Path $OutputPath "WuGesture-$Version-windows-x64.zip"
$notesPath = Join-Path $OutputPath "RELEASE_NOTES.md"

if (Test-Path $OutputPath) {
    Remove-Item -LiteralPath $OutputPath -Recurse -Force
}

New-Item -ItemType Directory -Path $OutputPath | Out-Null

& (Join-Path $scriptDir "publish-app.ps1") -Configuration Release -OutputPath $publishDirectory -Version $packageVersion -RuntimeIdentifier "win-x64"

$previousTag = git describe --tags --abbrev=0 "$Version^" 2>$null
if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($previousTag)) {
    $commitRange = "$previousTag..$Version"
    $notesHeader = "## 更新内容`r`n`r`n$previousTag 到 $Version 的变更："
} else {
    $commitRange = $Version
    $notesHeader = "## 更新内容"
}

$changes = @(git log $commitRange --pretty=format:"- %s (%h)")
if ($LASTEXITCODE -ne 0) {
    throw "Unable to generate release notes from Git tag $Version. Create and push the tag before packaging."
}

if ($changes.Count -eq 0) {
    $changes = @("- 首个自动发布版本。")
}

@($notesHeader, "", $changes) | Set-Content -LiteralPath $notesPath -Encoding utf8
Compress-Archive -LiteralPath $publishDirectory -DestinationPath $archivePath -CompressionLevel Optimal -Force

Write-Host "Release package complete:"
Write-Host "Archive: $archivePath"
Write-Host "Notes: $notesPath"
