param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory,
    [string]$ManifestPath
)

$ErrorActionPreference = "Stop"

$packageRoot = (Resolve-Path -LiteralPath $PackageDirectory).Path
$packageRootItem = Get-Item -LiteralPath $packageRoot -Force
if (-not $packageRootItem.PSIsContainer) {
    throw "Package path is not a directory: $PackageDirectory"
}
if ($packageRootItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw "Package directory must not be a reparse point."
}

function Get-PackageFiles([string]$root) {
    $directories = [System.Collections.Generic.Stack[string]]::new()
    $directories.Push($root)
    while ($directories.Count -gt 0) {
        $directory = $directories.Pop()
        foreach ($path in [IO.Directory]::EnumerateFileSystemEntries($directory)) {
            $item = Get-Item -LiteralPath $path -Force
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Package contains a reparse-point entry: $path"
            }

            if ($item.PSIsContainer) {
                $directories.Push($item.FullName)
            } else {
                $item
            }
        }
    }
}

$manifest = if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    Join-Path $packageRoot "SHA256SUMS.txt"
} else {
    (Resolve-Path -LiteralPath $ManifestPath).Path
}
$manifestItem = Get-Item -LiteralPath $manifest
if ($manifestItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw "SHA-256 manifest must not be a reparse point."
}

$entries = @{}
foreach ($line in Get-Content -LiteralPath $manifest) {
    if ([string]::IsNullOrWhiteSpace($line)) {
        continue
    }

    if ($line -notmatch '^(?<hash>[0-9a-fA-F]{64})\s{2}(?<path>.+)$') {
        throw "Invalid SHA-256 manifest entry: $line"
    }

    $relativePath = $Matches.path.Trim()
    if ([IO.Path]::IsPathRooted($relativePath) -or $relativePath -split '[\\/]' -contains '..') {
        throw "Manifest path escapes the package: $relativePath"
    }

    if ($entries.ContainsKey($relativePath)) {
        throw "Duplicate SHA-256 manifest entry: $relativePath"
    }

    $entries[$relativePath] = $Matches.hash.ToLowerInvariant()
}

if ($entries.Count -eq 0) {
    throw "The SHA-256 manifest contains no files."
}

$verified = 0
foreach ($entry in $entries.GetEnumerator()) {
    $candidate = Join-Path $packageRoot $entry.Key
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        $candidate = Join-Path (Get-Location) $entry.Key
    }

    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "Manifest file is missing: $($entry.Key)"
    }

    $candidateItem = Get-Item -LiteralPath $candidate
    if ($candidateItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Manifest file must not be a reparse point: $($entry.Key)"
    }
    $resolvedCandidate = (Resolve-Path -LiteralPath $candidate).Path
    $relativeCandidate = [IO.Path]::GetRelativePath($packageRoot, $resolvedCandidate)
    if ([IO.Path]::IsPathRooted($relativeCandidate) -or
        $relativeCandidate -eq '..' -or
        $relativeCandidate.StartsWith("..$([IO.Path]::DirectorySeparatorChar)", [StringComparison]::Ordinal) -or
        $relativeCandidate.StartsWith("..$([IO.Path]::AltDirectorySeparatorChar)", [StringComparison]::Ordinal)) {
        throw "Manifest file is outside the package: $($entry.Key)"
    }

    $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $resolvedCandidate).Hash.ToLowerInvariant()
    if ($actualHash -ne $entry.Value) {
        throw "SHA-256 mismatch for $($entry.Key)."
    }

    $verified++
}

$manifestPath = (Resolve-Path -LiteralPath $manifest).Path
$actualFiles = @(Get-PackageFiles $packageRoot | Where-Object FullName -ne $manifestPath)
if ($actualFiles.Count -ne $verified) {
    throw "Manifest covers $verified file(s), but the package contains $($actualFiles.Count) file(s)."
}

Write-Output "Verified $verified SHA-256 package file(s) in $packageRoot."
