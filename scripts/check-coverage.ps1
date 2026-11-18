param(
    [string]$ResultsDirectory = "artifacts/test-results",
    [double]$MinimumLineRate = 23.0,
    [double]$MinimumBranchRate = 21.0
)

$ErrorActionPreference = "Stop"

$allReports = @(Get-ChildItem -LiteralPath $ResultsDirectory -Recurse -Filter coverage.cobertura.xml -ErrorAction SilentlyContinue)

if ($allReports.Count -eq 0) {
    throw "未在 $ResultsDirectory 下找到 coverage.cobertura.xml，请先运行带覆盖率的测试。"
}

# 一次测试运行会为每个测试工程各写一份报告，同一批报告的写入时间很接近。
# 只比较最新一批（10 分钟窗口），既覆盖多工程，又不会被历史遗留报告干扰。
$newest = $allReports | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$reports = @($allReports | Where-Object {
        ($newest.LastWriteTime - $_.LastWriteTime) -le [TimeSpan]::FromMinutes(10)
    })

# 每一份报告都必须达到下限，避免只校验一份而忽略同批次中落后的工程。
$failures = @()

foreach ($report in $reports) {
    [xml]$coverage = Get-Content -LiteralPath $report.FullName
    $lineRate = [math]::Round([double]$coverage.coverage.'line-rate' * 100, 2)
    $branchRate = [math]::Round([double]$coverage.coverage.'branch-rate' * 100, 2)

    Write-Host "$($report.FullName)"
    Write-Host "  Line coverage: $lineRate% (minimum $MinimumLineRate%)"
    Write-Host "  Branch coverage: $branchRate% (minimum $MinimumBranchRate%)"

    if ($lineRate -lt $MinimumLineRate) {
        $failures += "行覆盖率 $lineRate% 低于下限 $MinimumLineRate%（$($report.FullName)）。"
    }

    if ($branchRate -lt $MinimumBranchRate) {
        $failures += "分支覆盖率 $branchRate% 低于下限 $MinimumBranchRate%（$($report.FullName)）。"
    }
}

if ($failures.Count -gt 0) {
    throw ($failures -join [Environment]::NewLine)
}
