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


var audio = new AmbientAudio(openDevice: shotPath == null);
Scene[] scenes = { new FireplaceScene(), new CabinScene(), new ForestScene(), new LibraryScene(audio) };
int sceneIdx = Math.Clamp(settings.Scene, 0, scenes.Length - 1);
int layerSel = 0;

var target = Raylib.LoadRenderTexture(Gfx.W, Gfx.H);
Raylib.SetTextureFilter(target.Texture, TextureFilter.Bilinear);

if (shotPath == null) PreventSleep(true);
if (settings.Fullscreen && shotPath == null) Raylib.ToggleBorderlessWindowed();

int[] timerChoices = { 0, 15, 30, 45, 60, 90, 120, 180 };
float[] dimLevels = { 0f, 0.3f, 0.55f, 0.75f, 0.88f };
float timerRemaining = settings.TimerMinutes * 60f;
float timerTotal = timerRemaining;
float idle = 0, sceneFade = 1, helpOn = 0;
bool showHelp = false;
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

while (!Raylib.WindowShouldClose())
{
    float dt = shotPath != null ? 1f / 30f : Math.Min(Raylib.GetFrameTime(), 0.1f);
    bool touched = false;

    // ---- input ----
    int newScene = sceneIdx;
    if (Raylib.IsKeyPressed(KeyboardKey.One)) newScene = 0;
    if (Raylib.IsKeyPressed(KeyboardKey.Two)) newScene = 1;
    if (Raylib.IsKeyPressed(KeyboardKey.Three)) newScene = 2;
    if (Raylib.IsKeyPressed(KeyboardKey.Four)) newScene = 3;
    if (Raylib.IsKeyPressed(KeyboardKey.Tab)) newScene = (sceneIdx + 1) % scenes.Length;
    if (newScene != sceneIdx) { sceneIdx = newScene; sceneFade = 1; ApplyScene(); touched = true; }

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

    if (Raylib.IsKeyPressed(KeyboardKey.D)) { settings.Dim = (settings.Dim + 1) % dimLevels.Length; settings.Save(); touched = true; }
    if (Raylib.IsKeyPressed(KeyboardKey.T))
    {
        int i = Array.IndexOf(timerChoices, settings.TimerMinutes);
        settings.TimerMinutes = timerChoices[(i + 1) % timerChoices.Length];
        timerRemaining = timerTotal = settings.TimerMinutes * 60f;
        settings.Save(); touched = true;
    }
    if (Raylib.IsKeyPressed(KeyboardKey.F) || Raylib.IsKeyPressed(KeyboardKey.F11))
    {
        Raylib.ToggleBorderlessWindowed();
        settings.Fullscreen = Raylib.IsWindowState(ConfigFlags.UndecoratedWindow) || Raylib.IsWindowFullscreen();
        settings.Save(); touched = true;
    }
    if (Raylib.IsKeyPressed(KeyboardKey.H)) { showHelp = !showHelp; touched = true; }

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
    sceneFade = Math.Max(0, sceneFade - dt / 0.7f);
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
        var img = Raylib.LoadImageFromTexture(target.Texture);
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
    Raylib.DrawTexturePro(target.Texture, new Rectangle(0, 0, Gfx.W, -Gfx.H), dest, Vector2.Zero, 0, Color.White);

    float dim = dimLevels[(int)settings.Dim];
    if (dim > 0) Raylib.DrawRectangle(0, 0, sw, sh, new Color(10, 5, 0, (int)(dim * 255)));
    float black = Math.Max(1 - fade, sceneFade * sceneFade);
    if (black > 0) Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, (int)(Math.Clamp(black, 0, 1) * 255)));

    DrawHud(sw, sh);
    Raylib.EndDrawing();
}

if (shotPath == null) { settings.Save(); PreventSleep(false); }
if (Environment.GetEnvironmentVariable("SLEEPY_AUDIO_STATS") != null) Console.WriteLine($"AUDIOSTATS reads={audio.Reads} frames={audio.FramesRead} maxReadMs={audio.MaxReadMs:F2}");
audio.Dispose();
Raylib.UnloadRenderTexture(target);

Raylib.CloseWindow();
return;

// ---------------------------------------------------------------------------

static bool KeyStep(KeyboardKey k) => Raylib.IsKeyPressed(k) || Raylib.IsKeyPressedRepeat(k);

void DrawHud(int sw, int sh)
{
    float a = Math.Clamp(1 - (idle - 3f) / 1f, 0, 1); // visible for 3s after input, then 1s fade
    if (a <= 0 && helpOn < 0.02f) return;
    float s = sh / 720f;
    int fs = Math.Max(12, (int)(22 * s)), pad = (int)(20 * s), y = pad;
    int al = (int)(a * 230);
    var white = new Color(255, 240, 215, al);
    var soft = new Color(255, 240, 215, (int)(al * 0.55f));
    var sc = scenes[sceneIdx];

    Raylib.DrawText($"{sc.Name}   ({sceneIdx + 1}/{scenes.Length})", pad, y, (int)(fs * 1.3f), white); y += (int)(fs * 1.9f);
    Raylib.DrawText($"Volume  {Bar(settings.Master)}  {(int)(settings.Master * 100)}%", pad, y, fs, white); y += (int)(fs * 1.5f);
    var v = Vols();
    for (int i = 0; i < sc.Layers.Length; i++)
    {
        bool sel = i == layerSel;
        Raylib.DrawText($"{(sel ? ">" : " ")} {sc.Layers[i].Label,-18} {Bar(v[i])}", pad, y, fs, sel ? white : soft);
        y += (int)(fs * 1.4f);
    }
    y += (int)(fs * 0.4f);
    string timer = timerTotal > 0 ? $"{(int)(timerRemaining / 60)}:{(int)(timerRemaining % 60):00} left" : "off";
    Raylib.DrawText($"Sleep timer: {timer}     Dim: {(int)(dimLevels[(int)settings.Dim] * 100)}%", pad, y, fs, soft);
    y += (int)(fs * 1.5f);
    Raylib.DrawText("H for help", pad, y, (int)(fs * 0.8f), soft);

    if (helpOn > 0.02f)
    {
        string[] lines =
        {
            "1-4 / Tab      switch scene",
            "Up / Down      master volume",
            "Left / Right   select sound layer",
            "PgUp / PgDn    layer volume (also + / -)",
            "T              sleep timer (fades out, then quits)",
            "D              dim screen",
            "F / F11        fullscreen",
            "H              toggle this help",
            "Esc            quit",
        };
        int hy = sh - pad - (int)(lines.Length * fs * 1.4f);
        int ha = (int)(helpOn * 230);
        foreach (var l in lines)
        {
            Raylib.DrawText(l, pad, hy, fs, new Color(255, 240, 215, ha));
            hy += (int)(fs * 1.4f);
        }
    }
}

static string Bar(float v)
{
    int n = (int)MathF.Round(v * 20);
    return "[" + new string('|', n) + new string('.', 20 - n) + "]";
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
