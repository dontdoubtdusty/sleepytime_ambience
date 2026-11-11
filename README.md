# Sleepytime Ambience

Offline, cozy lo-fi ambient scenes for falling asleep. The pictures are drawn live and the sounds are
recordings stored on disk, so nothing streams and nothing stops when the internet drops.

Scenes: Fireplace (with a sleeping cat), Snowy Cabin, Night Forest, Rainy Library, Night Train, Seaside Lighthouse, Tent in the Rain, Observation Window, Aquarium, City Window, Campfire, Hot Spring, Attic Skylight, Koi Pond.

## Install (Windows 10 or 11)

Nothing else needs to be installed.

1. Go to the [Releases page](https://github.com/dontdoubtdusty/sleepytime_ambience/releases/latest).
2. Under **Assets**, click `SleepytimeAmbience-<version>-win-x64.zip` to download it.
3. Right-click the downloaded zip, choose **Extract All...**, then **Extract**. (Running it from inside the zip without extracting will not work.)
4. Open the extracted `SleepytimeAmbience` folder and double-click `SleepytimeAmbience.exe`.
5. Windows may show "Windows protected your PC", because the app is not signed. Click **More info**, then **Run anyway**. You only need to do this once.

Optional: right-click `SleepytimeAmbience.exe`, choose **Show more options**, then **Send to > Desktop (create shortcut)**.

To update, download the new zip and replace the old folder. Your settings are kept.

To uninstall, delete the `SleepytimeAmbience` folder. To also remove your settings, delete `%APPDATA%\SleepytimeAmbience`
(paste that into the File Explorer address bar).

## Using it

Move the mouse to show a clickable panel with every control; the panel and cursor hide after 3 seconds. Press H for the
keyboard help. Settings are saved automatically, and Windows will not go to sleep while the app is open.

| Key | Action |
|---|---|
| 1-9, 0, F1-F4 / Tab | Switch scene (fourteen scenes) |
| Up / Down | Master volume |
| Left / Right | Select sound layer |
| PgUp / PgDn (or + / -) | Layer volume |
| T | Sleep timer (off, 15 min up to 10 h). Fades out over the last 5 min, then quits |
| U | Stop at a clock time (22:00 to 09:00 presets; Shift+U adds 30 min) |
| D | Dim screen |
| F / F11 | Borderless fullscreen |
| R | Auto-rotate scenes (off, 5, 10, 20, 30, 60 min) |
| V | Add or remove the current scene from auto-rotate (right-click a scene button does the same) |
| S | Shuffle auto-rotate |
| E | Random sound events (owl, chimes, gulls, log pops, distant thunder) on or off |
| N | Night drift: slowly dims and quiets over 2.5 hours |
| L | Lo-fi look on / off |
| P | Hide or show the control panel |
| F12 | Save a screenshot to `Pictures\Sleepytime Ambience` |
| H | Help |
| Esc | Quit |

Sound sources are credited in `Assets/CREDITS.md` (also in the `Assets` folder of the download).

## Building from source

Requires the .NET 10 SDK. Built with C# and [Raylib-cs](https://github.com/raylib-cs/raylib-cs), with sound through NAudio.

    dotnet run -c Release

## Making a release

1. Raise `Version` near the top of `Program.cs` (it shows in the window title and names the zip).
2. Double-click `Make Release.bat`. It writes `release\SleepytimeAmbience-<version>-win-x64.zip`
   (a self-contained build, so the people you share it with need no .NET).
3. On GitHub, open **Releases > Draft a new release**, create the tag `v<version>`, drag the zip into the
   attachments box, and click **Publish release**. The Install link above always points to the newest release.

The app icon is drawn by `tools\make-icon.ps1`; rerun it after changing the design.

Working notes for Claude Code are in `CLAUDE.md` and the `claude/` folder.
