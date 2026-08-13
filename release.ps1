# Builds the mod, zips it for Unity Mod Manager, and publishes a GitHub release.
# The release tag comes from the Version field in Info.json — bump it there first.
# Requires the GitHub CLI: winget install --id GitHub.cli ; gh auth login
param(
    [string]$Notes
)
$ErrorActionPreference = 'Stop'

if ($null -eq (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI (gh) not found. Install it with: winget install --id GitHub.cli   then run: gh auth login"
}

$root = $PSScriptRoot
$version = (Get-Content (Join-Path $root 'Info.json') -Raw | ConvertFrom-Json).Version
$tag = "v$version"

dotnet build (Join-Path $root 'FeatsEveryXLevels.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

# Stage the zip layout: a FeatsEveryXLevels folder that can be dropped into Mods,
# or fed to UMM's "Install mod" button as-is.
$dist = Join-Path $root 'dist'
$stage = Join-Path $dist 'FeatsEveryXLevels'
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item @(
    (Join-Path $root 'bin\Release\FeatsEveryXLevels.dll'),
    (Join-Path $root 'Info.json'),
    (Join-Path $root 'README.md')
) $stage

$zip = Join-Path $dist "FeatsEveryXLevels-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $stage -DestinationPath $zip

git -C $root push origin HEAD
if ($LASTEXITCODE -ne 0) { throw 'git push failed.' }

if (-not $Notes) { $Notes = "Install with Unity Mod Manager (drop the zip on the Mods tab), or extract into the game's Mods folder." }
gh release create $tag $zip --title "Feats Every X Levels $tag" --notes $Notes
if ($LASTEXITCODE -ne 0) { throw 'gh release create failed.' }

Write-Output "Released $tag ($zip)"
