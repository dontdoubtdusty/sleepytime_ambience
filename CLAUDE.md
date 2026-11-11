# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project
Drowse: a cozy, lo-fi ambient scene player for falling asleep, for Windows 10/11 x64. It replaces long ambient videos that stop when the internet is unreliable: everything (pictures and sound) lives on disk and is drawn live, so nothing streams. C# / .NET 10, Raylib-cs (drawing, shaders, window), NAudio (sound output through WASAPI). No test suite or linter. GitHub repo: `dontdoubtdusty/sleepytime_ambience`; the local folder is `sleepytime_ambience`. Commit and push only when I ask.

## Commands
- Run: `dotnet run` (slow Debug build; fine for looks, not for judging sound). Build: `dotnet build`. Release check: `dotnet build -c Release`.
- It is a `WinExe` (assembly `Drowse`), so there is no console window; run the exe from bash to see raylib's log lines.
- Make the clickable copy: `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=partial -p:EnableCompressionInSingleFile=true -p:InvariantGlobalization=true -p:DebugType=None -o dist` (trimmed and compressed, about 12 MB exe; `BuiltInComInteropSupport` in the project file keeps the sound output working). `dist/` is gitignored. The Desktop shortcut "Drowse" points at `dist\Drowse.exe`, so republish after changes I should see, and close the running app first.
- Screenshots (works on this PC): `Drowse.exe --shot <sceneIndex> <out.png>` renders 150 frames at a fixed 1/30 s step and saves the scene's render texture. Read the PNG to check looks. It captures the scene texture only, not the shader pass, the HUD or the dim overlay unless the code says otherwise (`claude/file-map.md`).
- Test-only: `SLEEPY_ROTATE_SECS=4` (with auto-rotate on in the settings) makes auto-rotate switch every 4 seconds; `SLEEPY_AUDIO_STATS=1` also prints a line each time the scene changes.
- Sound check: `Drowse.exe --bench` renders 30 s of every layer with no device and prints speed, level and which layers loaded a real recording. `set SLEEPY_AUDIO_STATS=1` before a normal run prints the audio callback count and slowest callback when the app closes normally; a slowest callback near 200 ms means audible stutter.
- Speed check: `Drowse.exe --perf` (frame times, garbage collections, start-up). Animal check: `Drowse.exe --animals sheet.png`. Release zips: `Make Release.bat` makes a full zip and a small update zip (no sounds; add `-WithSounds` to the script when a sound changed).
- Smoke test after every change: `claude/qa-plan.md` section 3.

## Architecture
Flat project: all `.cs` files in the repo root, no namespaces. `Assets/**` (recordings, shaders, credits) is copied to the build and publish output automatically. `claude/file-map.md` has one entry per file and a "Where to find things" index; use it before searching.
- Scenes live one per file (14 today: keys 1-9, 0, F1-F4) in `Scenes/` and draw into a fixed 640x360 render texture (`Gfx.W`, `Gfx.H`; all scene coordinates are in that space), which `Program.cs` scales to the window with letterboxing. Scenes simulate in `Update(dt)` and draw in `Draw()`; keep those separate. Scenes know nothing about the window or the UI.
- Sound is a mixer of looping real recordings, one per `Layer`. `AmbientAudio` implements NAudio's `ISampleProvider`, so Windows pulls samples on its own thread. `Next()` runs on that thread: no allocation, no locks, no raylib calls, no logging. The only values the main thread writes are `Target[]`, `Master` and `Fade`. Never go back to raylib's audio streams: polling them from the render loop caused constant stutter (see the session log).
- Short loops are layered (`Layered` in `Audio.cs`) so repeats blur. A scene can also list random sound `Events` (owl, chimes, gull, pop, thunder); `Program.cs` counts them down and calls `AmbientAudio.PlayEvent`, which only writes one volatile int for the audio thread to pick up.
- A scene lists its sound layers (`Scene.Layers`: layer, label, default volume). Settings store per-scene volumes by scene name and reset a scene's volumes if its layer list changes length. The `Layer` enum value is the index into the mixer's arrays, so adding a layer means raising `LayerCount`.
- Blending erodes the render texture's alpha, so `Program.cs` forces alpha back to opaque after drawing the scene (`Rlgl.ColorMask`). Keep that when touching the draw path.
- Raylib 6 API notes that bit us: this build cannot decode FLAC (use wav, ogg or mp3); `DrawCircleGradient` takes a `Vector2`; `DrawTriangle` needs counter-clockwise points; `BeginScissorMode` works inside texture mode.
- Persistence: `Settings` (bottom of `Program.cs`) writes `%APPDATA%\Drowse\settings.json`. Windows is kept awake while the app runs (`SetThreadExecutionState`).
- Every recording is listed with its source link in `Assets/CREDITS.md` (a record of where it came from, not a legal need). Add a line there when adding a sound.

## Before editing
- Never edit code or docs without my OK. A plan I have approved (for example "do the shader, controls and new scenes") covers the edits it describes.
- For non-trivial changes, ask only questions that change what you will build.
- For trivial fixes (a typo, a value I named), one confirmation is enough.
- When you must ask me something, use clickable multiple choice, max 4 options per round, recommended option first and labeled "(Recommended)".
- If the answer is obvious or easy to change later, decide and tell me what you chose.
- If a task touches "Open decisions" or an [Undecided] item in claude/vision.md, or is not covered by it, ask instead of guessing.

## Style
- No emojis anywhere: messages, code, comments, docs, UI text, placeholders.
- If something needs an icon, draw it with shapes in code or inline SVG.
- Challenge my ideas. Do not sugarcoat problems. Tell me when a plan is bad.
- The look is cozy lo-fi, not realism: soft warm light, limited palette, gentle motion. Nothing flashes, jumps or loops visibly fast; this is watched half-asleep in a dark room. Brightness and volume changes ease in and out.

## Building
- You do the building. Build features all the way through; do not hand me pieces to write.
- Comment in plain language: one comment per method or block saying what it does, not how. Keep file header comments accurate.
- Keep state and logic separate from rendering.
- Prefer real recordings over synthesized sound. This app is for me and one friend (zip on GitHub Releases), never sold. Asset licenses and attribution are a parked future problem: do not raise them; take the best-sounding file from any source you can download without logging in (OpenGameArt has worked well).
- If you cannot run or compile the code, re-read your changes for errors before handing them over.
- I cannot hear or see what you build until I run it. Say plainly what you verified (build, `--shot`, `--bench`, callback stats) and what only I can judge (how it sounds and looks).

## After each change
- Summarize in a few lines: what changed, why, and which files you touched.
- If you used a language, library or framework pattern I may not have seen (GLSL shader, `ISampleProvider`, additive blending), name it and explain it in one or two sentences, once.

## Bookkeeping
- At session start, read claude/file-map.md, claude/session-log.md and claude/vision.md before searching the code. The code wins if they disagree; flag the mismatch.
- `Version` in `Program.cs` shows in the window title and names the release zip. Raise it before each `Make Release.bat`.

## Wrap up
When I say "wrap up", do all of these:
1. Update claude/file-map.md if files changed.
2. Add a dated entry to claude/session-log.md (what was done, what is next) and add 1 to the session counter.
3. Propose claude/vision.md updates if a change made it wrong or finished a backlog item.
4. Republish `dist/` if the Desktop shortcut should show the latest build.
5. Give me a short summary.

## Instructions
When I write "Instructions:", read `claude/instructions-for-claude.md` first and act on it together with the rest of my message.

## Review and reuse
- Before writing a helper, search for an existing one (`Gfx.cs`, `claude/file-map.md`, grep). In every change summary, say "reused X" or "no existing equivalent, added Y".
- Keep changes small enough to read as one diff in a few minutes. If a change will be bigger, say so and propose a split first.
- After each feature, say in one line which files a reader should open to check it.
- Scenes share drawing helpers (`Gfx`, `Fire`, tree and window helpers). A new scene reuses them instead of copying; if two scenes need the same new helper, it goes in `Gfx.cs` once.

## File health
- At wrap up, flag any file that grew past about 800 lines or any class that now has a second job, and note stale line counts in `claude/file-map.md`. `Scenes.cs` holds every scene today; split it to one file per scene before it passes 800 lines.
- Every 5 features (or when I ask), do a duplication and dead-code pass. Report only; I decide what to merge. Cleanup goes in its own change, never mixed into a feature.

## Decisions
- `claude/vision.md` and the session log record why, not only what: the choice made, what was rejected, and what would make us revisit it.
