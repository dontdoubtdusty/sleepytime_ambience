# Drowse: Plan

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

### Backlog: "personality" wave (owner, Nov 11; nothing built yet, do when there is usage to spare)
Order the owner gave: items 1 to 5 together, then the efficiency pass (6), then fixes (7), then the rename (8). Ask a question round before each item (clickable choices, per CLAUDE.md); the owner wants questions throughout.
1. **One randomized element per scene, chosen at each launch.** Simple and visible: tent colour, what the planet looks like, and so on. One predetermined element per scene, picked from a short list, held for the whole run.
2. **One rare event per scene.** Not in your face, a nice surprise now and then. Plugs into the existing sound `Events` countdown in `Program.cs` where it has a sound, otherwise a visual-only timer in the scene.
   - Built (3a): campfire shooting star, space comet, hot spring aurora, seaside ship lights, city fireworks, aquarium turtle, koi dragonfly. 3b also built: fireplace log and sparks, cabin fox, forest rabbit, library moth, train station with clock, tent raccoon, attic bat. All animals are drawn through `Sprites.cs`. Timer: `Scene.Step` in `Scenes/Scene.cs` (an event every 7 to 23 minutes, first after 2.5 to 8). Test: `SLEEPY_RARE_SECS=n` and `SLEEPY_RARE_SPEED=k`.
3. **Zen loading screen with a calming quote.** Picking the randomized elements is near-instant, so no progress bar: a short breathing-style animation (for example ink or ripples settling) with one quote, eased in and out, a few seconds at most. Questions to ask: quote source (a short written list vs. none), length on screen, whether it also plays at each scene switch.
4. **More clocks, one fitting each scene that suits one.** The clock must match the scene (grandfather clock in the fireplace scene, desk clock stays in the library, and so on). Needs a new grandfather-clock tick sound with a soft, subtle hourly chime (real recording, add to `Assets/CREDITS.md`). Clocks are always present; real time is an option. With real time off, each clock starts at a random time at launch and then runs in real time, so the viewer never has to see the actual time. The library clock currently always shows real time, so it changes too. Questions to ask: which scenes get a clock, whether the chime follows the displayed time, one shared option for all clocks.
5. **Holiday themes, subtle.** Small touches on the day itself (Halloween, Thanksgiving, Christmas and similar), for example a carved pumpkin on the cabin sill or lights on a tree. Questions to ask: which holidays, whether to add an override to preview a date, whether a theme replaces the launch-time random element or adds to it. Needs a date check at launch only.
6. **Efficiency pass** after 1 to 5 (report first, owner decides merges, per CLAUDE.md). Also check the loading screen and new clock sounds against the audio-thread rules.
7. **Scene fixes:**
   - Fireplace cat reads as a small dinosaur: lean all the way into a cat-like little dinosaur (owner approves).
   - Koi do not look like koi: redraw (body shape, fins, tail, orange/white/black patches).
   - Night train looks empty inside: add passengers or seat silhouettes and warm light so the carriage looks lived in.
   - Seaside waves (owner, Nov 11, "least favorite scene"): the waves clip is about 12 s, so it repeats audibly and is not relaxing. Find a much longer, softer sea recording (or build the loop from several recordings), then re-check with the layering. Do right after step 2.
8. **Rename the app. DONE (Nov 11): the app is now Drowse** (title, assembly, settings folder with migration from the old one, Desktop shortcut, release script). Still open: the GitHub repo and local folder are still `sleepytime_ambience`; old 1.1.1 copies cannot self-update into Drowse (they look for the old exe name), so friends need one manual download. Original notes:  Brainstorm with the owner: relaxing theme, impactful. Round 1 (Nov 11): tone chosen "soft and sleepy"; shortlist was Hushlight, Lullwater, Drowse, Nightlull, Slumberlight, Softhour; owner's favorite is **Drowse** (not final). Still to do: check the name is free (GitHub, web), decide how it appears in the title and icon, then rename. A rename touches the window title, assembly name (`Drowse`), the Desktop shortcut, the settings folder (`%APPDATA%\Drowse`, which would orphan saved settings unless migrated), the GitHub repo, the in-app updater and the release zip name.

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
