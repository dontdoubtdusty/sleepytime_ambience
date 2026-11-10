using System.Numerics;
using Raylib_cs;
using static Gfx;

// ---------------------------------------------------------------------------
// Rainy library
// ---------------------------------------------------------------------------
public class LibraryScene : Scene
{
    public override string Name => "Rainy Library";
    protected override (Layer, string, float)[] SoundLayers => new[]
    {
        (Layer.Rain, "Rain", 0.6f), (Layer.Thunder, "Distant thunder", 0.3f), (Layer.Clock, "Clock", 0.12f),
    };

    const int Wx = 200, Wy = 30, Ww = 240, Wh = 215;
    readonly AmbientAudio audio;
    readonly List<(Rectangle r, Color c, int band)> books = new();
    readonly RainWindow rain = new(Wx, Wy, Ww, Wh);
    readonly (float x, float y, float r, Color c)[] bokeh = new (float, float, float, Color)[16];
    struct Steam { public float X, Y, Life; }
    readonly List<Steam> steam = new();
    float t, steamAcc, lampFlick = 1, flash, nextFlash = 18, flashStage;

    public LibraryScene(AmbientAudio audio)
    {
        this.audio = audio;
        var rnd = new Random(11);
        Color[] pal =
        {
            C(88, 38, 36), C(40, 56, 70), C(72, 60, 38), C(46, 68, 50), C(80, 60, 68), C(108, 82, 52), C(36, 40, 60), C(60, 44, 34),
        };
        foreach (var (x0, x1) in new[] { (0, 178), (462, 640) })
            for (int row = 0; row < 5; row++)
            {
                int shelfBottom = 66 + row * 66;
                float x = x0 + 4;
                while (x < x1 - 8)
                {
                    int bw = rnd.Next(6, 13), bh = rnd.Next(34, 58);
                    books.Add((new Rectangle(x, shelfBottom - bh, bw, bh), pal[rnd.Next(pal.Length)], rnd.Next(5, bh - 5)));
                    x += bw + 1;
                    if (rnd.Next(14) == 0) x += rnd.Next(10, 30); // gap on the shelf
                }
            }

        for (int i = 0; i < bokeh.Length; i++)
            bokeh[i] = (Rf(Wx, Wx + Ww), Rf(Wy + 110, Wy + Wh - 25), Rf(4, 10),
                rnd.Next(3) == 0 ? C(120, 160, 220, 38) : C(255, 190, 100, 42));
    }

    public override void Update(float dt)
    {
        t += dt;
        lampFlick += (1 + MathF.Sin(t * 3.1f) * 0.02f + Rf(-0.02f, 0.02f) - lampFlick) * 0.2f;

        rain.Update(dt);

        steamAcc += dt * 5;
        while (steamAcc >= 1) { steamAcc--; steam.Add(new Steam { X = 305 + Rf(-2, 2), Y = 296 }); }
        for (int i = steam.Count - 1; i >= 0; i--)
        {
            var s = steam[i];
            s.Life += dt;
            if (s.Life > 2.4f) { steam.RemoveAt(i); continue; }
            s.Y -= 12 * dt;
            s.X += MathF.Sin(t * 2 + s.Life * 3) * 5 * dt;
            steam[i] = s;
        }

        // Lightning: two quick pulses, then distant thunder.
        nextFlash -= dt;
        if (nextFlash <= 0 && flashStage == 0)
        {
            flashStage = 1; flash = 1; audio.TriggerThunder();
        }
        if (flashStage > 0)
        {
            flash -= dt * 5;
            if (flash <= 0)
            {
                if (flashStage == 1) { flashStage = 2; flash = 0.7f; }
                else { flashStage = 0; flash = 0; nextFlash = Rf(30, 80); }
            }
        }
    }

    // An analog desk clock showing the computer's local time; the second hand ticks once a second.
    static void DrawDeskClock(float cx, float cy, float r)
    {
        var now = DateTime.Now;
        float min = now.Minute + now.Second / 60f;
        float hr = now.Hour % 12 + min / 60f;
        Raylib.DrawRectangle((int)(cx - r * 0.7f), (int)(cy + r), (int)(r * 0.3f), 3, C(50, 34, 24));
        Raylib.DrawRectangle((int)(cx + r * 0.4f), (int)(cy + r), (int)(r * 0.3f), 3, C(50, 34, 24));
        Raylib.DrawCircleV(new Vector2(cx, cy), r + 2.5f, C(74, 50, 34));
        Raylib.DrawCircleV(new Vector2(cx, cy), r, C(160, 148, 126));
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.PI / 6;
            var d = new Vector2(MathF.Sin(a), -MathF.Cos(a));
            float inner = i % 3 == 0 ? 0.72f : 0.84f;
            Raylib.DrawLineEx(new Vector2(cx, cy) + d * r * inner, new Vector2(cx, cy) + d * r * 0.95f, i % 3 == 0 ? 1.5f : 1f, C(40, 30, 24));
        }
        void Hand(float turns, float len, float thick, Color col)
        {
            float a = turns * 2 * MathF.PI;
            Raylib.DrawLineEx(new Vector2(cx, cy), new Vector2(cx + MathF.Sin(a) * r * len, cy - MathF.Cos(a) * r * len), thick, col);
        }
        Hand(hr / 12f, 0.5f, 2f, C(40, 30, 26));
        Hand(min / 60f, 0.75f, 1.5f, C(40, 30, 26));
        Hand(now.Second / 60f, 0.85f, 0.8f, C(170, 50, 40));
        Raylib.DrawCircleV(new Vector2(cx, cy), 1.6f, C(40, 30, 26));
    }

    public override void Draw()
    {
        Raylib.DrawRectangleGradientV(0, 0, W, H, C(28, 20, 16), C(14, 10, 8));

        // Shelves
        foreach (var (x0, x1) in new[] { (0, 178), (462, 640) })
        {
            Raylib.DrawRectangle(x0, 0, x1 - x0, H, C(26, 17, 12));
            for (int row = 0; row < 5; row++)
                Raylib.DrawRectangle(x0, 66 + row * 66, x1 - x0, 6, C(58, 38, 25));
        }
        foreach (var (r, c, band) in books)
        {
            Raylib.DrawRectangleRec(r, c);
            Raylib.DrawRectangle((int)r.X, (int)r.Y + band, (int)r.Width, 2, Lerp(c, C(200, 170, 110), 0.35f));
        }
        Raylib.DrawRectangle(178, 0, 22, H, C(30, 20, 14));
        Raylib.DrawRectangle(440, 0, 22, H, C(30, 20, 14));

        // Window: stormy night, bokeh city lights, rain
        Raylib.BeginScissorMode(Wx, Wy, Ww, Wh);
        Raylib.DrawRectangleGradientV(Wx, Wy, Ww, Wh, C(13, 18, 30), C(34, 42, 58));
        Raylib.BeginBlendMode(BlendMode.Additive);
        foreach (var (x, y, r, c) in bokeh) Glow(x, y, r * 2.2f, c);
        Raylib.EndBlendMode();
        rain.DrawStreaks();
        rain.DrawGlassDrops();
        if (flash > 0)
            Raylib.DrawRectangle(Wx, Wy, Ww, Wh, C(190, 205, 255, (int)(150 * Math.Clamp(flash, 0, 1))));
        Raylib.EndScissorMode();

        // Window frame
        Raylib.DrawRectangleLinesEx(new Rectangle(Wx - 9, Wy - 9, Ww + 18, Wh + 18), 9, C(66, 44, 28));
        Raylib.DrawRectangle(Wx + Ww / 2 - 4, Wy, 8, Wh, C(58, 38, 25));
        Raylib.DrawRectangle(Wx, Wy + Wh / 2 - 4, Ww, 8, C(58, 38, 25));
        Raylib.DrawRectangle(Wx - 20, Wy + Wh + 9, Ww + 40, 12, C(76, 52, 34));

        // Desk with cup and book
        Raylib.DrawRectangle(130, 300, 380, 10, C(84, 56, 36));
        Raylib.DrawRectangle(130, 300, 380, 2, C(112, 78, 52));
        Raylib.DrawRectangle(140, 310, 360, 50, C(36, 24, 17));
        Raylib.DrawRectangle(342, 292, 40, 8, C(92, 44, 40));
        Raylib.DrawRectangle(346, 285, 34, 7, C(44, 62, 72));
        Raylib.DrawRectangle(298, 288, 16, 12, C(210, 205, 195));
        Raylib.DrawRectangle(298, 289, 16, 2, C(110, 70, 40));
        DrawDeskClock(236, 279, 18);

        // Lamp
        Raylib.DrawRectangle(414, 262, 4, 38, C(40, 30, 22));
        Raylib.DrawRectangle(404, 296, 24, 5, C(40, 30, 22));
        Raylib.DrawTriangle(new Vector2(411, 236), new Vector2(396, 264), new Vector2(430, 264), C(214, 172, 100));

        // Steam, light and rain-light
        Raylib.BeginBlendMode(BlendMode.Additive);
        foreach (var s in steam)
            Glow(s.X, s.Y, 5 + s.Life * 4, C(200, 200, 205, (int)(26 * (1 - s.Life / 2.4f))));
        Glow(413, 262, 280, C(255, 175, 80, (int)(62 * lampFlick)));
        Glow(413, 262, 90, C(255, 200, 120, (int)(60 * lampFlick)));
        Glow(320, 140, 230, C(110, 140, 200, 14));
        if (flash > 0) Glow(320, 140, 380, C(170, 190, 255, (int)(40 * Math.Clamp(flash, 0, 1))));
        Raylib.EndBlendMode();

        Vignette(165);
    }
}
