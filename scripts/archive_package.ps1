param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory,
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

$packageRoot = (Resolve-Path -LiteralPath $PackageDirectory).Path
$output = [IO.Path]::GetFullPath($OutputPath)
if ((Test-Path -LiteralPath $output -PathType Container) -or [IO.Path]::GetExtension($output) -ne ".zip") {
    throw "OutputPath must be a zip file path, not a directory."
}

$outputDirectory = Split-Path -Parent $output
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

if ($output.StartsWith($packageRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputPath must be outside PackageDirectory."
}

if (Test-Path -LiteralPath $output) {
    Remove-Item -LiteralPath $output -Force
}

Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $output -CompressionLevel Optimal
Write-Output "Archived $packageRoot to $output"
