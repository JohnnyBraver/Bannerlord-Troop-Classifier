param(
    [string]$BaseRef = "origin/main"
)

$ErrorActionPreference = "Stop"
$modulePath = "TroopClassifier/SubModule.xml"

if ($BaseRef -eq "0000000000000000000000000000000000000000" -or [string]::IsNullOrWhiteSpace($BaseRef)) {
    Write-Host "First push has no comparison commit; version validation is skipped." -ForegroundColor Yellow
    exit 0
}

function Parse-Version([string]$Value) {
    if ($Value -match '^v?(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?$') {
        return [Version]::new([int]$Matches[1], [int]$Matches[2], [int]$Matches[3], $(if ($Matches[4]) { [int]$Matches[4] } else { 0 }))
    }

    return $null
}

$changedFiles = @(git diff --name-only "$BaseRef...HEAD")
if ($LASTEXITCODE -ne 0) { throw "Could not compare $BaseRef with HEAD." }
if (-not ($changedFiles | Where-Object { $_ -like "TroopClassifier/*" })) {
    Write-Host "No classifier module files changed; no version bump required." -ForegroundColor Green
    exit 0
}

[xml]$headXml = Get-Content $modulePath
$headVersionText = $headXml.Module.Version.value
$headVersion = Parse-Version $headVersionText
if ($null -eq $headVersion) { throw "Current module version '$headVersionText' is not valid SemVer." }

$baseXmlText = git show "${BaseRef}:$modulePath" 2>$null
if ($LASTEXITCODE -ne 0 -or -not $baseXmlText) {
    Write-Host "TroopClassifier is new relative to $BaseRef; version validation is skipped." -ForegroundColor Yellow
    exit 0
}

[xml]$baseXml = $baseXmlText -join [Environment]::NewLine
$baseVersionText = $baseXml.Module.Version.value
$baseVersion = Parse-Version $baseVersionText
if ($null -eq $baseVersion) { throw "Base module version '$baseVersionText' is not valid SemVer." }
if ($headVersion -le $baseVersion) {
    throw "TroopClassifier changed without a version bump. Base: $baseVersionText; current: $headVersionText"
}

Write-Host "TroopClassifier version bumped from $baseVersionText to $headVersionText." -ForegroundColor Green
