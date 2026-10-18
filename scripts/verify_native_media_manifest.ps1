param(
    [Parameter(Mandatory = $true)]
    [string]$Directory,
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath
)

$ErrorActionPreference = "Stop"

$rootItem = Get-Item -LiteralPath $Directory -ErrorAction Stop
if (-not $rootItem.PSIsContainer -or ($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    throw "Native media bundle directory is invalid or linked: $Directory"
}

$manifestItem = Get-Item -LiteralPath $ManifestPath -Force -ErrorAction Stop
if ($manifestItem.PSIsContainer -or ($manifestItem.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    throw "Native media manifest must be a regular, non-linked file: $ManifestPath"
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
if ($manifest.schemaVersion -ne "frame-theory.native-media-manifest.v1") {
    throw "Unsupported native media manifest schema."
}
if ($null -eq $manifest.files -or $manifest.files.Count -eq 0) {
    throw "Native media manifest contains no files."
}

$entries = @(Get-ChildItem -LiteralPath $rootItem.FullName -Force)
foreach ($entry in $entries) {
    if ($entry.PSIsContainer) {
        throw "Native media bundle must contain files only: $($entry.Name)."
    }
    if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "Native media bundle contains a reparse-point entry: $($entry.Name)."
    }
}

$files = @($entries | Sort-Object Name)
$records = @($manifest.files | Sort-Object name)
if ($files.Count -ne $records.Count) {
    throw "Native media manifest file count does not match the bundle."
}

for ($index = 0; $index -lt $files.Count; $index++) {
    $file = $files[$index]
    $record = $records[$index]
    if ($file.Name -ne $record.name) {
        throw "Native media manifest file set does not match the bundle."
    }
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName).Hash.ToLowerInvariant()
    if ($hash -ne $record.sha256 -or $file.Length -ne [int64]$record.bytes) {
        throw "Native media manifest hash or size mismatch: $($file.Name)."
    }
}

foreach ($toolName in @("ffmpeg", "ffprobe")) {
    $tool = $manifest.tools.$toolName
    if ($null -eq $tool -or [string]::IsNullOrWhiteSpace($tool.name) -or [string]::IsNullOrWhiteSpace($tool.sha256) -or [string]::IsNullOrWhiteSpace($tool.version)) {
        throw "Native media manifest is missing tool provenance: $toolName."
    }
    $toolPath = Join-Path $rootItem.FullName $tool.name
    if (-not (Test-Path -LiteralPath $toolPath -PathType Leaf)) {
        throw "Native media manifest tool is missing from the bundle: $($tool.name)."
    }
    $toolHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $toolPath).Hash.ToLowerInvariant()
    if ($toolHash -ne $tool.sha256) {
        throw "Native media manifest tool hash mismatch: $($tool.name)."
    }
}

Write-Output "Verified native media manifest for $($files.Count) file(s)."
