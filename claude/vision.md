# Sleepytime Ambience Vision

Status tags: [Built] [In progress] [Planned] [Undecided]. This doc describes the app, not the builds: when something is built, change its tag and keep the description current. It is the one home for design; build sketches, asset sources and checks are in `next-session-plan.md`.

## The concept
A cozy ambient environment to fall asleep to, running on the owner's Windows PC. It replaces long ambient videos (a fireplace, a snowy cabin window, forest sounds, rain in a library) that stop when the internet drops overnight. Everything is stored locally and drawn live, so it never stalls and never needs a connection. Personal use only; it will not be released.

## The look
- [Built] Scenes drawn in code in a 640x360 space, scaled up, with warm light, glows and a dark vignette.
- [Built] A lo-fi post-process shader (`Assets/lofi.fs`): limited warm palette with ordered dithering, soft bloom, slight colour fringing, faint animated grain and scanlines, warm grading. L toggles it. Cozy and low-res, not realistic.
- [Built] Text in Lora (a warm serif).
- Rules: nothing flashes or moves fast; the room it is watched in is dark, so highlights stay gentle; dim mode must be able to go very low.

## The sound
- [Built] One looping real recording per sound layer, mixed live, each with its own volume, plus a master volume. Loops are crossfaded so they have no seam.
- [Built] Recordings: fire, rain, wind, crickets, forest, ticking clock, distant thunder, ocean waves, train rumble, ship hum, bubbles, underwater, cat purr, running water.
- [Built] Short loops (waves, crickets, wind, underwater, train, water, rain) play as two copies, half a loop apart and at slightly different speeds, so the repeat is hard to notice. The clock is not layered (a second tick rhythm would sound wrong).
- [Built] Random sound events per scene: owl hoots, wind chimes, gulls, log pops, distant thunder, each after a random wait. Switch off with E or the panel.
- [Built] Night drift: over 2.5 hours the picture dims (up to 55% extra) and the volume falls to 40%, gently; separate from the sleep timer. Toggle with N.
- Owl and pop are synthesized (no CC0 owl hoot found); the chimes and gull are recordings.
- [Built] Brown and pink noise layers in every scene, off by default.
- Sound must never stutter. Playback is driven by Windows, not by the video frame rate (see `CLAUDE.md`).
- [Undecided] The waves clip is still only about 12 seconds (now layered). A long river-waves file (`sea-and-river-wave-sounds`, 12 MB, a river not the sea) is an alternative if the layering is not enough.

## Scenes
- [Built] 1. Fireplace, now with a sleeping cat that breathes and twitches an ear.
- [Built] 2. Snowy Cabin.
- [Built] 3. Night Forest.
- [Built] 4. Rainy Library, with lightning and delayed thunder, and a desk clock showing the real local time (the ticking sound is not synced to its second hand).
- [Built] 5. Night Train: a window onto passing hills, village lights and poles, with a gentle sway.
- [Built] 6. Seaside Lighthouse: waves, moonlight glitter, gulls, a slowly sweeping beam.
- [Built] 7. Tent in the Rain: lantern light on the canvas, a wet forest through the flap.
- [Built] 8. Observation Window: drifting stars, a planet, console lights.
- [Built] 9. Aquarium: fish, plants, bubbles.
- [Built] 10. City Window: a rainy skyline with lit windows, neon and cars.
- [Built] 11. Campfire under the stars.
- [Built] 12. Hot Spring in the snow: steam, lanterns, falling snow.
- [Built] 13. Attic Skylight: rain on the roof window, a drip into a bucket, distant thunder.
- [Built] 14. Koi Pond: top-down, six koi, lily pads, ripples.
- The owner has not seen the newest scenes (the earlier six and 11-14) run yet; several were only checked in still screenshots (the planet in the observation window and the sleeping bag in the tent look the roughest).

## Controls and sleep features
- [Built] Mouse: a panel appears when the mouse moves with scene buttons, a master slider, one slider per sound layer, and buttons for timer, dim, auto-rotate, lo-fi, fullscreen and quit. It fades out after a few seconds, or switch it off with the Hide button or P (a faint "P: show controls" reminder appears when the mouse moves).
- [Built] Keyboard: 1-9, 0 and F1-F4 and Tab for scenes, Up/Down master volume, Left/Right choose a layer and PgUp/PgDn change it, T sleep timer, U stop at a clock time, D dim, R auto-rotate, V add or remove this scene from rotation, S shuffle, E sound events, N night drift, F fullscreen, L lo-fi, P hide or show the panel, H help, Esc quit.
- [Built] Sleep timer (15 minutes to 10 hours), or "stop at" a clock time (presets 22:00 to 09:00, right-click for +30 min), fades the picture and sound over the last 5 minutes, then quits the app. Decided: quit stays the ending; black screen or PC sleep were rejected for now.
- [Built] Auto-rotate (off, 5, 10, 20, 30, 60 minutes): fades to black, switches to the next scene and fades back in slowly, with a slow sound crossfade. Order is scene order or shuffled; scenes can be left out of the rotation (right-click a scene button, or V), and markers appear once some are left out.
- [Built] Dim levels, fullscreen, hidden cursor and panel after 3 seconds, saved settings (including the last scene), Windows kept awake while it runs.

## Open decisions
- [Undecided] Whether picking a scene by hand should restart its interval (today it does).
- [Built] App icon (`toolsmake-icon.ps1`) and version 1.0, shared with one friend as a zip on GitHub Releases.
- [Parked] Asset licenses and attribution. Personal project, one friend, never sold; revisit only if that changes. Do not raise it before then.
- [Undecided] Whether the day/night cycle (cabin and forest shifting through the night) is wanted.
