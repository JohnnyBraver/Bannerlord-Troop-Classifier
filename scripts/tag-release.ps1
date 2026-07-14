param(
    [string]$CompareTo = "HEAD~1",
    [switch]$Push
)

$ErrorActionPreference = "Stop"
$moduleDirectory = "TroopClassifier"
$modulePath = "$moduleDirectory/SubModule.xml"

if ($CompareTo -eq "0000000000000000000000000000000000000000" -or [string]::IsNullOrWhiteSpace($CompareTo)) {
    Write-Host "First push has no comparison commit; release tagging is skipped." -ForegroundColor Yellow
    exit 0
}

$changedFiles = @(git diff --name-only "$CompareTo..HEAD")
if ($LASTEXITCODE -ne 0) { throw "Could not compare $CompareTo with HEAD." }
if (-not ($changedFiles | Where-Object { $_ -like "$moduleDirectory/*" })) {
    Write-Host "No classifier module files changed; no release tag required." -ForegroundColor Green
    exit 0
}

[xml]$xml = Get-Content $modulePath
$version = $xml.Module.Version.value
if ([string]::IsNullOrWhiteSpace($version)) { throw "Could not read the TroopClassifier module version." }

$tagName = "TroopClassifier-$version"
if (git tag -l $tagName) {
    Write-Host "Tag '$tagName' already exists." -ForegroundColor Yellow
    exit 0
}

git tag -a $tagName -m "Release TroopClassifier version $version" HEAD
if ($LASTEXITCODE -ne 0) { throw "Could not create tag '$tagName'." }
if ($Push) {
    git push origin $tagName
    if ($LASTEXITCODE -ne 0) { throw "Could not push tag '$tagName'." }
}

Write-Host "Created release tag '$tagName'." -ForegroundColor Green
