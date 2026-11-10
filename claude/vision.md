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
- [Built] Recordings: fire, rain, wind, crickets, forest, ticking clock, distant thunder, ocean waves, train rumble, ship hum, bubbles, underwater, cat purr.
- [Built] Brown and pink noise layers in every scene, off by default.
- Sound must never stutter. Playback is driven by Windows, not by the video frame rate (see `CLAUDE.md`).
- [Undecided] The waves clip is only about 12 seconds, so it repeats noticeably; the long river-waves file (`sea-and-river-wave-sounds` on OpenGameArt, 12 MB) is an alternative if that bothers the owner. The 8 s clock and 11 s crickets loops also repeat.

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
- The owner has not seen the new scenes run yet; several were only checked in still screenshots (the planet in the observation window and the sleeping bag in the tent look the roughest).

## Controls and sleep features
- [Built] Mouse: a panel appears when the mouse moves with scene buttons, a master slider, one slider per sound layer, and buttons for timer, dim, auto-rotate, lo-fi, fullscreen and quit. It fades out after a few seconds, or switch it off with the Hide button or P (a faint "P: show controls" reminder appears when the mouse moves).
- [Built] Keyboard: 1-9 and 0 and Tab for scenes, Up/Down master volume, Left/Right choose a layer and PgUp/PgDn change it, T sleep timer, D dim, R auto-rotate, F fullscreen, L lo-fi, P hide or show the panel, H help, Esc quit.
- [Built] Sleep timer (15 to 180 minutes) fades the picture and sound over the last 5 minutes, then quits the app.
- [Built] Auto-rotate (off, 5, 10, 20, 30, 60 minutes): fades to black, switches to the next scene and fades back in slowly, with a slow sound crossfade. Order is scene order.
- [Built] Dim levels, fullscreen, hidden cursor and panel after 3 seconds, saved settings (including the last scene), Windows kept awake while it runs.

## Open decisions
- [Undecided] What happens when the sleep timer ends: today the app quits. Alternatives: black screen with the app still open, or put the PC to sleep.
- [Undecided] Whether auto-rotate should shuffle instead of following scene order, and whether picking a scene by hand should restart its interval (today it does).
- [Undecided] An app icon and a version number (not needed until we share a build with anyone, which is not planned).
- [Undecided] Whether the day/night cycle (cabin and forest shifting through the night) is wanted.
