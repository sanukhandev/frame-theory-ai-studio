param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory,
    [string]$ManifestPath
)

$ErrorActionPreference = "Stop"

$packageRoot = (Resolve-Path -LiteralPath $PackageDirectory).Path
$manifest = if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    Join-Path $packageRoot "SHA256SUMS.txt"
} else {
    (Resolve-Path -LiteralPath $ManifestPath).Path
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

    $resolvedCandidate = (Resolve-Path -LiteralPath $candidate).Path
    if (-not $resolvedCandidate.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Manifest file is outside the package: $($entry.Key)"
    }

    $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $resolvedCandidate).Hash.ToLowerInvariant()
    if ($actualHash -ne $entry.Value) {
        throw "SHA-256 mismatch for $($entry.Key)."
    }

    $verified++
}

$manifestLeaf = [IO.Path]::GetFileName($manifest)
$actualFiles = @(Get-ChildItem -LiteralPath $packageRoot -File -Recurse | Where-Object Name -ne $manifestLeaf)
if ($actualFiles.Count -ne $verified) {
    throw "Manifest covers $verified file(s), but the package contains $($actualFiles.Count) file(s)."
}

Write-Output "Verified $verified SHA-256 package file(s) in $packageRoot."
