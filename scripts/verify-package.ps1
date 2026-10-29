param(
    [Parameter(Mandatory = $true)]
    [string]$ArchivePath,
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^v?\d+(\.\d+){1,3}(-[0-9A-Za-z.-]+)?$")]
    [string]$Version
)

$ErrorActionPreference = "Stop"

$archive = Get-Item -LiteralPath $ArchivePath
$expectedVersion = $Version.TrimStart('v').Split('-')[0]

$expectedEntries = @(
    "WuGesture/WuGesture.exe",
    "WuGesture/WuGesture.dll",
    "WuGesture/WuGesture.deps.json",
    "WuGesture/WuGesture.runtimeconfig.json",
    "WuGesture/Web/index.html",
    "WuGesture/WebView2Loader.dll",
    "WuGesture/Microsoft.Web.WebView2.Core.dll",
    "WuGesture/Microsoft.Web.WebView2.WinForms.dll"
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::OpenRead($archive.FullName)
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("wugesture-package-" + [Guid]::NewGuid().ToString("N"))
try {
    $entries = @{}
    foreach ($entry in $zip.Entries) {
        $entries[$entry.FullName.Replace('\', '/')] = $entry
    }

    $entryNames = @($entries.Keys)

    $missing = @($expectedEntries | Where-Object { -not $entries.ContainsKey($_) })
    if ($missing.Count -gt 0) {
        throw "发行包缺少必需文件: $($missing -join ', ')"
    }

    $forbidden = @($entryNames | Where-Object { $_ -like "*/node_modules/*" -or $_ -like "*.test.js" })
    if ($forbidden.Count -gt 0) {
        throw "发行包包含前端测试或依赖文件: $($forbidden -join ', ')"
    }

    $indexEntry = $entries["WuGesture/Web/index.html"]
    $reader = New-Object System.IO.StreamReader($indexEntry.Open())
    try {
        $indexHtml = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }

    $assetReferences = @(
        [regex]::Matches($indexHtml, '(?:src|href)="\./(assets/[^"]+)"') |
            ForEach-Object { "WuGesture/Web/" + $_.Groups[1].Value } |
            Sort-Object -Unique
    )
    if ($assetReferences.Count -eq 0) {
        throw "Web/index.html 没有引用任何前端资源，前端构建可能未执行。"
    }

    $missingAssets = @($assetReferences | Where-Object { -not $entries.ContainsKey($_) })
    if ($missingAssets.Count -gt 0) {
        throw "Web/index.html 引用了发行包中不存在的资源: $($missingAssets -join ', ')"
    }

    $exeEntry = $entries["WuGesture/WuGesture.exe"]
    New-Item -ItemType Directory -Path $tempRoot | Out-Null
    $exePath = Join-Path $tempRoot "WuGesture.exe"
    [System.IO.Compression.ZipFileExtensions]::ExtractToFile($exeEntry, $exePath, $true)

    $fileVersion = (Get-Item -LiteralPath $exePath).VersionInfo.FileVersion
    if ([string]::IsNullOrWhiteSpace($fileVersion)) {
        throw "无法读取 WuGesture.exe 的文件版本。"
    }

    $expectedFieldCount = $expectedVersion.Split('.').Count
    $actualVersion = ([version]$fileVersion).ToString($expectedFieldCount)
    if ($actualVersion -ne $expectedVersion) {
        throw "发行包版本不一致: 期望 $expectedVersion，WuGesture.exe 为 $fileVersion。"
    }
}
finally {
    $zip.Dispose()
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

Write-Host "Package verified: $($archive.Name) (version $expectedVersion)"
