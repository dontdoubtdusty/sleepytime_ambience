using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Raylib_cs;

// Sleepytime Ambience: procedural, fully offline ambient scenes.
// Dev helper:  SleepytimeAmbience --shot <sceneIndex> <out.png>   renders a few seconds and saves a screenshot.

string? shotPath = null;
int shotScene = 0;
for (int i = 0; i < args.Length; i++)
    if (args[i] == "--shot" && i + 2 < args.Length) { shotScene = int.Parse(args[i + 1]); shotPath = args[i + 2]; }

if (args.Contains("--bench"))
{
    // Dev helper: render 30 s of every layer with no device and report speed.
    var b = new AmbientAudio(openDevice: false);
    foreach (Layer l in Enum.GetValues<Layer>()) b.Target[(int)l] = l == Layer.Rumble ? 0 : 1;
    var buf = new float[48000 * 2];
    var sw0 = System.Diagnostics.Stopwatch.StartNew();
    double sumSq = 0; float peak = 0;
    for (int s = 0; s < 30; s++)
    {
        b.Render(buf);
        foreach (float v in buf) { sumSq += v * v; peak = MathF.Max(peak, MathF.Abs(v)); }
    }
    Console.WriteLine($"rendered 30 s of audio in {sw0.ElapsedMilliseconds} ms; rms {Math.Sqrt(sumSq / (30.0 * buf.Length)):F3}, peak {peak:F3}");
    foreach (Layer l in Enum.GetValues<Layer>()) Console.WriteLine($"  {l}: {(b.HasRecording(l) ? "recording" : "synth fallback")}");
    // Random events: play each alone (all layers silent) and report its loudest sample.
    Array.Clear(b.Target);
    b.Smooth = 0.01f; for (int s = 0; s < 3; s++) b.Render(buf); // let the layers fade out first
    foreach (Evt e in new[] { Evt.Chimes, Evt.Gull, Evt.Owl, Evt.Pop })
    {
        b.PlayEvent(e);
        float ep = 0;
        for (int s = 0; s < 4; s++) { b.Render(buf); foreach (float v in buf) ep = MathF.Max(ep, MathF.Abs(v)); }
        Console.WriteLine($"  event {e}: peak {ep:F3}");
    }
    return;
}

var settings = Settings.Load();
if (shotPath != null) { settings.Scene = shotScene; settings.TimerMinutes = 0; settings.StopAtMinutes = -1; settings.Fullscreen = false; }

Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
// App version, shown in the window title; tools\make-release.ps1 reads it to name the zip.
const string Version = "1.0";
Raylib.InitWindow(1280, 720, $"Sleepytime Ambience {Version}");
// Window and taskbar icon (the exe's own icon comes from icon.ico in the project file).
string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.png");
if (File.Exists(iconPath)) { var icon = Raylib.LoadImage(iconPath); Raylib.SetWindowIcon(icon); Raylib.UnloadImage(icon); }
Raylib.SetTargetFPS(60);
UiText.Load();


var audio = new AmbientAudio(openDevice: shotPath == null);
Scene[] scenes =
{
    new FireplaceScene(), new CabinScene(), new ForestScene(), new LibraryScene(audio), new TrainScene(),
    new SeasideScene(), new TentScene(), new SpaceScene(), new AquariumScene(), new CityScene(),
    new CampfireScene(), new HotSpringScene(), new AtticScene(), new KoiPondScene(),
};
KeyboardKey[] sceneKeys =
{
    KeyboardKey.One, KeyboardKey.Two, KeyboardKey.Three, KeyboardKey.Four, KeyboardKey.Five,
    KeyboardKey.Six, KeyboardKey.Seven, KeyboardKey.Eight, KeyboardKey.Nine, KeyboardKey.Zero,
    KeyboardKey.F1, KeyboardKey.F2, KeyboardKey.F3, KeyboardKey.F4,
};
int sceneIdx = Math.Clamp(settings.Scene, 0, scenes.Length - 1);
int layerSel = 0;

var target = Raylib.LoadRenderTexture(Gfx.W, Gfx.H);
Raylib.SetTextureFilter(target.Texture, TextureFilter.Bilinear);

// Lo-fi look: a fragment shader applied when the scene picture is drawn to the window.
var lofi = Raylib.LoadShader(null, Path.Combine(AppContext.BaseDirectory, "Assets", "lofi.fs"));
bool lofiOk = Raylib.IsShaderValid(lofi);
int lofiTime = Raylib.GetShaderLocation(lofi, "time");
if (lofiOk) Raylib.SetShaderValue(lofi, Raylib.GetShaderLocation(lofi, "texSize"), new Vector2(Gfx.W, Gfx.H), ShaderUniformDataType.Vec2);

if (shotPath == null) PreventSleep(true);
if (settings.Fullscreen && shotPath == null) Raylib.ToggleBorderlessWindowed();

int[] timerChoices = { 0, 15, 30, 45, 60, 90, 120, 180, 240, 300, 360, 420, 480, 540, 600 }; // minutes, up to 10 hours
int[] stopPresets = { 22 * 60, 23 * 60, 0, 60, 120, 180, 240, 300, 360, 420, 480, 540 }; // "stop at" clock times, evening to morning
float[] dimLevels = { 0f, 0.3f, 0.55f, 0.75f, 0.88f };
float timerRemaining = 0, timerTotal = 0;
float nightT = 0; // seconds since night drift was switched on
const float NightSecs = 9000f; // night drift takes 2.5 hours to reach its full effect
float[] evLeft = Array.Empty<float>(); // seconds until each random sound event of the current scene
float idle = 0, sceneFade = 1, helpOn = 0;
bool showHelp = false, quit = false;

// Scene changes fade to black, switch, then fade back in. Auto-rotate uses slower fades.
int pendingScene = -1;
float fadeTarget = 0, fadeOutSpeed = 1 / 0.25f, fadeInSpeed = 1 / 0.7f;
int[] rotateChoices = { 0, 5, 10, 20, 30, 60 };
// SLEEPY_ROTATE_SECS (test only) replaces the interval, in seconds, so auto-rotate can be tried quickly.
float RotateSeconds() => float.TryParse(Environment.GetEnvironmentVariable("SLEEPY_ROTATE_SECS"), out var rs) ? rs : settings.AutoRotateMinutes * 60f;
float rotateLeft = RotateSeconds();
Vector2 lastMouse = Raylib.GetMousePosition();
int shotFrames = 0;
// F12 screenshot: a save waiting for the next frame, and the "saved" message (seconds left and text).
bool saveShot = false;
float shotMsg = 0;
string shotMsgText = "";

float[] Vols() => settings.GetVolumes(scenes[sceneIdx]);
void ApplyScene()
{
    var sc = scenes[sceneIdx];
    var v = settings.GetVolumes(sc);
    Array.Clear(audio.Target);
    for (int i = 0; i < sc.Layers.Length; i++) audio.Target[(int)sc.Layers[i].Layer] = v[i];
    layerSel = Math.Clamp(layerSel, 0, sc.Layers.Length - 1);
}
ApplyScene();
ResetEvents();
StartTimer();

// Starts a fade to another scene (the switch happens when the screen is black).
void RequestScene(int n, bool slow)
{
    rotateLeft = RotateSeconds();
    n = (n % scenes.Length + scenes.Length) % scenes.Length;
    if (n == sceneIdx && pendingScene < 0) return;
    pendingScene = n;
    fadeTarget = 1;
    fadeOutSpeed = slow ? 1f / 2f : 1f / 0.25f;
    fadeInSpeed = slow ? 1f / 2.5f : 1f / 0.7f;
    audio.Smooth = slow ? 0.00002f : 0.00008f;
}

// Seconds from now until the next time the wall clock reads the given minute of the day.
float SecondsUntil(int minuteOfDay)
{
    var now = DateTime.Now;
    var at = now.Date.AddMinutes(minuteOfDay);
    if (at <= now) at = at.AddDays(1);
    return (float)(at - now).TotalSeconds;
}

// (Re)starts the sleep timer from the settings: either "stop at HH:MM" or a number of minutes.
void StartTimer()
{
    timerRemaining = settings.StopAtMinutes >= 0 ? SecondsUntil(settings.StopAtMinutes) : settings.TimerMinutes * 60f;
    timerTotal = timerRemaining;
}

// Cycles the timer length (off up to 10 hours). Picking a length switches "stop at" off.
void CycleTimer()
{
    int i = Array.IndexOf(timerChoices, settings.TimerMinutes);
    settings.TimerMinutes = timerChoices[(i + 1) % timerChoices.Length];
    settings.StopAtMinutes = -1;
    StartTimer();
    settings.Save();
}

// Steps "stop at" through the evening-to-morning presets, then off.
void CycleStopAt()
{
    static int Ord(int m) => (m + 720) % 1440; // noon = 0, so the night sorts in order
    int next = -1;
    if (settings.StopAtMinutes < 0) next = stopPresets[0];
    else foreach (int p in stopPresets) if (Ord(p) > Ord(settings.StopAtMinutes)) { next = p; break; }
    settings.StopAtMinutes = next;
    StartTimer();
    settings.Save();
}

// Moves "stop at" on by 30 minutes (starts at 06:00 if it was off).
void NudgeStopAt()
{
    settings.StopAtMinutes = settings.StopAtMinutes < 0 ? 360 : (settings.StopAtMinutes + 30) % 1440;
    StartTimer();
    settings.Save();
}

// Which scenes auto-rotate uses: the picked favorites, or every scene when none are picked.
bool InRotation(int i) => settings.RotateScenes.Count == 0 || settings.RotateScenes.Contains(scenes[i].Name);
void ToggleRotation(int i)
{
    var set = Enumerable.Range(0, scenes.Length).Where(InRotation).Select(n => scenes[n].Name).ToList();
    if (set.Contains(scenes[i].Name)) { if (set.Count > 1) set.Remove(scenes[i].Name); }
    else set.Add(scenes[i].Name);
    settings.RotateScenes = set.Count == scenes.Length ? new List<string>() : set;
    settings.Save();
}

// The scene auto-rotate goes to next: the following favorite in scene order, or a random other one with shuffle on.
int NextRotateScene()
{
    var ids = Enumerable.Range(0, scenes.Length).Where(InRotation).ToList();
    int cur = pendingScene >= 0 ? pendingScene : sceneIdx;
    if (settings.RotateShuffle && ids.Count > 1)
    {
        int n;
        do n = ids[Random.Shared.Next(ids.Count)]; while (n == cur);
        return n;
    }
    foreach (int i in ids) if (i > cur) return i;
    return ids[0];
}

// Starts the random-event countdowns for the current scene.
void ResetEvents() => evLeft = scenes[sceneIdx].Events.Select(e => Gfx.Rf(e.MinSecs * 0.3f, e.MaxSecs)).ToArray();

void ToggleShuffle() { settings.RotateShuffle = !settings.RotateShuffle; settings.Save(); }
void ToggleEvents() { settings.EventsOn = !settings.EventsOn; settings.Save(); }
void ToggleDrift() { settings.NightDrift = !settings.NightDrift; nightT = 0; settings.Save(); }
void CycleDim() { settings.Dim = (settings.Dim + 1) % dimLevels.Length; settings.Save(); }
void CycleRotate()
{
    int i = Array.IndexOf(rotateChoices, settings.AutoRotateMinutes);
    settings.AutoRotateMinutes = rotateChoices[(i + 1) % rotateChoices.Length];
    rotateLeft = RotateSeconds();
    settings.Save();
}
void ToggleLofi() { settings.Lofi = !settings.Lofi; settings.Save(); }
void ToggleFullscreen()
{
    Raylib.ToggleBorderlessWindowed();
    settings.Fullscreen = Raylib.IsWindowState(ConfigFlags.UndecoratedWindow) || Raylib.IsWindowFullscreen();
    settings.Save();
}

// What the clickable panel shows and does. Values are refreshed every frame in the loop below.
var panel = new PanelModel
{
    SceneNames = scenes.Select(s => s.Name).ToArray(),
    SelectScene = n => RequestScene(n, false),
    SetMaster = v => settings.Master = v,
    SetLayer = (i, v) => { Vols()[i] = v; ApplyScene(); },
    CycleTimer = CycleTimer, CycleStopAt = CycleStopAt, NudgeStopAt = NudgeStopAt, CycleDim = CycleDim, CycleRotate = CycleRotate,
    ToggleShuffle = ToggleShuffle, ToggleEvents = ToggleEvents, ToggleDrift = ToggleDrift, ToggleRotation = ToggleRotation,
    ToggleLofi = ToggleLofi, ToggleFullscreen = ToggleFullscreen,
    Quit = () => quit = true,
    HidePanel = () => { settings.PanelHidden = true; settings.Save(); },
    Commit = () => settings.Save(),
};

while (!Raylib.WindowShouldClose() && !quit)
{
    float dt = shotPath != null ? 1f / 30f : Math.Min(Raylib.GetFrameTime(), 0.1f);
    bool touched = false;

    // ---- input ----
    for (int k = 0; k < sceneKeys.Length && k < scenes.Length; k++)
        if (Raylib.IsKeyPressed(sceneKeys[k])) { RequestScene(k, false); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.Tab)) { RequestScene((pendingScene >= 0 ? pendingScene : sceneIdx) + 1, false); touched = true; }

    var sc2 = scenes[sceneIdx];
    if (Raylib.IsKeyPressed(KeyboardKey.Left)) { layerSel = (layerSel + sc2.Layers.Length - 1) % sc2.Layers.Length; touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.Right)) { layerSel = (layerSel + 1) % sc2.Layers.Length; touched = true; }

    float step = 0;
    if (KeyStep(KeyboardKey.PageUp) || KeyStep(KeyboardKey.Equal) || KeyStep(KeyboardKey.KpAdd)) step = 0.05f;
    if (KeyStep(KeyboardKey.PageDown) || KeyStep(KeyboardKey.Minus) || KeyStep(KeyboardKey.KpSubtract)) step = -0.05f;
    if (step != 0)
    {
        var v = Vols();
        v[layerSel] = Math.Clamp(MathF.Round((v[layerSel] + step) * 20) / 20, 0, 1);
        ApplyScene(); settings.Save(); touched = true;
    }
    if (KeyStep(KeyboardKey.Up)) { settings.Master = Math.Clamp(MathF.Round((settings.Master + 0.05f) * 20) / 20, 0, 1); settings.Save(); touched = true; }
    if (KeyStep(KeyboardKey.Down)) { settings.Master = Math.Clamp(MathF.Round((settings.Master - 0.05f) * 20) / 20, 0, 1); settings.Save(); touched = true; }

    if (Raylib.IsKeyPressed(KeyboardKey.D)) { CycleDim(); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.T)) { CycleTimer(); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.R)) { CycleRotate(); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.U)) { if (Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift)) NudgeStopAt(); else CycleStopAt(); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.S)) { ToggleShuffle(); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.V)) { ToggleRotation(sceneIdx); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.E)) { ToggleEvents(); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.N)) { ToggleDrift(); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.F) || Raylib.IsKeyPressed(KeyboardKey.F11)) { ToggleFullscreen(); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.L)) { ToggleLofi(); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.H)) { showHelp = !showHelp; touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.P)) { settings.PanelHidden = !settings.PanelHidden; settings.Save(); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.F12)) saveShot = true; // not "touched": the panel should not pop into the picture

    var mouse = Raylib.GetMousePosition();
    if (mouse != lastMouse) { lastMouse = mouse; touched = true; }

    idle = touched ? 0 : idle + dt;
    if (idle < 3f) Raylib.ShowCursor(); else Raylib.HideCursor();

    // ---- sleep timer ----
    float fade = 1;
    if (timerTotal > 0)
    {
        timerRemaining -= dt;
        float fadeSecs = Math.Min(300f, timerTotal);
        fade = Math.Clamp(timerRemaining / fadeSecs, 0, 1);
        fade = fade * fade * (3 - 2 * fade); // smoothstep
        if (timerRemaining <= 0) break;
    }
    // Night drift: over a couple of hours the picture dims and the sound falls, slowly and without a click.
    nightT = settings.NightDrift ? nightT + dt : 0;
    float nd = Math.Clamp(nightT / NightSecs, 0, 1);
    nd = nd * nd * (3 - 2 * nd);
    audio.Master = settings.Master * (1 - 0.6f * nd);
    audio.Fade = fade;
    audio.EventLevel = settings.EventsOn ? 1f : 0f;

    // ---- simulate ----
    scenes[sceneIdx].Update(dt);

    // Random sound events: each counts down and plays its sound, then waits a new random time.
    var evs = scenes[sceneIdx].Events;
    if (settings.EventsOn && sceneFade < 0.5f && pendingScene < 0 && evLeft.Length == evs.Length)
        for (int e = 0; e < evs.Length; e++)
            if ((evLeft[e] -= dt) <= 0) { audio.PlayEvent(evs[e].Kind); evLeft[e] = Gfx.Rf(evs[e].MinSecs, evs[e].MaxSecs); }

    // Auto-rotate: after the chosen number of minutes, drift to the next scene.
    if (settings.AutoRotateMinutes > 0 && pendingScene < 0)
    {
        rotateLeft -= dt;
        if (rotateLeft <= 0) RequestScene(NextRotateScene(), true);
    }
    // Scene fade: out to black, switch at full black, then back in.
    if (sceneFade < fadeTarget) sceneFade = Math.Min(fadeTarget, sceneFade + dt * fadeOutSpeed);
    else if (sceneFade > fadeTarget) sceneFade = Math.Max(fadeTarget, sceneFade - dt * fadeInSpeed);
    if (pendingScene >= 0 && sceneFade >= 1)
    {
        sceneIdx = pendingScene; pendingScene = -1; fadeTarget = 0;
        settings.Scene = sceneIdx; settings.Save();
        if (Environment.GetEnvironmentVariable("SLEEPY_AUDIO_STATS") != null) Console.WriteLine($"SCENE {sceneIdx} {scenes[sceneIdx].Name}");
        ApplyScene();
        ResetEvents();
    }
    helpOn += ((showHelp ? 1 : 0) - helpOn) * Math.Min(1, dt * 8);

    // ---- render scene to low-res target ----
    Raylib.BeginTextureMode(target);
    Raylib.ClearBackground(Color.Black);
    scenes[sceneIdx].Draw();
    ForceOpaque(Gfx.W, Gfx.H);
    Raylib.EndTextureMode();

    if (shotPath != null && ++shotFrames >= 150)
    {
        // Screenshot the final picture (with the shader), not just the raw scene texture.
        var shot = Raylib.LoadRenderTexture(1280, 720);
        Raylib.BeginTextureMode(shot);
        Raylib.ClearBackground(Color.Black);
        DrawShaded(new Rectangle(0, 0, 1280, 720), shotFrames / 30f);
        RefreshPanel();
        ControlPanel.Draw(panel, 1280, 720, 1f);
        ForceOpaque(1280, 720);
        Raylib.EndTextureMode();
        var img = Raylib.LoadImageFromTexture(shot.Texture);
        Raylib.ImageFlipVertical(ref img);
        Raylib.ExportImage(img, shotPath);
        break;
    }

    // ---- present ----
    int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
    float scale = Math.Min(sw / (float)Gfx.W, sh / (float)Gfx.H);
    float dw = Gfx.W * scale, dh = Gfx.H * scale;
    var dest = new Rectangle((sw - dw) / 2, (sh - dh) / 2, dw, dh);
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.Black);
    DrawShaded(dest, (float)Raylib.GetTime());

    float dim = 1 - (1 - dimLevels[(int)settings.Dim]) * (1 - 0.55f * nd);
    if (dim > 0) Raylib.DrawRectangle(0, 0, sw, sh, new Color(10, 5, 0, (int)(dim * 255)));
    float black = Math.Max(1 - fade, sceneFade * sceneFade);
    if (black > 0) Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, (int)(Math.Clamp(black, 0, 1) * 255)));

    // Clickable panel (fades in on input) and the keyboard help overlay.
    float panelAlpha = Math.Clamp(1 - (idle - 3f) / 1f, 0, 1);
    RefreshPanel();
    if (settings.PanelHidden)
    {
        // Panel switched off: only a faint reminder shows when something is touched.
        UiText.Draw("P: show controls", 20 * sh / 720f, 20 * sh / 720f, 15 * sh / 720f, new Color(255, 240, 215, (int)(panelAlpha * 110)));
    }
    else if (ControlPanel.Draw(panel, sw, sh, panelAlpha)) idle = Math.Min(idle, 0.5f);
    DrawHelp(sw, sh);
    // F12: save exactly what is on screen now, before the "saved" message is drawn over it.
    if (saveShot) { saveShot = false; SaveScreenshot(); }
    // The "saved" message: bottom centre, fades out over its last second.
    if (shotMsg > 0)
    {
        shotMsg -= dt;
        float ms = 18 * sh / 720f;
        UiText.Draw(shotMsgText, (sw - UiText.Measure(shotMsgText, ms)) / 2, sh - ms * 3, ms, new Color(255, 240, 215, (int)(Math.Clamp(shotMsg, 0, 1) * 200)));
    }
    // Blending leaves the window partly transparent, which Windows screenshot tools capture as black; make it opaque.
    ForceOpaque(sw, sh);
    Raylib.EndDrawing();
}

if (shotPath == null) { settings.Save(); PreventSleep(false); }
if (Environment.GetEnvironmentVariable("SLEEPY_AUDIO_STATS") != null) Console.WriteLine($"AUDIOSTATS reads={audio.Reads} frames={audio.FramesRead} maxReadMs={audio.MaxReadMs:F2}");
audio.Dispose();
Raylib.UnloadRenderTexture(target);
UiText.Unload();

Raylib.CloseWindow();
return;

// ---------------------------------------------------------------------------

// Sets the alpha of everything drawn so far to fully opaque (blending wears it down).
static void ForceOpaque(int w, int h)
{
    Rlgl.DrawRenderBatchActive();
    Rlgl.ColorMask(false, false, false, true);
    Raylib.DrawRectangle(0, 0, w, h, Color.White);
    Rlgl.DrawRenderBatchActive();
    Rlgl.ColorMask(true, true, true, true);
}

// Saves what is on screen right now as a PNG in Pictures\Sleepytime Ambience, and sets the message to show.
void SaveScreenshot()
{
    try
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Sleepytime Ambience");
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, $"{scenes[sceneIdx].Name} {DateTime.Now:yyyy-MM-dd HH.mm.ss}.png");
        var img = Raylib.LoadImageFromScreen();
        bool ok = Raylib.ExportImage(img, file);
        Raylib.UnloadImage(img);
        shotMsgText = ok ? @"Screenshot saved to Pictures\Sleepytime Ambience" : "Screenshot could not be saved";
    }
    catch (Exception ex) { shotMsgText = "Screenshot could not be saved: " + ex.Message; }
    shotMsg = 3f;
}

// Draws the scene picture into dest, through the lo-fi shader when it is on and compiled.
void DrawShaded(Rectangle dest, float seconds)
{
    bool on = lofiOk && settings.Lofi;
    if (on)
    {
        Raylib.SetShaderValue(lofi, lofiTime, seconds, ShaderUniformDataType.Float);
        Raylib.BeginShaderMode(lofi);
    }
    Raylib.DrawTexturePro(target.Texture, new Rectangle(0, 0, Gfx.W, -Gfx.H), dest, Vector2.Zero, 0, Color.White);
    if (on) Raylib.EndShaderMode();
}

// Formats a time left as minutes, or hours and minutes from one hour up.
static string Hm(float secs)
{
    int m = (int)Math.Ceiling(secs / 60);
    return m >= 60 ? $"{m / 60}h {m % 60:00}m" : $"{m} min";
}

static bool KeyStep(KeyboardKey k) => Raylib.IsKeyPressed(k) || Raylib.IsKeyPressedRepeat(k);

// Copies the app's current values into the panel model (what the panel shows).
void RefreshPanel()
{
    var cs = scenes[sceneIdx];
    var vols = Vols();
    panel.SceneIdx = pendingScene >= 0 ? pendingScene : sceneIdx;
    panel.Master = settings.Master;
    panel.Layers = cs.Layers.Select((l, i) => (l.Label, vols[i])).ToArray();
    panel.LayerSel = layerSel;
    panel.TimerText = timerTotal > 0 ? $"Timer: {Hm(timerRemaining)}" : "Timer: off";
    panel.StopText = settings.StopAtMinutes >= 0 ? $"Stop at: {settings.StopAtMinutes / 60:00}:{settings.StopAtMinutes % 60:00}" : "Stop at: off";
    panel.DimText = $"Dim: {(int)(dimLevels[(int)settings.Dim] * 100)}%";
    panel.RotateText = settings.AutoRotateMinutes > 0 ? $"Rotate: {settings.AutoRotateMinutes} min" : "Rotate: off";
    panel.ShuffleText = "Shuffle: " + (settings.RotateShuffle ? "on" : "off");
    panel.EventsText = "Sound events: " + (settings.EventsOn ? "on" : "off");
    panel.DriftText = "Night drift: " + (settings.NightDrift ? "on" : "off");
    panel.InRotation = Enumerable.Range(0, scenes.Length).Select(InRotation).ToArray();
    panel.LofiOn = settings.Lofi;
}

// Keyboard help overlay (toggled with H).
void DrawHelp(int sw, int sh)
{
    float s = sh / 720f;
    int fs = Math.Max(12, (int)(20 * s)), pad = (int)(20 * s);
    if (helpOn > 0.02f)
    {
        string[] lines =
        {
            "1-9, 0, F1-F4 / Tab   switch scene",
            "Up / Down      master volume",
            "Left / Right   select sound layer",
            "PgUp / PgDn    layer volume (also + / -)",
            "T              sleep timer, up to 10 h (fades out, then quits)",
            "U              stop at a clock time (Shift+U: +30 min)",
            "D              dim screen",
            "R              auto-rotate scenes (off, 5, 10, 20, 30, 60 min)",
            "V              add or remove this scene from auto-rotate",
            "S              shuffle auto-rotate",
            "E              random sound events on / off",
            "N              night drift (slow dim and quieting) on / off",
            "Mouse          move it to show the clickable panel",
            "F / F11        fullscreen",
            "F12            save a screenshot to Pictures",
            "L              lo-fi look on / off",
            "P              hide or show the control panel",
            "H              toggle this help",
            "Esc            quit",
        };
        int hy = sh - pad - (int)(lines.Length * fs * 1.4f);
        int ha = (int)(helpOn * 230);
        foreach (var l in lines)
        {
            UiText.Draw(l, pad, hy, fs, new Color(255, 240, 215, ha));
            hy += (int)(fs * 1.4f);
        }
    }
}

static void PreventSleep(bool on)
{
    const uint ES_CONTINUOUS = 0x80000000, ES_SYSTEM_REQUIRED = 0x1, ES_DISPLAY_REQUIRED = 0x2;
    try { NativeMethods.SetThreadExecutionState(on ? ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED : ES_CONTINUOUS); }
    catch { }
}

static class NativeMethods
{
    [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint flags);
}

class Settings
{
    public int Scene { get; set; }
    public float Master { get; set; } = 0.7f;
    public float Dim { get; set; }
    public int TimerMinutes { get; set; } = 60;
    public bool Fullscreen { get; set; }
    public bool Lofi { get; set; } = true;
    public bool PanelHidden { get; set; }
    public int AutoRotateMinutes { get; set; }
    public int StopAtMinutes { get; set; } = -1; // clock time to stop at, in minutes after midnight (-1 = off)
    public bool RotateShuffle { get; set; }
    public List<string> RotateScenes { get; set; } = new(); // scenes auto-rotate uses (empty = all)
    public bool EventsOn { get; set; } = true;
    public bool NightDrift { get; set; }
    public Dictionary<string, float[]> LayerVolumes { get; set; } = new();

    static string PathFile => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SleepytimeAmbience", "settings.json");

    public float[] GetVolumes(Scene sc)
    {
        if (!LayerVolumes.TryGetValue(sc.Name, out var v) || v.Length != sc.Layers.Length)
            LayerVolumes[sc.Name] = v = sc.Layers.Select(l => l.Default).ToArray();
        return v;
    }

    public static Settings Load()
    {
        try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(PathFile)) ?? new Settings(); }
        catch { return new Settings(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathFile)!);
            File.WriteAllText(PathFile, JsonSerializer.Serialize(this));
        }
        catch { }
    }
}
