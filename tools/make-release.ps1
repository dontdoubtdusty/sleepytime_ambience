# Builds the two shareable zips in release\ (upload both to GitHub Releases):
#   Drowse-<version>-win-x64.zip          the full app: first install, everything inside.
#   Drowse-<version>-update-win-x64.zip   the app without the sound files; the in-app updater downloads this one, so an update stays small.
# Add -WithSounds when a sound file changed, so the update zip carries the sounds too.
# Both are self-contained (no .NET install needed), trimmed and compressed to keep the download small.
# Run from anywhere: powershell -ExecutionPolicy Bypass -File tools\make-release.ps1 (or double-click "Make Release.bat").
param([switch]$WithSounds)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

# Read the version from Program.cs so the zip names always match the window title.
$m = Select-String -Path Program.cs -Pattern 'const string Version = "([^"]+)"'
if (-not $m) { throw 'Could not find the Version constant in Program.cs' }
$version = $m.Matches[0].Groups[1].Value

# Publish a self-contained, trimmed, compressed single-file build into a clean folder.
# (InvariantGlobalization drops the culture data, which the app does not use.)
$out = Join-Path $root 'release\Drowse'
if (Test-Path (Join-Path $root 'release')) { Remove-Item (Join-Path $root 'release') -Recurse -Force }
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=partial -p:EnableCompressionInSingleFile=true -p:InvariantGlobalization=true -p:DebugType=None -o $out
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

# Full zip (the zip holds one Drowse folder, so unzipping stays tidy).
$zip = Join-Path $root "release\Drowse-$version-win-x64.zip"
Compress-Archive -Path $out -DestinationPath $zip

# Update zip: the same folder without the sound files (unless -WithSounds).
$upd = Join-Path $root 'release\update\Drowse'
Copy-Item $out $upd -Recurse
if (-not $WithSounds) { Get-ChildItem $upd -Recurse -Include *.mp3, *.ogg, *.wav | Remove-Item }
$updZip = Join-Path $root "release\Drowse-$version-update-win-x64.zip"
Compress-Archive -Path $upd -DestinationPath $updZip
Remove-Item (Join-Path $root 'release\update') -Recurse -Force

$mb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
$umb = [math]::Round((Get-Item $updZip).Length / 1MB, 1)
"Made $zip ($mb MB) and $updZip ($umb MB). Upload both at https://github.com/dontdoubtdusty/sleepytime_ambience/releases/new with tag v$version."
