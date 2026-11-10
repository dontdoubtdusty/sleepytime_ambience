# Sleepytime Ambience

Offline, cozy lo-fi ambient scenes for falling asleep. The pictures are drawn live and the sounds are
recordings stored on disk, so nothing streams and nothing stops when the internet drops.

Scenes: Fireplace (with a sleeping cat), Snowy Cabin, Night Forest, Rainy Library, Night Train, Seaside Lighthouse, Tent in the Rain, Observation Window, Aquarium, City Window.

## Run
    dotnet run -c Release

Requires the .NET 10 SDK. Built with C# and [Raylib-cs](https://github.com/raylib-cs/raylib-cs),
with sound through NAudio. For a double-click copy, see `CLAUDE.md` (Commands).

## Controls
| Key | Action |
|---|---|
| 1-9, 0 / Tab | Switch scene (ten scenes) |
| Up / Down | Master volume |
| Left / Right | Select sound layer |
| PgUp / PgDn (or + / -) | Layer volume |
| T | Sleep timer (off, 15, 30, 45, 60, 90, 120, 180 min). Fades out over the last 5 min, then quits |
| D | Dim screen |
| F / F11 | Borderless fullscreen |
| R | Auto-rotate scenes (off, 5, 10, 20, 30, 60 min) |
| L | Lo-fi look on / off |
| P | Hide or show the control panel |
| H | Help |
| Esc | Quit |

Move the mouse to show a clickable panel with every control; the cursor and panel hide after 3 seconds. Settings are saved to
`%APPDATA%\SleepytimeAmbience\settings.json`. Windows is kept awake while the app runs.

Working notes for Claude Code are in `CLAUDE.md` and the `claude/` folder. Sound sources are in `Assets/CREDITS.md`.
