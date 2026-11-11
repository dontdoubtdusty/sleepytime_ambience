using System.Numerics;
using Raylib_cs;

// The app's text font (Lora, a warm serif). Falls back to raylib's built-in font if the file is missing.
// All on-screen text goes through here so the look stays consistent.
public static class UiText
{
    static Font font;
    static bool loaded;

    public static void Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Lora.ttf");
        if (!File.Exists(path)) { font = Raylib.GetFontDefault(); return; }
        // Rasterized large and drawn smaller, with smooth filtering, so every size stays crisp.
        font = Raylib.LoadFontEx(path, 64, null, 0);
        Raylib.SetTextureFilter(font.Texture, TextureFilter.Bilinear);
        loaded = true;
    }

    public static void Draw(string text, float x, float y, float size, Color color)
        => Raylib.DrawTextEx(font, text, new Vector2(x, y), size, size * 0.02f, color);

    public static float Measure(string text, float size)
        => Raylib.MeasureTextEx(font, text, size, size * 0.02f).X;

    // Splits text into lines that fit the width; "\n" starts a new paragraph (an empty line for "\n\n").
    public static List<string> Wrap(string text, float size, float width)
    {
        var lines = new List<string>();
        foreach (var para in text.Split('\n'))
        {
            string line = "";
            foreach (var word in para.Split(' '))
            {
                string next = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && Measure(next, size) > width) { lines.Add(line); line = word; }
                else line = next;
            }
            lines.Add(line);
        }
        return lines;
    }

    public static void Unload() { if (loaded) Raylib.UnloadFont(font); }
}
