using System.Numerics;
using Raylib_cs;

// The clickable control panel. It fades in when the mouse moves or a key is pressed and fades out
// after a few seconds. It shows what the app is doing (via PanelModel) and reports clicks back
// through the model's actions; it keeps no app state of its own except which slider is being dragged.
public class PanelModel
{
    public string[] SceneNames = Array.Empty<string>();
    public int SceneIdx;
    public float Master;
    public (string Label, float Value)[] Layers = Array.Empty<(string, float)>();
    public int LayerSel;
    public string TimerText = "", DimText = "", RotateText = "";
    public bool LofiOn;

    public Action<int> SelectScene = _ => { };
    public Action<float> SetMaster = _ => { };
    public Action<int, float> SetLayer = (_, _) => { };
    public Action CycleTimer = () => { }, CycleDim = () => { }, CycleRotate = () => { };
    public Action HidePanel = () => { };
    public Action ToggleLofi = () => { }, ToggleFullscreen = () => { }, Quit = () => { };
    /// <summary>Called when a slider drag ends, so settings are saved once, not on every pixel.</summary>
    public Action Commit = () => { };
}

public static class ControlPanel
{
    static int dragId = -1;
    static readonly Color Warm = new(255, 240, 215, 255);

    static Color A(Color c, float alpha, float mul = 1) => new((int)c.R, (int)c.G, (int)c.B, (int)Math.Clamp(c.A * alpha * mul, 0, 255));

    /// <summary>Draws the panel and handles clicks. Returns true if the mouse is over it (keeps it visible).</summary>
    public static bool Draw(PanelModel m, int sw, int sh, float alpha)
    {
        if (alpha <= 0.01f) { dragId = -1; return false; }
        float s = sh / 720f;
        int fs = Math.Max(11, (int)(16 * s));
        float pad = 14 * s, gap = 6 * s, btnH = fs * 1.9f, rowH = fs * 1.7f;
        float pw = 360 * s;
        int sceneRows = (m.SceneNames.Length + 1) / 2;
        float ph = pad + fs * 1.9f                              // title
                 + sceneRows * (btnH + gap) + pad               // scene buttons
                 + (1 + m.Layers.Length) * rowH + pad           // sliders
                 + 3 * (btnH + gap) + fs * 1.4f + pad;          // option buttons and the footer line
        var panel = new Rectangle(pad, pad, pw, ph);
        var mouse = Raylib.GetMousePosition();
        bool hot = Raylib.CheckCollisionPointRec(mouse, panel) || dragId != -1;
        bool live = alpha > 0.5f;

        Raylib.DrawRectangleRounded(panel, 0.03f, 8, new Color(10, 6, 3, (int)(alpha * 200)));
        float x = panel.X + pad, w = pw - 2 * pad, y = panel.Y + pad;

        UiText.Draw("Sleepytime Ambience", x, y, (int)(fs * 1.2f), A(Warm, alpha, 0.9f));
        var hideBtn = new Rectangle(x + w - 84 * s, y - 2 * s, 84 * s, fs * 1.6f);
        if (Button(hideBtn, "Hide (P)", false, alpha, live, fs)) m.HidePanel();
        y += fs * 1.9f;

        // Scene buttons, two per row
        float bw = (w - gap) / 2;
        for (int i = 0; i < m.SceneNames.Length; i++)
        {
            var r = new Rectangle(x + (i % 2) * (bw + gap), y + (i / 2) * (btnH + gap), bw, btnH);
            if (Button(r, m.SceneNames[i], i == m.SceneIdx, alpha, live, fs)) m.SelectScene(i);
        }
        y += sceneRows * (btnH + gap) + pad;

        // Sliders: master first, then each sound layer of this scene
        float v = Slider(1000, new Rectangle(x, y, w, rowH), "Master", m.Master, alpha, live, false, fs);
        if (v != m.Master) m.SetMaster(v);
        y += rowH;
        for (int i = 0; i < m.Layers.Length; i++)
        {
            float nv = Slider(i, new Rectangle(x, y, w, rowH), m.Layers[i].Label, m.Layers[i].Value, alpha, live, i == m.LayerSel, fs);
            if (nv != m.Layers[i].Value) m.SetLayer(i, nv);
            y += rowH;
        }
        y += pad;

        // Option buttons, two per row
        string[] labels = { m.TimerText, m.DimText, m.RotateText, "Lo-fi: " + (m.LofiOn ? "on" : "off"), "Fullscreen", "Quit" };
        Action[] actions = { m.CycleTimer, m.CycleDim, m.CycleRotate, m.ToggleLofi, m.ToggleFullscreen, m.Quit };
        for (int i = 0; i < labels.Length; i++)
        {
            var r = new Rectangle(x + (i % 2) * (bw + gap), y + (i / 2) * (btnH + gap), bw, btnH);
            if (Button(r, labels[i], false, alpha, live, fs)) actions[i]();
        }
        y += 3 * (btnH + gap);
        UiText.Draw("H: keyboard help", x, y, (int)(fs * 0.85f), A(Warm, alpha, 0.5f));

        if (dragId != -1 && Raylib.IsMouseButtonReleased(MouseButton.Left)) { dragId = -1; m.Commit(); }
        return hot;
    }

    static bool Button(Rectangle r, string text, bool selected, float alpha, bool live, int fs)
    {
        bool hover = live && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), r);
        var fill = selected ? new Color(255, 190, 120, 80) : hover ? new Color(255, 240, 215, 50) : new Color(255, 240, 215, 22);
        Raylib.DrawRectangleRounded(r, 0.3f, 6, A(fill, alpha));
        float tw = UiText.Measure(text, fs);
        UiText.Draw(text, (r.X + (r.Width - tw) / 2), (r.Y + (r.Height - fs) / 2), fs, A(Warm, alpha, selected ? 1f : 0.8f));
        return hover && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    // A horizontal slider with its label on the left and the percentage on the right.
    // Returns the (possibly changed) value, snapped to 5% steps.
    static float Slider(int id, Rectangle row, string label, float value, float alpha, bool live, bool selected, int fs)
    {
        float labelW = row.Width * 0.38f, valueW = row.Width * 0.14f;
        var track = new Rectangle(row.X + labelW, row.Y + row.Height / 2 - 3, row.Width - labelW - valueW, 6);
        var mouse = Raylib.GetMousePosition();
        var hit = new Rectangle(track.X - 6, row.Y, track.Width + 12, row.Height);
        if (live && dragId == -1 && Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mouse, hit)) dragId = id;
        if (dragId == id && Raylib.IsMouseButtonDown(MouseButton.Left))
            value = MathF.Round(Math.Clamp((mouse.X - track.X) / track.Width, 0, 1) * 20) / 20;

        UiText.Draw(label, row.X, (row.Y + (row.Height - fs) / 2), fs, A(Warm, alpha, selected ? 1f : 0.75f));
        Raylib.DrawRectangleRounded(track, 1f, 4, A(new Color(255, 240, 215, 40), alpha));
        var fill = new Rectangle(track.X, track.Y, track.Width * value, track.Height);
        if (fill.Width > 1) Raylib.DrawRectangleRounded(fill, 1f, 4, A(new Color(255, 190, 120, 200), alpha));
        Raylib.DrawCircleV(new Vector2(track.X + track.Width * value, track.Y + 3), 6 * (fs / 15f), A(new Color(255, 225, 180, 255), alpha));
        string pct = $"{(int)MathF.Round(value * 100)}%";
        UiText.Draw(pct, row.X + row.Width - UiText.Measure(pct, fs), row.Y + (row.Height - fs) / 2, fs, A(Warm, alpha, 0.7f));
        return value;
    }
}
