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
    return;
}

var settings = Settings.Load();
if (shotPath != null) { settings.Scene = shotScene; settings.TimerMinutes = 0; settings.Fullscreen = false; }

Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
Raylib.InitWindow(1280, 720, "Sleepytime Ambience");
Raylib.SetTargetFPS(60);
UiText.Load();


var audio = new AmbientAudio(openDevice: shotPath == null);
Scene[] scenes =
{
    new FireplaceScene(), new CabinScene(), new ForestScene(), new LibraryScene(audio), new TrainScene(),
    new SeasideScene(), new TentScene(), new SpaceScene(), new AquariumScene(), new CityScene(),
};
KeyboardKey[] sceneKeys =
{
    KeyboardKey.One, KeyboardKey.Two, KeyboardKey.Three, KeyboardKey.Four, KeyboardKey.Five,
    KeyboardKey.Six, KeyboardKey.Seven, KeyboardKey.Eight, KeyboardKey.Nine, KeyboardKey.Zero,
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

int[] timerChoices = { 0, 15, 30, 45, 60, 90, 120, 180 };
float[] dimLevels = { 0f, 0.3f, 0.55f, 0.75f, 0.88f };
float timerRemaining = settings.TimerMinutes * 60f;
float timerTotal = timerRemaining;
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

// Starts a fade to another scene (the switch happens when the screen is black).
void RequestScene(int n, bool slow)
{
    n = (n % scenes.Length + scenes.Length) % scenes.Length;
    if (n == sceneIdx && pendingScene < 0) return;
    pendingScene = n;
    fadeTarget = 1;
    fadeOutSpeed = slow ? 1f / 2f : 1f / 0.25f;
    fadeInSpeed = slow ? 1f / 2.5f : 1f / 0.7f;
    audio.Smooth = slow ? 0.00002f : 0.00008f;
    rotateLeft = RotateSeconds();
}

void CycleTimer()
{
    int i = Array.IndexOf(timerChoices, settings.TimerMinutes);
    settings.TimerMinutes = timerChoices[(i + 1) % timerChoices.Length];
    timerRemaining = timerTotal = settings.TimerMinutes * 60f;
    settings.Save();
}
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
    CycleTimer = CycleTimer, CycleDim = CycleDim, CycleRotate = CycleRotate,
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
    if (Raylib.IsKeyPressed(KeyboardKey.F) || Raylib.IsKeyPressed(KeyboardKey.F11)) { ToggleFullscreen(); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.L)) { ToggleLofi(); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.H)) { showHelp = !showHelp; touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.P)) { settings.PanelHidden = !settings.PanelHidden; settings.Save(); touched = true; }

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
    audio.Master = settings.Master;
    audio.Fade = fade;

    // ---- simulate ----
    scenes[sceneIdx].Update(dt);

    // Auto-rotate: after the chosen number of minutes, drift to the next scene.
    if (settings.AutoRotateMinutes > 0 && pendingScene < 0)
    {
        rotateLeft -= dt;
        if (rotateLeft <= 0) RequestScene(sceneIdx + 1, true);
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
    }
    helpOn += ((showHelp ? 1 : 0) - helpOn) * Math.Min(1, dt * 8);

    // ---- render scene to low-res target ----
    Raylib.BeginTextureMode(target);
    Raylib.ClearBackground(Color.Black);
    scenes[sceneIdx].Draw();
    // Blending (vignette etc.) erodes the alpha channel; force it back to opaque.
    Rlgl.DrawRenderBatchActive();
    Rlgl.ColorMask(false, false, false, true);
    Raylib.DrawRectangle(0, 0, Gfx.W, Gfx.H, Color.White);
    Rlgl.DrawRenderBatchActive();
    Rlgl.ColorMask(true, true, true, true);
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

    float dim = dimLevels[(int)settings.Dim];
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
    panel.TimerText = timerTotal > 0 ? $"Timer: {(int)Math.Ceiling(timerRemaining / 60)} min left" : "Timer: off";
    panel.DimText = $"Dim: {(int)(dimLevels[(int)settings.Dim] * 100)}%";
    panel.RotateText = settings.AutoRotateMinutes > 0 ? $"Rotate: {settings.AutoRotateMinutes} min" : "Rotate: off";
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
            "1-9, 0 / Tab   switch scene",
            "Up / Down      master volume",
            "Left / Right   select sound layer",
            "PgUp / PgDn    layer volume (also + / -)",
            "T              sleep timer (fades out, then quits)",
            "D              dim screen",
            "R              auto-rotate scenes (off, 5, 10, 20, 30, 60 min)",
            "Mouse          move it to show the clickable panel",
            "F / F11        fullscreen",
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
