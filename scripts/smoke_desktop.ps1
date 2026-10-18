param(
    [int]$TimeoutSeconds = 20,
    [string]$AssemblyPath = "src/FrameTheory.Desktop/bin/Release/net10.0/FrameTheory.Desktop.dll",
    [switch]$RequireWindowTitle
)

$ErrorActionPreference = "Stop"

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnet = if ($dotnetCommand) {
    $dotnetCommand.Source
} else {
    $userDotnet = Join-Path $env:USERPROFILE ".dotnet/dotnet.exe"
    if (-not (Test-Path -LiteralPath $userDotnet -PathType Leaf)) {
        throw "The .NET host executable was not found on PATH or at $userDotnet."
    }

    $userDotnet
}
$dll = (Resolve-Path -LiteralPath $AssemblyPath).Path
$process = Start-Process -FilePath $dotnet -ArgumentList @($dll) -WorkingDirectory (Split-Path $dll) -PassThru
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
$title = $null

try {
    if (-not $RequireWindowTitle) {
        Start-Sleep -Seconds 2
        $process.Refresh()
        if ($process.HasExited) {
            throw "Desktop shell exited during the Unix startup smoke (exit code $($process.ExitCode))."
        }

        Write-Output "Desktop shell process remained alive: $($process.Id)"
        return
    }

    while ([DateTime]::UtcNow -lt $deadline) {
        if ($process.HasExited) {
            throw "Desktop shell exited before creating its window (exit code $($process.ExitCode))."
        }

        $process.Refresh()
        if (-not [string]::IsNullOrWhiteSpace($process.MainWindowTitle)) {
            $title = $process.MainWindowTitle
            break
        }

        Start-Sleep -Milliseconds 250
    }

    if ($title -ne "Frame Theory Studio AI") {
        throw "Desktop shell did not expose the expected window title within $TimeoutSeconds seconds."
    }

    Write-Output "Desktop shell window detected: $title"
} finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
        $process.WaitForExit(5000)
    }
}
