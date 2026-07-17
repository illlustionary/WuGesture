[CmdletBinding()]
param(
    [string]$ReleaseDirectory = "",
    [switch]$Manual
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$overrideVariableName = "WUGESTURE_DOTNET_ROOT_OVERRIDE"

if ([string]::IsNullOrWhiteSpace($ReleaseDirectory)) {
    $releaseDirectory = Get-ChildItem -Path (Join-Path $rootDir "artifacts\release") -Directory -Filter "WuGesture-v*" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
} else {
    $releaseDirectory = [System.IO.Path]::GetFullPath($ReleaseDirectory)
}

if ([string]::IsNullOrWhiteSpace($releaseDirectory)) {
    throw "No local release directory was found. Pass -ReleaseDirectory with the extracted release directory."
}

$launcherPath = Join-Path $releaseDirectory "WuGesture.exe"
if (-not (Test-Path $launcherPath)) {
    throw "WuGesture.exe was not found in $releaseDirectory."
}

if (-not $Manual -and -not ("WuGestureMissingRuntimeTest.NativeMethods" -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

namespace WuGestureMissingRuntimeTest;

public static class NativeMethods
{
    public const uint BmClick = 0x00F5;
    public const int IdYes = 6;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string className, string windowName);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDlgItem(IntPtr dialog, int itemId);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
'@
}

$temporaryDotnetRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("WuGesture-dotnet-test-" + [Guid]::NewGuid().ToString("N"))
$previousOverride = [Environment]::GetEnvironmentVariable($overrideVariableName, "Process")

try {
    New-Item -ItemType Directory -Path $temporaryDotnetRoot | Out-Null
    [Environment]::SetEnvironmentVariable($overrideVariableName, $temporaryDotnetRoot, "Process")

    Write-Host "Testing missing .NET Desktop Runtime with: $launcherPath"
    $launcherProcess = Start-Process -FilePath $launcherPath -PassThru

    if (-not $Manual) {
        $deadline = [DateTime]::UtcNow.AddSeconds(10)
        $confirmed = $false

        while ([DateTime]::UtcNow -lt $deadline) {
            $dialog = [WuGestureMissingRuntimeTest.NativeMethods]::FindWindow("#32770", "WuGesture - 缺少运行环境")
            if ($dialog -ne [IntPtr]::Zero) {
                [uint32]$dialogProcessId = 0
                [WuGestureMissingRuntimeTest.NativeMethods]::GetWindowThreadProcessId($dialog, [ref]$dialogProcessId) | Out-Null
                if ($dialogProcessId -ne [uint32]$launcherProcess.Id) {
                    Start-Sleep -Milliseconds 100
                    continue
                }

                $yesButton = [WuGestureMissingRuntimeTest.NativeMethods]::GetDlgItem($dialog, [WuGestureMissingRuntimeTest.NativeMethods]::IdYes)
                if ($yesButton -ne [IntPtr]::Zero) {
                    [WuGestureMissingRuntimeTest.NativeMethods]::SendMessage($yesButton, [WuGestureMissingRuntimeTest.NativeMethods]::BmClick, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
                    $confirmed = $true
                    break
                }
            }

            Start-Sleep -Milliseconds 100
        }

        if (-not $confirmed) {
            throw "The missing-runtime dialog did not appear within 10 seconds."
        }

        Write-Host "The download confirmation was clicked. Confirm that the Microsoft download page opened in your browser."
    } else {
        Write-Host "In the WuGesture dialog, select Yes to open the Microsoft download page."
    }

    Wait-Process -Id $launcherProcess.Id
}
finally {
    [Environment]::SetEnvironmentVariable($overrideVariableName, $previousOverride, "Process")
    if (Test-Path $temporaryDotnetRoot) {
        Remove-Item -LiteralPath $temporaryDotnetRoot -Recurse -Force
    }
}
