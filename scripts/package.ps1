param(
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputDirectory = "artifacts/package",
    [switch]$SelfContained,
    [string]$FfmpegDirectory = ""
)

$ErrorActionPreference = "Stop"
$selfContainedValue = if ($SelfContained.IsPresent) { "true" } else { "false" }

dotnet publish src/FrameTheory.Desktop/FrameTheory.Desktop.csproj `
    --configuration Release `
    --runtime $RuntimeIdentifier `
    --self-contained $selfContainedValue `
    --output $OutputDirectory

if (-not [string]::IsNullOrWhiteSpace($FfmpegDirectory)) {
    $directoryItem = Get-Item -LiteralPath $FfmpegDirectory -ErrorAction Stop
    if ($directoryItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "FfmpegDirectory cannot be a reparse-point directory."
    }

    $toolRoot = (Resolve-Path -LiteralPath $FfmpegDirectory -ErrorAction Stop).Path
    foreach ($toolName in @("ffmpeg.exe", "ffprobe.exe")) {
        $toolPath = Join-Path $toolRoot $toolName
        if (-not (Test-Path -LiteralPath $toolPath -PathType Leaf)) {
            throw "FfmpegDirectory must contain $toolName."
        }
    }
    foreach ($bundleFile in Get-ChildItem -LiteralPath $toolRoot -File) {
        if ($bundleFile.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "FfmpegDirectory cannot contain reparse-point files: $($bundleFile.Name)."
        }

        Copy-Item -LiteralPath $bundleFile.FullName -Destination (Join-Path $OutputDirectory $bundleFile.Name) -Force
    }
}

$manifestPath = Join-Path $OutputDirectory "SHA256SUMS.txt"
$packageRoot = (Resolve-Path $OutputDirectory).Path
$lines = foreach ($file in Get-ChildItem $OutputDirectory -File -Recurse | Where-Object Name -ne "SHA256SUMS.txt" | Sort-Object FullName) {
    $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName
    $relativePath = [IO.Path]::GetRelativePath($packageRoot, $file.FullName)
    "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), $relativePath
}
$lines | Set-Content $manifestPath
Write-Output "Published $RuntimeIdentifier (self-contained=$selfContainedValue) to $OutputDirectory"
