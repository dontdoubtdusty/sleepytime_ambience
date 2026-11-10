# Sleepytime Ambience

Offline, procedural ambient scenes for falling asleep. Everything (visuals and sound) is generated
in code, so there are no videos or audio files to download and nothing stops when the internet drops.

Scenes: Fireplace, Snowy Cabin, Night Forest, Rainy Library.

## Run
    dotnet run -c Release

Requires the .NET 10 SDK. Built with C# and [Raylib-cs](https://github.com/raylib-cs/raylib-cs).

## Controls
| Key | Action |
|---|---|
| 1-4 / Tab | Switch scene |
| Up / Down | Master volume |
| Left / Right | Select sound layer |
| PgUp / PgDn (or + / -) | Layer volume |
| T | Sleep timer (off, 15, 30, 45, 60, 90, 120, 180 min). Fades out over the last 5 min, then quits |
| D | Dim screen |
| F / F11 | Borderless fullscreen |
| H | Help |
| Esc | Quit |

The cursor and on-screen text hide after 3 seconds. Settings are saved to
`%APPDATA%\SleepytimeAmbience\settings.json`. Windows is kept awake while the app runs.
