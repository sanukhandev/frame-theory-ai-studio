param(
    [string]$Solution = "FrameTheory.sln",
    [string]$OutputPath = "artifacts/nuget-vulnerabilities.json"
)

$ErrorActionPreference = "Stop"

$report = dotnet list $Solution package --include-transitive --vulnerable --format json | Out-String
if ([string]::IsNullOrWhiteSpace($report)) {
    throw "The NuGet vulnerability command returned no report."
}

$report | ConvertFrom-Json | Out-Null
$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}
$report.Trim() | Set-Content -LiteralPath $OutputPath

$json = $report | ConvertFrom-Json
$vulnerabilities = [System.Collections.Generic.List[object]]::new()

function Find-Vulnerabilities([object]$Value) {
    if ($null -eq $Value -or $Value -is [string]) {
        return
    }

    if ($Value -is [System.Collections.IEnumerable]) {
        foreach ($item in $Value) {
            Find-Vulnerabilities $item
        }
        return
    }

    $property = $Value.PSObject.Properties['vulnerabilities']
    if ($null -ne $property -and $null -ne $property.Value) {
        foreach ($item in @($property.Value)) {
            $vulnerabilities.Add($item)
        }
    }

    foreach ($child in $Value.PSObject.Properties) {
        if ($child.Name -ne 'vulnerabilities') {
            Find-Vulnerabilities $child.Value
        }
    }
}

Find-Vulnerabilities $json
if ($vulnerabilities.Count -gt 0) {
    throw "NuGet vulnerability scan found $($vulnerabilities.Count) vulnerable package record(s). Review $OutputPath."
}

Write-Output "No NuGet vulnerabilities reported. Evidence: $OutputPath"
