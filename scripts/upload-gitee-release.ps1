param(
    [Parameter(Mandatory = $true)]
    [string]$Repository,
    [Parameter(Mandatory = $true)]
    [string]$Token,
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [Parameter(Mandatory = $true)]
    [string]$NotesPath,
    [Parameter(Mandatory = $true)]
    [string]$AssetPath,
    [string]$TargetCommitish = "main"
)

$ErrorActionPreference = "Stop"

$encodedRepository = ($Repository -split "/" | ForEach-Object { [uri]::EscapeDataString($_) }) -join "/"
$accessToken = [uri]::EscapeDataString($Token)
$releaseEndpoint = "https://gitee.com/api/v5/repos/$encodedRepository/releases?access_token=$accessToken"
$notes = Get-Content -LiteralPath $NotesPath -Raw

$release = Invoke-RestMethod -Method Post -Uri $releaseEndpoint -ContentType "application/json" -Body (@{
        tag_name = $Version
        target_commitish = $TargetCommitish
        name = "WuGesture $Version"
        body = $notes
    } | ConvertTo-Json)

$uploadEndpoint = "https://gitee.com/api/v5/repos/$encodedRepository/releases/$($release.id)/attach_files?access_token=$accessToken"
Invoke-RestMethod -Method Post -Uri $uploadEndpoint -Form @{ file = Get-Item -LiteralPath $AssetPath } | Out-Null

Write-Host "Uploaded $Version to Gitee repository $Repository."
