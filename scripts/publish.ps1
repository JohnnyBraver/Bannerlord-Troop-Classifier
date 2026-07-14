param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$moduleName = "TroopClassifier"
$publishDirectory = Join-Path $repoRoot "publish"
$temporaryDirectory = Join-Path $repoRoot ".tmp_publish"
$gameModulesRoot = "E:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules"

& (Join-Path $PSScriptRoot "build.ps1") -Configuration $Configuration -Deploy
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$sourceDirectory = Join-Path $gameModulesRoot $moduleName
if (-not (Test-Path $sourceDirectory)) {
    throw "Could not find deployed module at $sourceDirectory"
}

New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null
if (Test-Path $temporaryDirectory) {
    Remove-Item -Recurse -Force $temporaryDirectory
}

try {
    $modulePackageDirectory = Join-Path $temporaryDirectory $moduleName
    New-Item -ItemType Directory -Force -Path $modulePackageDirectory | Out-Null
    Copy-Item -Path "$sourceDirectory\*" -Destination $modulePackageDirectory -Recurse -Force
    Get-ChildItem -Path $modulePackageDirectory -Filter "*.pdb" -Recurse | Remove-Item -Force

    $zipPath = Join-Path $publishDirectory "$moduleName.zip"
    if (Test-Path $zipPath) {
        Remove-Item -Force $zipPath
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($temporaryDirectory, $zipPath)
    Write-Host "Packaged $zipPath" -ForegroundColor Green
}
finally {
    if (Test-Path $temporaryDirectory) {
        Remove-Item -Recurse -Force $temporaryDirectory
    }
}
