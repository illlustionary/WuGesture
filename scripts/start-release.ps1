param(
    [string]$Version = "",
    [string]$Remote = "github",
    [string]$Branch = "main"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Read-Host "请输入发行版本号（例如 v0.99）"
}

$Version = $Version.Trim()
if ($Version -notmatch "^v\d+(\.\d+){1,3}(-[0-9A-Za-z.-]+)?$") {
    throw "Version must use the v0.99 format."
}

$pendingChanges = @(git status --porcelain | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($pendingChanges.Count -ne 0) {
    throw "Working tree is not clean. Commit or stash changes before starting a release."
}

git rev-parse --verify "refs/tags/$Version" 2>$null
if ($LASTEXITCODE -eq 0) {
    throw "Tag $Version already exists. Choose a new version."
}

git tag -a $Version -m "Release $Version"
if ($LASTEXITCODE -ne 0) {
    throw "Unable to create release tag $Version."
}

git push $Remote "HEAD:refs/heads/$Branch"
if ($LASTEXITCODE -ne 0) {
    throw "Unable to push $Branch to $Remote. The local tag has not been pushed."
}

git push $Remote "refs/tags/$Version"
if ($LASTEXITCODE -ne 0) {
    throw "Unable to push release tag $Version to $Remote."
}

Write-Host "Release $Version started. GitHub Actions will build and publish it."
