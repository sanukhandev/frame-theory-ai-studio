param(
    [Parameter(Mandatory = $true)]
    [string]$Directory,
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,
    [string]$FfmpegName = "ffmpeg",
    [string]$FfprobeName = "ffprobe"
)

$ErrorActionPreference = "Stop"

$rootItem = Get-Item -LiteralPath $Directory -ErrorAction Stop
if (-not $rootItem.PSIsContainer) {
    throw "Native media bundle path is not a directory: $Directory"
}
if ($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw "Native media bundle directory cannot be a reparse point."
}

$rootPath = $rootItem.FullName
$entries = @(Get-ChildItem -LiteralPath $rootPath -Force)
foreach ($entry in $entries) {
    if ($entry.PSIsContainer) {
        throw "Native media bundle must contain files only: $($entry.Name)."
    }
    if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Native media bundle cannot contain a reparse-point file: $($entry.Name)."
    }
}

$files = @($entries | Sort-Object Name)
if ($files.Count -eq 0) {
    throw "Native media bundle directory is empty: $Directory"
}

function Get-RequiredTool([string]$name) {
    if ([string]::IsNullOrWhiteSpace($name) -or [IO.Path]::GetFileName($name) -ne $name) {
        throw "Native media bundle tool name must be a file name: $name."
    }

    $path = Join-Path $rootPath $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Native media bundle must contain $name."
    }

    $item = Get-Item -LiteralPath $path
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Native media bundle tool cannot be a reparse point: $name."
    }

    return $item
}

function Get-ToolVersion([IO.FileInfo]$tool) {
    $version = (& $tool.FullName -version 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($version)) {
        throw "Native media tool did not return version output: $($tool.Name)."
    }

    if ($version.Length -gt 8192) {
        return $version.Substring(0, 8192)
    }

    return $version
}

$ffmpeg = Get-RequiredTool $FfmpegName
$ffprobe = Get-RequiredTool $FfprobeName
$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$manifest = [ordered]@{
    schemaVersion = "frame-theory.native-media-manifest.v1"
    files = @(
        foreach ($file in $files) {
            [ordered]@{
                name = $file.Name
                sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash.ToLowerInvariant()
                bytes = $file.Length
            }
        }
    )
    tools = [ordered]@{
        ffmpeg = [ordered]@{
            name = $ffmpeg.Name
            sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $ffmpeg.FullName).Hash.ToLowerInvariant()
            version = Get-ToolVersion $ffmpeg
        }
        ffprobe = [ordered]@{
            name = $ffprobe.Name
            sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $ffprobe.FullName).Hash.ToLowerInvariant()
            version = Get-ToolVersion $ffprobe
        }
    }
}

$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputPath
Write-Output "Wrote native media manifest for $($files.Count) file(s) to $OutputPath."
