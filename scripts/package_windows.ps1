param(
    [ValidateSet("win-x64")]
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputDirectory = "artifacts/installer",
    [string]$PublishDirectory = "artifacts/installer-publish",
    [string]$InnoSetupCompiler = "iscc.exe",
    [ValidatePattern('^\d+\.\d+\.\d+([.-][0-9A-Za-z.-]+)?$')]
    [string]$Version = "0.1.0",
    [string]$FfmpegDirectory = ""
)

$ErrorActionPreference = "Stop"

$publishPath = (Resolve-Path -LiteralPath $PublishDirectory -ErrorAction SilentlyContinue)?.Path
if (-not $publishPath) {
    dotnet publish src/FrameTheory.Desktop/FrameTheory.Desktop.csproj `
        --configuration Release `
        --runtime $RuntimeIdentifier `
        --self-contained true `
        --output $PublishDirectory
    $publishPath = (Resolve-Path -LiteralPath $PublishDirectory).Path
}

if (-not [string]::IsNullOrWhiteSpace($FfmpegDirectory)) {
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

        Copy-Item -LiteralPath $bundleFile.FullName -Destination (Join-Path $publishPath $bundleFile.Name) -Force
    }
}

$compiler = Get-Command $InnoSetupCompiler -ErrorAction SilentlyContinue
if (-not $compiler) {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Leaf) }
    $compilerPath = $candidates | Select-Object -First 1
    if (-not $compilerPath) {
        throw "Inno Setup ISCC.exe was not found. Install Inno Setup before creating the Windows installer."
    }
} else {
    $compilerPath = $compiler.Source
}

$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
& $compilerPath "/DPublishDir=$publishPath" "/DOutputDir=$outputPath" "/DAppVersion=$Version" "/DOutputBaseFilename=FrameTheoryStudioAI-$RuntimeIdentifier" "packaging/windows/FrameTheoryStudio.iss"
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE."
}

Write-Output "Built unsigned Windows installer in $outputPath"
