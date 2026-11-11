# Drowse File Map

One entry per file: what it does today. Read this first each session, then open only the files the task needs. Update it when a file is added, split, renamed or deleted, and when a file's job changes. Describe the current state only; history lives in git and the session log.

Line counts are rough (refreshed Nov 9, 2026). Files over ~400 lines are marked *. Length only flags a file to look at; a long file with one job is fine.

---

## Main code (repo root, flat, no namespaces)

| File | Lines | What it does |
|---|---|---|
| `Program.cs` | 520 | Entry point and main loop (top-level statements). Dev flags `--bench` and `--shot <scene> <png>`. Creates the window, font, `AmbientAudio` and the scene list; reads keyboard input (scene keys 1-9, 0 and F1-F4, Tab, volume, layer select, timer, stop-at, dim, auto-rotate, shuffle, rotation list, sound events, night drift, fullscreen, lo-fi, help); scene changes fade to black, switch, fade in (`RequestScene`; slow fades for auto-rotate); the sleep timer (up to 10 hours, or "stop at" a clock time) and its fade; auto-rotate over the picked scenes (optionally shuffled); the random sound-event countdowns; night drift (slow dim and quieting); renders the scene into the 640x360 texture, restores the texture's alpha, draws it through the lo-fi shader (`DrawShaded`), then the dim overlay, black fade, the control panel (`RefreshPanel`) and the keyboard help (`DrawHelp`). Also holds `Settings` (JSON in `%APPDATA%\Drowse`) and `PreventSleep`. |
| `Audio.cs` | 520 | `Layer` enum and `AmbientAudio`: the mixer. Decodes each recording from `Assets/` (raylib `LoadWave`), crossfades its end into its start for a seamless loop (`Finish`), normalizes loudness, and mixes layers per sample in `Next()` on the WASAPI thread (NAudio `ISampleProvider`). Short loops (crickets, underwater, train, water, rain) play twice, the second copy half a loop away and 7% slower, so repeats blur (`Layered`, `Loop`). Random sound events (`Evt`: chimes, gull, owl, log pop, distant thunder) are one-shots started with `PlayEvent`; the owl and pop are synthesized, chimes play a random 14 s excerpt. Brown and pink noise are synthesized. Thunder is a one-shot from a rain+thunder clip, low-passed, started a little before its loudest moment. If a recording is missing, the layer falls back to a synthesized version (`Synth*` methods). `Smooth` sets how fast volumes ease (slow for auto-rotate). Keeps callback timing stats for `SLEEPY_AUDIO_STATS`. |
| `Controls.cs` | 150 | The clickable panel: `PanelModel` (values and actions the app gives it) and `ControlPanel.Draw` (scene buttons with rotation markers and right-click to add or remove a scene from auto-rotate, sliders, ten option buttons, a Hide button; shrinks to fit the window; handles clicks and slider drags). Fades in on input. Draws at window resolution, on top of the shader. |
| `Sprites.cs` | 100 | Smooth animals: each animal is painted 4x bigger into a hidden atlas texture in a "prepare" pass (`Scene.PrepareSprites`, called from `Program.cs` before the scene is drawn), then shrunk into the scene with mipmap smoothing (anti-aliased edges, sub-pixel movement). An animal draw method calls `Sprites.Begin(id, centre, w, h)` before painting and `Sprites.End()` after; no additive glow inside. |
| `AnimalLab.cs` | 230 | Dev tool: `Drowse.exe --animals <out.png>` draws each animal with and without itself, diffs the pictures, prints size, roundness, aspect (worm-like check), solidity (ragged outline), colour variety (flat blob), contrast and draw cost, and saves an enlarged sheet. Numbers are proxies; cozy is judged by eye. The koi redraw is the model: plump tapered body, wide pale fins, patches, highlight, dot eyes. |
| `Perf.cs` | 60 | Dev tool: `Drowse.exe --perf` runs every scene flat out and prints frame time, p99, worst frame, allocations per frame and start-up time. Set `SLEEPY_RARE_SECS=0.1` to include rare events. |
| `Launch.cs` | 12 | `Launch.Pick(count)`: a random number chosen when the program opens. Each scene calls it once to choose its one random look (colour, planet type); `SLEEPY_VARIANT=n` forces variant n for `--shot` tests. |
| `Intro.cs` | 100 | The opening screen: a slow brush circle (enso) drawing itself and one calm line from a list of 24, then a fade into the scene (and the sound). Any key or click skips ahead. Cosmetic only; off in `--shot` unless `SLEEPY_SHOT_UI=intro`, and off with `SLEEPY_NO_INTRO=1`. |
| `Clocks.cs` | 70 | The time every clock shows (`Clocks.Seconds`, `Hour`, `Hands`, `Text`): real local time when `Settings.RealTime` is on, otherwise a random start time at launch running at normal speed. Also `DrawLed` (red seven-segment display, used on the observation window wall) and `DrawFace`, the shared round analog face used by the library, cabin, attic, city and fireplace clocks. |
| `UiText.cs` | 28 | The text font (Lora from `Assets/Lora.ttf`, smooth-scaled; falls back to raylib's default). All on-screen text goes through `UiText.Draw` / `Measure`. |
| `Gfx.cs` | 150 | Drawing helpers shared by scenes: size constants (`W`, `H`), color helpers (`C`, `Lerp`, `WithA`), `Glow` (soft radial light, use with additive blending), `Tri` (triangle in any point order), `Vignette`, `DrawPine`. Also the `Fire` class: particle flames and embers (fireplace and cabin). |
| `Drowse.csproj` | 25 | Project file. WinExe, assembly `Drowse`, net10.0, unsafe allowed (raylib sample pointers), `Assets/**` copied to build and publish output. |

## Scenes (`Scenes/`, one file per scene; order in the array in `Program.cs` sets keys 1-9, 0, then F1-F4)

| File | Lines | Scene |
|---|---|---|
| `Scene.cs` | 50 | Base class (also the rare-event timer: `RareLength`, `RareT`, `OnRareStart`, `Step`) (also `Chimes`, true for a scene with a striking clock): name, optional random sound `Events`, per-scene sound layers (`SoundLayers`), plus brown and pink noise layers added to every scene (`Layers`), `Update`, `Draw`. |
| `FireplaceScene.cs` | 118 | 1. Brick hearth, flames, candles, and a sleeping cat (breathing, ear twitch). Layers: Fireplace, Cat purr, Grandfather clock (tick). A grandfather clock with a swinging pendulum on the left; it strikes the hour (`Chimes`). |
| `CabinScene.cs` | 133 | 2. Snowy Cabin: window onto falling snow and pines, small hearth. Layers: Fireplace, Winter wind. |
| `ForestScene.cs` | 132 | 3. Night Forest: swaying pines, moon, mist, fireflies. Layers: Crickets, Wind, Forest ambience. |
| `LibraryScene.cs` | 190 | 4. Rainy Library: shelves, lamp, tea, rain window, lightning with thunder, desk clock (`DrawDeskClock`, real local time). Layers: Rain, Thunder, Clock. |
| `TrainScene.cs` | 134 | 5. Night Train: compartment window, passing hills, village lights, telephone poles, gentle sway. Layer: Train rumble. |
| `SeasideScene.cs` | 126 | 6. Seaside Lighthouse: waves, moonlight glitter, foam, gulls, sweeping beam. Layers: Waves, Sea breeze. |
| `TentScene.cs` | 104 | 7. Tent in the Rain: lantern-lit canvas, flap view of a wet forest, drops running on the canvas. Layers: Rain inside the tent (`TentRain`), Wind, Crickets (off). |
| `SpaceScene.cs` | 104 | 8. Observation Window: drifting stars, nebula, planet, console lights. Layer: Ship hum. |
| `AquariumScene.cs` | 136 | 9. Aquarium: fish, plants, bubbles, light shafts. Layers: Bubbles, Underwater hum. |
| `CityScene.cs` | 119 | 10. City Window: rainy skyline with lit windows, neon, cars, plant. Layers: Rain, Wind. |
| `CampfireScene.cs` | 95 | 11. Campfire under the stars: fire, ring of stones, logs, tent, pines, Milky Way. Layers: Campfire, Crickets, Night breeze. Events: log pop, owl. |
| `HotSpringScene.cs` | 135 | 12. Hot Spring in the snow: steaming pool ringed with snowy rocks, stone lanterns, mountains, snowfall. Layers: Spring water, Winter wind. Events: owl, chimes. |
| `AtticScene.cs` | 130 | 13. Attic Skylight: rafters, rain on a roof window, dust in the light, trunk, lamp, a dripping leak into a bucket. Layers: Rain on the roof (`RoofRain`), Distant thunder, Wind. Event: distant thunder. |
| `KoiPondScene.cs` | 175 | 14. Koi Pond: top-down pond, six koi, lily pads, petals, ripples, lantern light. Layers: Pond water, Crickets, Breeze. Events: chimes, owl. |
| `RainWindow.cs` | 74 | Shared rain-on-glass helper: falling streaks and drops that slide on the glass. Used by the library, city and tent scenes. |

## Assets (`Assets/`)

- Shader: `lofi.fs` (GLSL 330 fragment shader, the lo-fi look; strengths are constants at the top).
- Font: `Lora.ttf`.
- Recordings loaded by `AmbientAudio`: `fire.mp3`, `rain.ogg`, `wind.mp3`, `crickets.mp3`, `forest.mp3`, `clock.wav`, `tent_rain.mp3` (rain heard inside a real tent), `roof_rain.mp3` (rain on a metal roof), `grandfather.wav` (slowed tick), `clockstrike.mp3` (12 strikes; the hour chime plays the first N), `thunder.ogg`, `waves.mp3`, `train.ogg`, `hum.mp3`, `bubbles.wav`, `underwater.mp3`, `purr.ogg`, `water.mp3`, and the event clips `chimes.ogg` and `gull.wav`. Raylib here cannot decode FLAC, so use wav, ogg or mp3.
- `CREDITS.md` records where each file came from.

## Docs and project files

| File | What it does |
|---|---|
| `CLAUDE.md` | Working rules, commands, architecture notes. Read by Claude Code at session start. |
| `AGENTS.md` | Extra notes for other coding agents. |
| `README.md` | Short run and controls guide for a person. |
| `claude/file-map.md` | This file. |
| `claude/session-log.md` | One row per session. |
| `claude/vision.md` | What the app is and does, with status tags. |
| `claude/next-session-plan.md` | Build sketches, patterns, asset sources and backlog. |
| `claude/qa-plan.md` | Smoke test, full test, bug log, code-health check. |
| `claude/instructions-for-claude.md` | Prompt used with "Instructions:" (guarding against codebase decay). |
| `.gitignore` | Ignores `bin/`, `obj/`, `.vs/`, `*.user`, `dist/`. |

## Where to find things

- Loudness of each real recording: the `Load(...)` calls at the top of the `AmbientAudio` constructor (second argument is the target RMS level; Clock uses a peak level). Loop seam smoothness: the third argument.
- Default volume of a sound in a scene: that scene's `SoundLayers`. Noise layers: `Scene.Layers`; their loudness: `Next()` in `Audio.cs`.
- Overall volume: `Settings.Master`, changed with Up/Down or the Master slider.
- Flame size, speed, color, brightness: `Fire` in `Gfx.cs`.
- Edge darkening: `Gfx.Vignette`. Lo-fi strength (palette steps, bloom, grain, scanlines, tint): `Assets/lofi.fs`.
- Lightning and thunder timing: `LibraryScene` (`nextFlash`) and `AmbientAudio.TriggerThunder` / `LoadThunder`.
- Sleep timer choices (to 10 h): `timerChoices` in `Program.cs`; "stop at" presets: `stopPresets`; night drift length: `NightSecs`; fade length (5 min, or the whole timer if shorter): `fadeSecs`. Dim levels: `dimLevels`. Auto-rotate choices: `rotateChoices`.
- Scene switch speeds: `RequestScene` (manual 0.25 s out and 0.7 s in; auto-rotate 2 s and 2.5 s). Audio crossfade speed: `AmbientAudio.Smooth`.
- Random sound events: each scene's `Events` (sound, shortest and longest wait); loudness of each sound: `ClipGain` in `Audio.cs`; the countdown loop is in `Program.cs` (`evLeft`).
- Auto-rotate favorites and shuffle: `Settings.RotateScenes`, `RotateShuffle`, `NextRotateScene` in `Program.cs`.
- Panel layout and look: `ControlPanel.Draw` in `Controls.cs`. Panel and help visibility time (3 s then a 1 s fade): `idle` in `Program.cs`.
- Hiding the panel: `Settings.PanelHidden`, key P and the Hide button; the reminder text is in `Program.cs` near `ControlPanel.Draw`.
- Font and its size: `Assets/Lora.ttf`, `UiText.cs`; text sizes are set in `Controls.cs` and `DrawHelp`.
- Where settings are saved: `Settings.PathFile` in `Program.cs`.
