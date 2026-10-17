param(
    [string]$OutputPath = "artifacts/sbom/nuget-packages.json"
)

$ErrorActionPreference = "Stop"
$parent = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($parent)) {
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
}

$reportText = & dotnet list FrameTheory.sln package --include-transitive --format json 2>&1 | Out-String
if ($LASTEXITCODE -ne 0) {
    throw "dotnet package inventory failed with exit code $LASTEXITCODE."
}

$report = $reportText | ConvertFrom-Json
if ($null -eq $report.projects -or $report.projects.Count -eq 0) {
    throw "The package inventory did not contain any projects."
}

$reportText.Trim() | Set-Content -LiteralPath $OutputPath
Write-Output "Wrote NuGet dependency inventory for $($report.projects.Count) project(s) to $OutputPath."
