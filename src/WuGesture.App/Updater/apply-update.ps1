param(
    [Parameter(Mandatory = $true)][int]$ProcessId,
    [Parameter(Mandatory = $true)][string]$InstallDir,
    [Parameter(Mandatory = $true)][string]$StagingDir,
    [Parameter(Mandatory = $true)][string]$BackupDir,
    [string]$LaunchArguments = "",
    [Parameter(Mandatory = $true)][string]$LogPath
)

$ErrorActionPreference = "Stop"
$swapped = $false

function Write-UpdateLog([string]$Message) {
    try {
        $line = "[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Message
        Add-Content -LiteralPath $LogPath -Value $line -Encoding UTF8
    } catch {
    }
}

function Test-ProcessAlive([int]$Id) {
    return [bool](Get-Process -Id $Id -ErrorAction SilentlyContinue)
}

function Remove-DirectoryWithRetry([string]$Path) {
    for ($attempt = 0; $attempt -lt 10; $attempt++) {
        try {
            if (Test-Path -LiteralPath $Path) {
                Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
            }

            return $true
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }

    return $false
}

function Start-WuGesture([string]$ExePath, [string]$Arguments) {
    if ([string]::IsNullOrWhiteSpace($Arguments)) {
        return Start-Process -FilePath $ExePath -WorkingDirectory (Split-Path -Parent $ExePath) -PassThru
    }

    return Start-Process -FilePath $ExePath -ArgumentList $Arguments -WorkingDirectory (Split-Path -Parent $ExePath) -PassThru
}

function Restore-PreviousVersion {
    if (-not (Test-Path -LiteralPath $BackupDir)) {
        return
    }

    Write-UpdateLog "Restoring the previous version from $BackupDir."
    Remove-DirectoryWithRetry $InstallDir | Out-Null
    try {
        Move-Item -LiteralPath $BackupDir -Destination $InstallDir -ErrorAction Stop
        Write-UpdateLog "Previous version restored."
    } catch {
        Write-UpdateLog "Unable to restore the previous version: $_"
        return
    }

    try {
        $null = Start-WuGesture (Join-Path $InstallDir "WuGesture.exe") $LaunchArguments
        Write-UpdateLog "Previous version restarted."
    } catch {
        Write-UpdateLog "Previous version was restored but could not be restarted: $_"
    }
}

try {
    Write-UpdateLog "Waiting for process $ProcessId to exit."
    for ($attempt = 0; $attempt -lt 240 -and (Test-ProcessAlive $ProcessId); $attempt++) {
        Start-Sleep -Milliseconds 250
    }

    if (Test-ProcessAlive $ProcessId) {
        throw "The running application did not exit in time."
    }

    # Give WebView2 child processes and antivirus scanners a moment to release file handles.
    Start-Sleep -Milliseconds 400

    Write-UpdateLog "Moving the current installation aside."
    $moved = $false
    for ($attempt = 0; $attempt -lt 20 -and -not $moved; $attempt++) {
        try {
            if (Test-Path -LiteralPath $BackupDir) {
                Remove-DirectoryWithRetry $BackupDir | Out-Null
            }

            Move-Item -LiteralPath $InstallDir -Destination $BackupDir -ErrorAction Stop
            $moved = $true
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }

    if (-not $moved) {
        throw "Unable to move the current installation aside."
    }

    try {
        Move-Item -LiteralPath $StagingDir -Destination $InstallDir -ErrorAction Stop
        $swapped = $true
    } catch {
        Write-UpdateLog "Moving the new version into place failed: $_"
        Restore-PreviousVersion
        throw "Unable to move the new version into place."
    }

    $exePath = Join-Path $InstallDir "WuGesture.exe"
    if (-not (Test-Path -LiteralPath $exePath)) {
        throw "The new installation does not contain WuGesture.exe."
    }

    Write-UpdateLog "Starting the updated application."
    # The restart flag makes the new version open the settings window on the About page so the user can see
    # that the update finished. It is only appended here, after the files were replaced successfully.
    $updatedArguments = if ([string]::IsNullOrWhiteSpace($LaunchArguments)) {
        "--updated"
    } else {
        "$LaunchArguments --updated"
    }

    $startedAt = (Get-Date).AddSeconds(-2)
    $startedProcess = Start-WuGesture $exePath $updatedArguments

    # Wait with a real budget instead of a fixed 3 seconds: slow disks and antivirus scans can delay the first
    # process well past that, and with "run as administrator" the launched process exits and relaunches itself
    # through UAC. Check the launched PID first, then fall back to any WuGesture instance started by this update.
    $deadline = (Get-Date).AddSeconds(30)
    $running = $false
    while ((Get-Date) -lt $deadline) {
        try {
            if ($startedProcess -and -not $startedProcess.HasExited) {
                $running = $true
                break
            }
        } catch {
        }

        try {
            $candidates = @(Get-Process -Name "WuGesture" -ErrorAction SilentlyContinue | Where-Object {
                    try {
                        $_.StartTime -ge $startedAt
                    } catch {
                        $true
                    }
                })
            if ($candidates.Count -gt 0) {
                $running = $true
                break
            }
        } catch {
        }

        Start-Sleep -Milliseconds 500
    }

    if (-not $running) {
        throw "The updated application did not start."
    }

    Remove-DirectoryWithRetry $BackupDir | Out-Null
    Write-UpdateLog "Update completed successfully."
    exit 0
} catch {
    Write-UpdateLog "Update failed: $_"
    if ($swapped) {
        Restore-PreviousVersion
    }

    exit 1
} finally {
    # Remove the staging root (downloaded package, extracted files and this script) on the next cleanup pass
    # if the file is still in use here.
    try {
        $stagingRoot = Split-Path -Parent $StagingDir
        if ($stagingRoot -and (Test-Path -LiteralPath $stagingRoot)) {
            Remove-DirectoryWithRetry $stagingRoot | Out-Null
        }
    } catch {
    }
}
