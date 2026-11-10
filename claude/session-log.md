# Session Log

Session count: 1.

**Writing this log:** one row per session, one or two lines: what was built or decided, anything left open. Design decisions go in `vision.md`, tuning pointers in the file map's "Where to find things".

**Open bugs:** none known (`qa-plan.md` section 6). I (Claude) cannot hear or see the app running; the owner's report is the test for sound and looks.

**Committed:** the first working app (commit `5d5fac5`, four scenes, real recordings, NAudio). Everything after that is uncommitted until the owner says so.

**Workspace tooling:** on this PC compile with `dotnet build`. `--shot` screenshots work here (the PC has a GPU and a window can open). No ffmpeg is installed, so recordings are bundled as downloaded. Web: WebSearch, then `curl` on OpenGameArt pages works without logging in; Freesound needs a login, so skip it.

---

## Sessions

| # | Date | What happened |
|---|---|---|
| 1 | 2026-11-09 | Built the app from scratch: Raylib-cs scenes in a 640x360 texture (fireplace, snowy cabin, night forest, rainy library), sleep timer with fade, dim, fullscreen, saved settings, keep-awake. First audio was synthesized through raylib audio streams and stuttered constantly: polling the stream from the render loop could not keep up (a worker thread and a bigger buffer did not fix it). Rewrote sound on NAudio/WASAPI (Windows pulls samples; slowest callback measured 2.25 ms against a 200 ms cushion) and replaced every synthesized sound with a real recording from OpenGameArt; synth stays as a fallback. Published a Release build to `dist/` with a Desktop shortcut. Committed and pushed. Created this `claude/` folder from the birb-box pattern. Then built, in order: the lo-fi shader (`Assets/lofi.fs`; fixed a cyan flare on bright spots by clamping before the contrast curve), brown and pink noise layers, auto-rotate (R key; verified with a 4-second test interval: it moved Cabin, Forest, Library on schedule), the clickable control panel (`Controls.cs`), a sleeping cat in the fireplace scene, six new scenes (train, seaside, tent, observation window, aquarium, city), number keys 1-9 and 0, and the Lora font (`UiText.cs`). Split `Scenes.cs` into one file per scene under `Scenes/` and moved rain-on-glass into a shared `RainWindow`. New recordings from OpenGameArt: waves, train, ship hum, bubbles, underwater, cat purr. Found that this raylib cannot decode FLAC (waves had to use the ogg preview, so that loop is only ~12 s). Fixed the saved scene never being written. Checked by build, `--bench` (all recordings load), `--shot` of every scene with the panel, a live 12 s run (slowest audio callback 4 ms). The owner has not yet seen the new scenes run. Republished `dist/`. Not committed since the first push. |

## Decisions and why

- **Panel drawn in window resolution, after the shader.** Text and sliders stay sharp and unaffected by the lo-fi look. Rejected: drawing the UI into the 640x360 scene (blurry text).
- **Scene switch fades to black, then switches.** Simple and never shows a half-loaded scene; auto-rotate uses longer fades. Rejected for now: a true picture crossfade (needs two scene textures).
- **Lora for text.** Warm serif that stays legible at small sizes; Cormorant Garamond was considered and rejected as too thin at 16 px.

- **NAudio over raylib audio streams.** Rejected: raylib `UpdateAudioStream` polled each frame (stuttered), then polled from a worker thread (still stuttered for the owner). Chosen: WASAPI pull model, so the sound card asks for samples and a slow video frame cannot starve it. Revisit only if NAudio causes trouble on another machine.
- **Real recordings, synth as fallback.** The owner heard the synthesized fire and wanted real sound. Revisit if file size becomes a problem (the fire file alone is 10 MB; no ffmpeg here to shrink it).
- **Scenes drawn in code, not video.** The original problem is videos too large to download and streams that drop. Drawing live costs nothing to store and can run all night.
- **Fixed 640x360 scene space.** Cheap to draw, gives a soft low-resolution look, and scene code never deals with window size.
- **Licenses do not matter.** The app is personal and never released (owner, Nov 9).
- **Commit only when asked.** Differs from birb-box, where the owner commits through GitHub Desktop.
