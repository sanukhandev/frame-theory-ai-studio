param(
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputDirectory = "artifacts/package"
)

$ErrorActionPreference = "Stop"

dotnet publish src/FrameTheory.Desktop/FrameTheory.Desktop.csproj `
    --configuration Release `
    --runtime $RuntimeIdentifier `
    --self-contained false `
    --output $OutputDirectory

$manifestPath = Join-Path $OutputDirectory "SHA256SUMS.txt"
$packageRoot = (Resolve-Path $OutputDirectory).Path
$lines = foreach ($file in Get-ChildItem $OutputDirectory -File -Recurse | Where-Object Name -ne "SHA256SUMS.txt" | Sort-Object FullName) {
    $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $file.FullName
    $relativePath = [IO.Path]::GetRelativePath($packageRoot, $file.FullName)
    "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), $relativePath
}
$lines | Set-Content $manifestPath
Write-Output "Published $RuntimeIdentifier to $OutputDirectory"
