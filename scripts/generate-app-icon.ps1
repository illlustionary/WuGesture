# 从 Resources\wu.svg 生成多尺寸 wu.ico。
#
# 用法（仓库根目录）：
#   pwsh -NoProfile -File scripts\generate-app-icon.ps1
#
# 依赖：Windows 自带的 Microsoft Edge（无头模式用于把 SVG 栅格化）。脚本只在需要更新图标时手动运行，
# 不参与 dotnet build；生成的 wu.ico 提交进仓库，普通构建不需要 Edge。
# 16–64px 使用 32bpp DIB 帧（兼容旧图标 API），128/256px 使用 PNG 帧（Windows Vista+ 标准做法）。

param(
  [string]$SvgPath = "",
  [string]$IcoPath = "",
  [string]$EdgePath = ""
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $SvgPath) { $SvgPath = Join-Path $repoRoot 'src\WuGesture.App\Resources\wu.svg' }
if (-not $IcoPath) { $IcoPath = Join-Path $repoRoot 'src\WuGesture.App\Resources\wu.ico' }

if (-not (Test-Path -LiteralPath $SvgPath)) {
  throw "找不到 SVG：$SvgPath"
}

if (-not $EdgePath) {
  $candidates = @(
    "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
    "$env:LOCALAPPDATA\Microsoft\Edge\Application\msedge.exe"
  )
  $EdgePath = $candidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
}

if (-not $EdgePath) {
  throw '找不到 Microsoft Edge。请用 -EdgePath 指定 msedge.exe 的位置。'
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$workDir = Join-Path ([System.IO.Path]::GetTempPath()) ("WuGesture-icon-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $workDir | Out-Null

# 只渲染一张 1024px 母图再高质量缩小：Chromium 无头窗口小于约 200px 时不会按窗口尺寸排版，
# 直接截 16/128px 会得到空白或裁切图。
function Render-SvgMaster([int]$size, [string]$outputPath) {
  $profileDir = Join-Path $workDir 'edge-profile'
  $arguments = @(
    '--headless=new',
    '--disable-gpu',
    '--no-sandbox',
    '--hide-scrollbars',
    '--force-device-scale-factor=1',
    '--run-all-compositor-stages-before-draw',
    '--virtual-time-budget=3000',
    '--default-background-color=00000000',
    "--user-data-dir=$profileDir",
    "--window-size=$size,$size",
    "--screenshot=$outputPath",
    ('file:///' + ($previewHtml -replace '\\', '/'))
  )

  # 必须等待浏览器主进程结束：用 & 调用时 Edge 会立刻返回，截图由子进程稍后写入。
  Start-Process -FilePath $EdgePath -ArgumentList $arguments -Wait -NoNewWindow | Out-Null

  if (-not (Test-Path -LiteralPath $outputPath)) {
    throw "Edge 没有生成 $size px 母图。"
  }

  $image = [System.Drawing.Image]::FromFile($outputPath)
  try {
    if ($image.Width -ne $size -or $image.Height -ne $size) {
      throw "Edge 截图尺寸是 $($image.Width)x$($image.Height)，期望 ${size}x${size}。"
    }
  } finally {
    $image.Dispose()
  }
}

function Resize-Icon([string]$masterPath, [int]$size, [string]$outputPath) {
  $source = [System.Drawing.Bitmap]::new($masterPath)
  try {
    $target = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
      $graphics = [System.Drawing.Graphics]::FromImage($target)
      try {
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $attributes = [System.Drawing.Imaging.ImageAttributes]::new()
        try {
          $attributes.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
          $rect = [System.Drawing.Rectangle]::new(0, 0, $size, $size)
          $graphics.DrawImage(
            $source,
            $rect,
            0,
            0,
            $source.Width,
            $source.Height,
            [System.Drawing.GraphicsUnit]::Pixel,
            $attributes
          )
        } finally {
          $attributes.Dispose()
        }
      } finally {
        $graphics.Dispose()
      }

      $target.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
      $target.Dispose()
    }
  } finally {
    $source.Dispose()
  }
}

function New-DibFrame([string]$pngPath, [int]$size) {
  $bitmap = [System.Drawing.Bitmap]::new($pngPath)
  try {
    if ($bitmap.Width -ne $size -or $bitmap.Height -ne $size) {
      throw "图标帧尺寸不匹配：$($bitmap.Width)x$($bitmap.Height) != ${size}x${size}。"
    }

    $rect = [System.Drawing.Rectangle]::new(0, 0, $size, $size)
    $data = $bitmap.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
      $stride = $data.Stride
      $pixels = New-Object byte[] ($size * $size * 4)
      for ($y = 0; $y -lt $size; $y++) {
        $sourceRow = $size - 1 - $y
        [System.Runtime.InteropServices.Marshal]::Copy(
          [System.IntPtr]::Add($data.Scan0, $sourceRow * $stride),
          $pixels,
          $y * $size * 4,
          $size * 4
        )
      }
    } finally {
      $bitmap.UnlockBits($data)
    }
  } finally {
    $bitmap.Dispose()
  }

  $maskStride = [int]([math]::Ceiling($size / 8.0))
  if ($maskStride % 4 -ne 0) { $maskStride += 4 - ($maskStride % 4) }
  $mask = New-Object byte[] ($maskStride * $size)
  for ($y = 0; $y -lt $size; $y++) {
    for ($x = 0; $x -lt $size; $x++) {
      if ($pixels[($y * $size + $x) * 4 + 3] -eq 0) {
        $mask[$y * $maskStride + [math]::Floor($x / 8)] = $mask[$y * $maskStride + [math]::Floor($x / 8)] -bor (0x80 -shr ($x % 8))
      }
    }
  }

  $xorSize = $size * $size * 4
  $header = New-Object byte[] 40
  [BitConverter]::GetBytes([int]40).CopyTo($header, 0)
  [BitConverter]::GetBytes([int]$size).CopyTo($header, 4)
  [BitConverter]::GetBytes([int]($size * 2)).CopyTo($header, 8)
  [BitConverter]::GetBytes([int16]1).CopyTo($header, 12)
  [BitConverter]::GetBytes([int16]32).CopyTo($header, 14)
  [BitConverter]::GetBytes([int]0).CopyTo($header, 16)
  [BitConverter]::GetBytes([int]$xorSize).CopyTo($header, 20)

  $frame = New-Object byte[] ($header.Length + $xorSize + $mask.Length)
  $header.CopyTo($frame, 0)
  $pixels.CopyTo($frame, $header.Length)
  $mask.CopyTo($frame, $header.Length + $xorSize)
  # 逗号阻止 PowerShell 把 byte[] 展开成多个输出对象，否则 BinaryWriter 只会写入单个字节。
  return ,$frame
}

$previewHtml = Join-Path $workDir 'preview.html'
$svgUri = 'file:///' + ((Resolve-Path -LiteralPath $SvgPath).Path -replace '\\', '/')
$html = @"
<!doctype html>
<html><head><meta charset="utf-8"><style>
html, body { margin: 0; padding: 0; background: transparent; overflow: hidden; }
img { display: block; width: 100vw; height: 100vh; }
</style></head><body><img src="$svgUri" alt=""></body></html>
"@
[System.IO.File]::WriteAllText($previewHtml, $html)

try {
  $masterPath = Join-Path $workDir 'master.png'
  Render-SvgMaster -size 1024 -outputPath $masterPath

  $frames = foreach ($size in $sizes) {
    $pngPath = Join-Path $workDir "icon-$size.png"
    Resize-Icon -masterPath $masterPath -size $size -outputPath $pngPath
    $data = if ($size -le 64) { New-DibFrame -pngPath $pngPath -size $size } else { [System.IO.File]::ReadAllBytes($pngPath) }
    [pscustomobject]@{ Size = $size; Data = [byte[]]$data }
  }

  $offset = 6 + 16 * $frames.Count
  $stream = [System.IO.MemoryStream]::new()
  try {
    $writer = [System.IO.BinaryWriter]::new($stream)
    try {
      $writer.Write([int16]0)
      $writer.Write([int16]1)
      $writer.Write([int16]$frames.Count)

      foreach ($frame in $frames) {
        $writer.Write([byte]$(if ($frame.Size -ge 256) { 0 } else { $frame.Size }))
        $writer.Write([byte]$(if ($frame.Size -ge 256) { 0 } else { $frame.Size }))
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([int16]1)
        $writer.Write([int16]32)
        $writer.Write([int]$frame.Data.Length)
        $writer.Write([int]$offset)
        $offset += $frame.Data.Length
      }

      foreach ($frame in $frames) {
        $writer.Write($frame.Data)
      }

      $writer.Flush()
      [System.IO.File]::WriteAllBytes($IcoPath, $stream.ToArray())
    } finally {
      $writer.Dispose()
    }
  } finally {
    $stream.Dispose()
  }

  $verify = [System.Drawing.Icon]::new($IcoPath)
  try {
    Write-Output "已生成 $IcoPath（$([math]::Round((Get-Item -LiteralPath $IcoPath).Length / 1KB, 1)) KB，默认帧 $($verify.Width)x$($verify.Height)，共 $($sizes.Count) 个尺寸）"
  } finally {
    $verify.Dispose()
  }
} finally {
  Remove-Item -LiteralPath $workDir -Recurse -Force -ErrorAction SilentlyContinue
}
