param(
    [string]$OutputPath = "artifacts/sbom/nuget-packages.json",
    [string]$LicenseOutputPath = ""
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

if ([string]::IsNullOrWhiteSpace($LicenseOutputPath)) {
    $directory = Split-Path -Parent $OutputPath
    $LicenseOutputPath = Join-Path $directory "nuget-licenses.json"
}

$nugetRoot = if (-not [string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES)) {
    $env:NUGET_PACKAGES
} elseif (-not [string]::IsNullOrWhiteSpace($env:USERPROFILE)) {
    Join-Path $env:USERPROFILE ".nuget/packages"
} elseif (-not [string]::IsNullOrWhiteSpace($env:HOME)) {
    Join-Path $env:HOME ".nuget/packages"
} else {
    throw "NuGet package cache location is unavailable; set NUGET_PACKAGES or HOME."
}

if ([string]::IsNullOrWhiteSpace($nugetRoot)) {
    throw "NuGet package cache location is unavailable."
}

if (-not (Test-Path -LiteralPath $nugetRoot -PathType Container)) {
    throw "NuGet package cache directory does not exist: $nugetRoot"
}

$packages = foreach ($project in @($report.projects)) {
    foreach ($framework in @($project.frameworks)) {
        foreach ($package in @($framework.topLevelPackages) + @($framework.transitivePackages)) {
            if ($null -ne $package -and $package.id -and $package.resolvedVersion) {
                [PSCustomObject]@{
                    Id = [string]$package.id
                    Version = [string]$package.resolvedVersion
                }
            }
        }
    }
}

$licenseReport = foreach ($package in $packages | Sort-Object Id, Version -Unique) {
    $packageDirectory = Join-Path (Join-Path $nugetRoot $package.Id.ToLowerInvariant()) $package.Version
    $nuspec = Get-ChildItem -LiteralPath $packageDirectory -Filter "*.nuspec" -File -ErrorAction SilentlyContinue |
        Select-Object -First 1
    $licenseExpression = $null
    $licenseUrl = $null
    if ($nuspec) {
        [xml]$metadataDocument = Get-Content -LiteralPath $nuspec.FullName
        $metadata = $metadataDocument.package.metadata
        if ($metadata.license) {
            $licenseExpression = [string]$metadata.license.'#text'
            if ([string]::IsNullOrWhiteSpace($licenseExpression)) {
                $licenseExpression = [string]$metadata.license
            }
        }
        $licenseUrl = [string]$metadata.licenseUrl
    }

    [ordered]@{
        id = $package.Id
        version = $package.Version
        licenseExpression = $licenseExpression
        licenseUrl = $licenseUrl
        metadataFound = $null -ne $nuspec
        source = if ($nuspec) { $nuspec.FullName } else { $null }
    }
}

$licenseDirectory = Split-Path -Parent $LicenseOutputPath
if (-not [string]::IsNullOrWhiteSpace($licenseDirectory)) {
    New-Item -ItemType Directory -Force -Path $licenseDirectory | Out-Null
}
$licenseReport | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $LicenseOutputPath
Write-Output "Wrote NuGet license metadata for $(@($licenseReport).Count) package version(s) to $LicenseOutputPath."
