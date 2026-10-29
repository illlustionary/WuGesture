param(
    [string]$ResultsDirectory = "artifacts/test-results",
    [double]$MinimumLineRate = 21.0,
    [double]$MinimumBranchRate = 19.0
)

$ErrorActionPreference = "Stop"

$report = Get-ChildItem -LiteralPath $ResultsDirectory -Recurse -Filter coverage.cobertura.xml -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime |
    Select-Object -Last 1

if (-not $report) {
    throw "未在 $ResultsDirectory 下找到 coverage.cobertura.xml，请先运行带覆盖率的测试。"
}

[xml]$coverage = Get-Content -LiteralPath $report.FullName
$lineRate = [math]::Round([double]$coverage.coverage.'line-rate' * 100, 2)
$branchRate = [math]::Round([double]$coverage.coverage.'branch-rate' * 100, 2)

Write-Host "Line coverage: $lineRate% (minimum $MinimumLineRate%)"
Write-Host "Branch coverage: $branchRate% (minimum $MinimumBranchRate%)"

if ($lineRate -lt $MinimumLineRate) {
    throw "行覆盖率 $lineRate% 低于下限 $MinimumLineRate%。"
}

if ($branchRate -lt $MinimumBranchRate) {
    throw "分支覆盖率 $branchRate% 低于下限 $MinimumBranchRate%。"
}
