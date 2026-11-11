# Sleepytime Ambience QA Action Plan

The goal: every change is small, tested, and saved, so we can always step back to the last version that worked. Section numbers stay fixed because other docs point at them (section 3 is the smoke test, section 7 the code-health check).

---

## 3. Smoke test (about 2 minutes, after every change)

What Claude can check without ears or eyes is marked (C). The rest only the owner can judge.

- [ ] (C) `dotnet build -c Release` succeeds with no errors
- [ ] (C) `--bench` runs, reports speed far above real time, and every layer says "recording" (not "synth fallback")
- [ ] (C) `--shot` for each scene writes a PNG, and the picture looks right (no missing pieces, nothing off-screen)
- [ ] (C) The app starts and stays up for 10+ seconds when run from bash; with `SLEEPY_AUDIO_STATS=1` and a normal close, the slowest audio callback is well under 50 ms
- [ ] Launches with no error and the last scene, volumes, dim and fullscreen come back
- [ ] Sound plays with no stutter, clicks or dropouts, in every scene, for a few minutes
- [ ] Keys 1-9, 0, F1-F4 and Tab switch scenes with a fade to black and back, and the sound crossfades
- [ ] Up/Down master volume, Left/Right choose a layer, PgUp/PgDn change it; the HUD shows the change and it is remembered after a restart
- [ ] T cycles the sleep timer (off up to 10 h); with a short timer the picture and sound fade out and the app closes
- [ ] U (or the Stop at button) sets a clock time; the countdown matches the time left until then; right-click on the button adds 30 min
- [ ] Right-click a scene button (or V) removes it from auto-rotate and markers appear; S shuffles; rotation only visits the marked scenes
- [ ] Sound events (E): owl, chimes, gull, log pops and distant thunder play now and then at a gentle level and never startle
- [ ] Night drift (N): after a while the picture is dimmer and quieter; turning it off restores both
- [ ] No short loop (waves, crickets, water, wind, rain) sounds like an obvious repeat over five minutes
- [ ] D dims in steps and stays comfortable in a dark room
- [ ] F and F11 toggle borderless fullscreen; the cursor and the HUD hide after 3 seconds and come back when the mouse moves
- [ ] H shows and hides help; Esc quits
- [ ] Moving the mouse shows the panel; every button and slider in it works, dragging a slider changes the sound live, and the values survive a restart
- [ ] R cycles auto-rotate; with it on the scenes change by themselves with slow fades (try `SLEEPY_ROTATE_SECS=4`)
- [ ] Each new scene (train, seaside, tent, observation window, aquarium, city, campfire, hot spring, attic, koi pond) has moving parts, sensible default volumes and no layer far louder than the rest
- [ ] The Rainy Library flashes now and then and thunder follows a moment later
- [ ] Windows does not turn off the screen or sleep while it runs

## 4. Full test (before calling a feature done)

- Everything in section 3, in every scene.
- Leave it running overnight-length (a few hours) at a low dim level: no growth in memory, no stutter, no loop that sounds like a repeat, no flicker.
- Switch scenes 20 times quickly: no crash, no stuck silence, no stuck black.
- Close the app mid-fade: next launch is normal.
- New scenes: each scene's layers appear in the HUD with sensible default volumes; no layer is far louder than the others at defaults.

## 6. Bug log

### Open bugs
None known.

### Fixed
- Constant audio stutter (Nov 9): raylib audio stream polled from the render loop, then from a worker thread, could not keep up. Replaced with NAudio/WASAPI pull playback.
- Gray haze at screen edges (Nov 9): blended drawing lowered the render texture's alpha. Fixed by forcing alpha opaque after the scene draws (`Program.cs`).
- Fireplace flames too small and dim (Nov 9): `Fire` spawn rate, size and brightness raised.

## 7. Code-health check (the duplication and dead-code pass)

Every 5 features, or when asked. Report only; the owner decides what to merge. Watch list:
- `Scenes.cs`: split to one file per scene before 800 lines.
- Repeated window-frame, pine-tree and book drawing code across scenes: move to `Gfx.cs` once a second scene needs it.
- `Program.cs`: input, HUD and the mouse panel should not all live in the main file; the controls panel goes in its own file.
- Layer volumes or loudness targets copied between scenes by hand.
