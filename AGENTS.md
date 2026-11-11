# AGENTS.md

Extra information for AI coding agents. The README is for people who just want to run the app and stays short. Claude Code reads `CLAUDE.md`; any other agent should read it too, since it holds the working rules, architecture guide and session routine. This file adds what is not there.

## Stack
C# (.NET 10), [Raylib-cs](https://github.com/raylib-cs/raylib-cs) 8.1 (raylib 6.0) for the window, drawing and shaders, [NAudio](https://github.com/naudio/NAudio) 2.2 for sound output through WASAPI. Windows 10/11 x64 only.

## Build and run
Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download).
- Run: `dotnet run`. Compile check: `dotnet build`.
- Local play copy: `dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o dist` (needs the .NET 10 runtime to play).
- Dev flags: `--shot <scene> <out.png>` (screenshot a scene), `--bench` (render audio offline and report speed and which recordings loaded).

## Files and folders
- `Assets/` (sound recordings, shaders, `CREDITS.md`) is copied to the build and publish output automatically.
- Settings live in `%APPDATA%\Drowse\settings.json`, outside the repo.
- `dist/`, `bin/` and `obj/` are git-ignored.
- `claude/file-map.md` has one entry per file and a "Where to find things" index. Read it before searching the code.

## Assets
This is a personal app that is never released, so asset licenses are not a concern. `Assets/CREDITS.md` records where each recording came from; keep it current when adding or removing sounds.
