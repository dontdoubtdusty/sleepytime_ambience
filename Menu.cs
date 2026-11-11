using System.Numerics;
using System.Text.RegularExpressions;
using Raylib_cs;

// What the Esc menu shows and does. Program.cs refreshes the values every frame and supplies the actions.
public class MenuModel
{
    public string Version = "";
    public string UpdateStatus = "";
    public bool UpdateAvailable, UpdateBusy, CheckAtLaunch;
    public string[] Credits = Array.Empty<string>();
    public Action Quit = () => { }, OpenScreenshots = () => { }, ResetSettings = () => { }, OpenWebsite = () => { };
    public Action CheckNow = () => { }, InstallUpdate = () => { }, ToggleCheckAtLaunch = () => { }, OpenDownloadPage = () => { };
}

public enum ToastChoice { None, Update, Later, Skip }

// The Esc menu (Resume, Guide, Keyboard shortcuts, Updates, screenshots folder, About, Reset settings, Quit)
// and the soft "new version" pop-up. Like the control panel, it draws what the model says and reports
// choices through the model's actions; its only own state is which page and button are selected.
public static class Menu
{
    enum Page { Main, Guide, Shortcuts, Updates, About, Reset }
    static Page page = Page.Main;
    static int sel, guidePage;
    public static bool IsOpen { get; private set; }

    static readonly Color Warm = new(255, 240, 215, 255), Glow = new(255, 190, 120, 255);
    static Color A(Color c, float a) => new((int)c.R, (int)c.G, (int)c.B, (int)Math.Clamp(c.A * a, 0, 255));

    // Every keyboard shortcut; also used by the H help overlay.
    public static readonly (string Keys, string Does)[] Shortcuts =
    {
        ("1-9, 0, F1-F4 / Tab", "switch scene"),
        ("Up / Down", "master volume"),
        ("Left / Right", "select sound layer"),
        ("PgUp / PgDn", "layer volume (also + / -)"),
        ("T", "sleep timer, up to 10 h (fades out, then quits)"),
        ("U", "stop at a clock time (Shift+U: +30 min)"),
        ("D", "dim screen"),
        ("R", "auto-rotate scenes (off, 5, 10, 20, 30, 60 min)"),
        ("V", "add or remove this scene from auto-rotate"),
        ("S", "shuffle auto-rotate"),
        ("E", "random sound events on / off"),
        ("N", "night drift (slow dim and quieting) on / off"),
        ("Mouse", "move it to show the clickable panel"),
        ("F / F11", "fullscreen"),
        ("F12", "save a screenshot to Pictures"),
        ("L", "lo-fi look on / off"),
        ("C", "clocks show the real time on / off (off: a random start time)"),
        ("P", "hide or show the control panel"),
        ("H", "toggle the shortcut list"),
        ("Esc", "menu: guide, updates, quit"),
    };

    // The guide, one short page per topic.
    static readonly (string Title, string Body)[] Guide =
    {
        ("Welcome",
         "Drowse plays a cozy scene with soft sounds, all from your own computer, so it never stops when the internet drops.\n\n" +
         "Pick a scene, set a timer, and let it run while you fall asleep.\n\n" +
         "Move the mouse to bring up the control panel. It hides itself again after a few seconds."),
        ("Scenes and sounds",
         "There are fourteen scenes. Click one in the panel, or press 1-9, 0 and F1-F4. Tab goes to the next one.\n\n" +
         "Each scene mixes a few sounds. The sliders set each one, and Master sets the overall volume.\n\n" +
         "Sound events adds the odd owl, wind chime, gull or log pop now and then."),
        ("Falling asleep",
         "Timer: plays for a set time, fades out over the last five minutes, then closes the app.\n\n" +
         "Stop at: the same, but at a clock time, such as 01:00.\n\n" +
         "Dim: darkens the picture. Night drift: over two and a half hours the picture slowly darkens and the sound grows quieter."),
        ("Scenes that change by themselves",
         "Rotate: moves to another scene every few minutes, with a slow fade.\n\n" +
         "Right-click a scene button to leave it out. Once some are left out, a small dot marks the ones still in.\n\n" +
         "Shuffle: picks the next scene at random instead of in order."),
        ("Look and extras",
         "Lo-fi: the soft, grainy picture style. Turn it off for a cleaner image.\n\n" +
         "F or F11 switches fullscreen. F12 saves a screenshot to your Pictures folder. P hides the panel completely.\n\n" +
         "Everything you change is saved, so the app opens the way you left it."),
    };

    public static void Open(bool updates = false) { IsOpen = true; page = updates ? Page.Updates : Page.Main; sel = 0; }
    // Test only (SLEEPY_SHOT_UI): opens the named page for a --shot screenshot.
    public static void OpenPage(string name) { IsOpen = true; page = Enum.TryParse<Page>(name, true, out var p) ? p : Page.Main; }

    // Esc: opens the menu, steps back from a sub-page, or closes it.
    public static void HandleEsc()
    {
        if (!IsOpen) Open();
        else if (page != Page.Main) { page = Page.Main; sel = 0; }
        else IsOpen = false;
    }

    // Reads the sound credits ("Title - Author") from Assets/CREDITS.md for the About page.
    public static string[] LoadCredits()
    {
        try
        {
            return File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Assets", "CREDITS.md"))
                .Where(l => l.StartsWith("| ") && !l.StartsWith("| File"))
                .Select(l => l.Split('|'))
                .Where(c => c.Length > 4)
                .Select(c =>
                {
                    string title = Regex.Replace(c[2].Trim(), @"\s*\(.*?\)", "");
                    string author = c[3].Trim();
                    return author == "see page" ? title : $"{title} - {author}";
                })
                .ToArray();
        }
        catch { return Array.Empty<string>(); }
    }

    /// <summary>Draws the menu at the given fade (0..1) and handles its keys and clicks.</summary>
    public static void Draw(MenuModel m, int sw, int sh, float alpha)
    {
        if (alpha <= 0.01f) return;
        bool live = IsOpen && alpha > 0.5f;
        float s = sh / 720f;
        int fs = Math.Max(12, (int)(18 * s)), small = Math.Max(11, (int)(15 * s));

        Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, (int)(alpha * 110)));
        float cw = MathF.Min(600 * s, sw - 32 * s), ch = MathF.Min(580 * s, sh - 32 * s);
        var card = new Rectangle((sw - cw) / 2, (sh - ch) / 2, cw, ch);
        Raylib.DrawRectangleRounded(card, 0.04f, 8, new Color(14, 9, 5, (int)(alpha * 228)));
        float pad = 30 * s, x = card.X + pad, w = cw - 2 * pad, y = card.Y + pad;
        var buttons = new List<(string Label, Action Do)>();
        void Text(string t, float size, Color c, float a) { UiText.Draw(t, x, y, size, A(c, alpha * a)); y += size * 1.45f; }
        void Para(string t, float size, float a) { foreach (var l in UiText.Wrap(t, size, w)) Text(l, size, Warm, a); }
        void Back() { page = Page.Main; sel = 0; }

        switch (page)
        {
            case Page.Main:
                Text("Drowse", fs * 1.6f, Warm, 0.95f);
                Text($"Version {m.Version}", small, Warm, 0.5f);
                y += 10 * s;
                buttons.Add(("Resume", () => IsOpen = false));
                buttons.Add(("Guide", () => { page = Page.Guide; guidePage = 0; sel = 0; }));
                buttons.Add(("Keyboard shortcuts", () => { page = Page.Shortcuts; sel = 0; }));
                buttons.Add((m.UpdateAvailable ? "Updates - a new version is ready" : "Updates", () => { page = Page.Updates; sel = 0; }));
                buttons.Add(("Open screenshots folder", m.OpenScreenshots));
                buttons.Add(("About", () => { page = Page.About; sel = 0; }));
                buttons.Add(("Reset settings", () => { page = Page.Reset; sel = 1; }));
                buttons.Add(("Quit", m.Quit));
                break;

            case Page.Guide:
                var g = Guide[guidePage];
                Text("Guide", fs * 1.6f, Warm, 0.95f);
                Text(g.Title, fs * 1.1f, Glow, 0.95f);
                y += 6 * s;
                Para(g.Body, fs, 0.85f);
                y += 6 * s;
                Text($"Page {guidePage + 1} of {Guide.Length}. Left and Right arrows turn pages.", small, Warm, 0.45f);
                if (guidePage > 0) buttons.Add(("Previous", () => guidePage--));
                if (guidePage < Guide.Length - 1) buttons.Add(("Next", () => guidePage++));
                buttons.Add(("Back", Back));
                if (live && Raylib.IsKeyPressed(KeyboardKey.Left) && guidePage > 0) { guidePage--; sel = 0; }
                if (live && Raylib.IsKeyPressed(KeyboardKey.Right) && guidePage < Guide.Length - 1) { guidePage++; sel = 0; }
                break;

            case Page.Shortcuts:
                Text("Keyboard shortcuts", fs * 1.6f, Warm, 0.95f);
                y += 4 * s;
                foreach (var (k, d) in Shortcuts)
                {
                    UiText.Draw(k, x, y, small, A(Glow, alpha * 0.85f));
                    UiText.Draw(d, x + 170 * s, y, small, A(Warm, alpha * 0.85f));
                    y += small * 1.35f;
                }
                buttons.Add(("Back", Back));
                break;

            case Page.Updates:
                Text("Updates", fs * 1.6f, Warm, 0.95f);
                Text($"This is version {m.Version}.", fs, Warm, 0.85f);
                foreach (var l in UiText.Wrap(m.UpdateStatus, fs, w)) Text(l, fs, m.UpdateAvailable ? Glow : Warm, 0.95f);
                y += 8 * s;
                Para("With Check at launch on, the app looks for a new version once, a few seconds after it starts. " +
                     "Updating downloads it, swaps the files and restarts the app. Your settings stay. Nothing else ever goes online.", small, 0.55f);
                if (!m.UpdateBusy) buttons.Add(m.UpdateAvailable ? ("Update now", m.InstallUpdate) : ("Check now", m.CheckNow));
                buttons.Add(($"Check at launch: {(m.CheckAtLaunch ? "on" : "off")}", m.ToggleCheckAtLaunch));
                buttons.Add(("Download page", m.OpenDownloadPage));
                buttons.Add(("Back", Back));
                break;

            case Page.About:
                Text($"Drowse {m.Version}", fs * 1.6f, Warm, 0.95f);
                Para("Cozy lo-fi scenes and real recorded sounds for falling asleep. Everything runs from your own computer, " +
                     "so it never stops when the internet drops.", fs, 0.85f);
                y += 6 * s;
                Text("Sounds", fs * 1.1f, Glow, 0.95f);
                // Credits in two columns, cut to fit the space above the buttons.
                float colW = (w - 16 * s) / 2, lineH = small * 1.3f, top = y;
                int perCol = Math.Max(1, Math.Min((m.Credits.Length + 1) / 2, (int)((card.Y + ch - pad - fs * 2.2f - 12 * s - top) / lineH)));
                for (int i = 0; i < m.Credits.Length && i < perCol * 2; i++)
                {
                    string c = m.Credits[i];
                    while (c.Length > 4 && UiText.Measure(c, small) > colW) c = c[..^4] + "...";
                    UiText.Draw(c, x + (i / perCol) * (colW + 16 * s), top + (i % perCol) * lineH, small, A(Warm, alpha * 0.6f));
                }
                buttons.Add(("GitHub page", m.OpenWebsite));
                buttons.Add(("Back", Back));
                break;

            case Page.Reset:
                Text("Reset settings", fs * 1.6f, Warm, 0.95f);
                Para("Put volumes, timers, rotation, dimming and every other option back to how they were at the start? " +
                     "Your current scene, fullscreen and update choices stay.", fs, 0.85f);
                buttons.Add(("Reset", () => { m.ResetSettings(); Back(); }));
                buttons.Add(("Cancel", Back));
                break;
        }

        // Buttons: a list on the main page, rows of two along the bottom on the others.
        float btnH = fs * 2.0f, gap = 8 * s;
        sel = Math.Clamp(sel, 0, buttons.Count - 1);
        Action? chosen = null;
        for (int i = 0; i < buttons.Count; i++)
        {
            Rectangle r;
            if (page == Page.Main) r = new Rectangle(x, y + i * (btnH + gap), w, btnH);
            else
            {
                int rows = (buttons.Count + 1) / 2, row = i / 2;
                float by = card.Y + ch - pad - rows * btnH - (rows - 1) * gap + row * (btnH + gap);
                bool alone = i == buttons.Count - 1 && buttons.Count % 2 == 1;
                float bw = alone ? w : (w - gap) / 2;
                r = new Rectangle(alone ? x : x + (i % 2) * (bw + gap), by, bw, btnH);
            }
            if (live && Raylib.GetMouseDelta() != Vector2.Zero && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), r)) sel = i;
            if (ControlPanel.Button(r, buttons[i].Label, i == sel, alpha, live, fs) == 1) chosen = buttons[i].Do;
        }

        if (live)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Down) || (page == Page.Main && Raylib.IsKeyPressed(KeyboardKey.Tab))) sel = (sel + 1) % buttons.Count;
            if (Raylib.IsKeyPressed(KeyboardKey.Up)) sel = (sel + buttons.Count - 1) % buttons.Count;
            if (Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.KpEnter) || Raylib.IsKeyPressed(KeyboardKey.Space))
                chosen = buttons[sel].Do;
        }
        chosen?.Invoke();
    }

    /// <summary>Draws the small "new version" pop-up in the bottom right. Returns the choice clicked and whether the mouse is over it.</summary>
    public static (ToastChoice Choice, bool Hover) DrawToast(string version, int sw, int sh, float alpha)
    {
        if (alpha <= 0.01f) return (ToastChoice.None, false);
        float s = sh / 720f;
        int fs = Math.Max(12, (int)(17 * s)), small = Math.Max(11, (int)(14 * s));
        float w = 380 * s, h = 118 * s, m = 24 * s;
        var r = new Rectangle(sw - w - m, sh - h - m, w, h);
        bool hover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), r);
        Raylib.DrawRectangleRounded(r, 0.14f, 8, new Color(14, 9, 5, (int)(alpha * 215)));

        // A small warm glow, like a lamp, as the pop-up's icon.
        var c = new Vector2(r.X + 30 * s, r.Y + 31 * s);
        Raylib.DrawCircleGradient(c, 17 * s, A(Glow, alpha * 0.4f), A(Glow, 0));
        Raylib.DrawCircleV(c, 5 * s, A(new Color(255, 225, 180, 255), alpha));

        float tx = r.X + 56 * s;
        UiText.Draw("A new version is ready", tx, r.Y + 16 * s, fs, A(Warm, alpha * 0.95f));
        UiText.Draw($"Version {version}. Your settings stay as they are.", tx, r.Y + 16 * s + fs * 1.4f, small, A(Warm, alpha * 0.55f));

        string[] labels = { "Update", "Later", "Skip this version" };
        float[] share = { 0.3f, 0.25f, 0.45f };
        float gap = 8 * s, bh = small * 2.1f, bx = r.X + 16 * s, by = r.Y + h - bh - 14 * s, inner = w - 32 * s - 2 * gap;
        var choice = ToastChoice.None;
        for (int i = 0; i < 3; i++)
        {
            var b = new Rectangle(bx, by, inner * share[i], bh);
            if (ControlPanel.Button(b, labels[i], false, alpha, alpha > 0.6f, small) == 1) choice = (ToastChoice)(i + 1);
            bx += b.Width + gap;
        }
        return (choice, hover);
    }
}
