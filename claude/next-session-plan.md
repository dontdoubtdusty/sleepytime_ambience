# Sleepytime Ambience: Plan

**One home per fact.** `vision.md` holds the design (what the app is and does). This doc holds, for open steps only, the build sketch, the questions to ask and the checks, plus the code patterns, asset sources and backlog. When a step is built, cut its section down to what later steps need.

**Session start:** `file-map.md`, `session-log.md`, `vision.md`, then this plan.

---

## 1. Goal for this stretch

Everything planned for the first big stretch is built (see below). Next: look at it running, fix and tune. What is built is in `vision.md` ([Built] tags).

---

## 2. Patterns to follow in the code

**Scene coordinates:** everything in a scene is drawn in the 640x360 space (`Gfx.W`, `Gfx.H`). Do not read the window size inside a scene.

**Light is additive:** draw the lit objects normally, then `BeginBlendMode(Additive)` and draw `Gfx.Glow` circles over them for light. Every scene ends with `Gfx.Vignette`.

**A new scene** is a `Scene` subclass in its own file under `Scenes/`: `Name`, `SoundLayers` (layer, label, default volume; noise layers are added automatically), `Update(dt)`, `Draw()`. Add it to the scene array in `Program.cs`. Keep particles in plain arrays or lists inside the scene. Reuse `Fire`, `DrawPine`-style helpers and the window-frame drawing; move a helper to `Gfx.cs` the second time it is needed.

**A new sound layer** is: a value in the `Layer` enum, `LayerCount` raised, a `Load(...)` call in the `AmbientAudio` constructor (loudness target and crossfade), and a branch in `Next()`. The audio thread rule applies: no allocation, no locks, no raylib calls in `Next()`.

**Asset record:** add the file and its source link to `Assets/CREDITS.md`.

**Check looks with `--shot`**, then read the PNG. Judge motion by reading the code; only the owner can judge how it moves and sounds.

---

## 3. Open steps

### Built this stretch (Nov 9), waiting for the owner to see it run

Lo-fi shader, noise layers, auto-rotate, mouse control panel, sleeping cat, six new scenes (train, seaside, tent, observation window, aquarium, city), number keys 1-9 and 0, Lora font. Feedback to expect: shader strength, scene looks, loop repeats. Known rough spots: the planet and the tent's sleeping bag, short loops (waves 12 s, clock 8 s, crickets 11 s).

### Up next

1. Fix whatever the owner finds when running it.
2. Tune the shader and scene looks by eye (`Assets/lofi.fs`).
3. Longer waves recording if the repeat bothers (see section 4).
4. Decide the open questions in `vision.md` (timer end, shuffle, day and night).

### Ideas (not scheduled)
- Auto-rotate that shuffles; a gentle sunrise wake-up; a dim corner clock; day and night drift in the cabin and forest; a phone-sized remote; a pixel font for the HUD.

---

## 4. Recording sources found (OpenGameArt, downloadable with curl, no login)

All recordings in use are listed in `Assets/CREDITS.md`.

Sources used for the new scenes (and one alternative):
- Used: ocean waves (preview ogg of https://opengameart.org/content/beach-ocean-waves). Longer alternative: https://opengameart.org/content/sea-and-river-wave-sounds (`VistulaShort.mp3`, 12 MB, river).
- Train rumble (recorded on a real train trip): https://opengameart.org/content/background-rumble-noise (`background_ambience.ogg`)
- Ship or space hum: https://opengameart.org/content/ambient-spaceship-hums (`ambient_hum.mp3`)
- Bubbles: https://opengameart.org/content/bubble-sound-effects (`bubbles-loop1-amp.wav`)
- Underwater ambience: https://opengameart.org/content/aquaria (`TylerSong3_Normal.mp3`)
- Cat purr: https://opengameart.org/content/cat-purr-meow (`cat_purrsleepy_loop.ogg`)
- Not found yet: gulls, a true tent-rain-on-fabric clip (rain.ogg is used), city traffic hush.

---

## 5. Testing

Smoke test after every change: `qa-plan.md` section 3. Full test before calling a feature done: section 4.

---

## 6. Keeping token use down

Read the file map, then only the files the task needs. Do not re-read scene files you just wrote. Use `--shot` and `--bench` instead of long reasoning about what something looks or sounds like.
