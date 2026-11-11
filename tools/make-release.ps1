# Builds the shareable zip: a self-contained Windows build (no .NET install needed) in
# release\SleepytimeAmbience-<version>-win-x64.zip, ready to upload to GitHub Releases.
# Run from anywhere: powershell -ExecutionPolicy Bypass -File tools\make-release.ps1 (or double-click "Make Release.bat").
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

# Read the version from Program.cs so the zip name always matches the window title.
$m = Select-String -Path Program.cs -Pattern 'const string Version = "([^"]+)"'
if (-not $m) { throw 'Could not find the Version constant in Program.cs' }
$version = $m.Matches[0].Groups[1].Value

# Publish a self-contained build into a clean folder.
$out = Join-Path $root 'release\SleepytimeAmbience'
if (Test-Path (Join-Path $root 'release')) { Remove-Item (Join-Path $root 'release') -Recurse -Force }
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -o $out
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

# Zip the folder (the zip holds one SleepytimeAmbience folder, so unzipping stays tidy).
$zip = Join-Path $root "release\SleepytimeAmbience-$version-win-x64.zip"
Compress-Archive -Path $out -DestinationPath $zip
$mb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
"Made $zip ($mb MB). Upload it at https://github.com/dontdoubtdusty/sleepytime_ambience/releases/new with tag v$version."
