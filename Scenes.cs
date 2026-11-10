using System.Numerics;
using Raylib_cs;
using static Gfx;

public abstract class Scene
{
    public abstract string Name { get; }
    /// <summary>Sound layers this scene mixes, with default volumes (0..1).</summary>
    public abstract (Layer Layer, string Label, float Default)[] Layers { get; }
    public abstract void Update(float dt);
    public abstract void Draw();
}

// ---------------------------------------------------------------------------
// 1. Fireplace
// ---------------------------------------------------------------------------
public class FireplaceScene : Scene
{
    public override string Name => "Fireplace";
    public override (Layer, string, float)[] Layers => new[]
    {
        (Layer.Crackle, "Fireplace", 0.8f),
    };

    readonly Fire fire = new(320, 316, 110, 1.75f);
    readonly List<(Rectangle r, Color c)> bricks = new();
    float t, candle = 1, candleTarget = 1, candleTimer;

    public FireplaceScene()
    {
        var rnd = new Random(7);
        for (int row = 0; 100 + row * 14 < H; row++)
        {
            int off = (row % 2) * 14;
            for (int x = 160 - off; x < 480; x += 28)
            {
                int y = 100 + row * 14;
                if (x + 14 > 215 && x + 14 < 425 && y + 6 > 175 && y + 6 < 335) continue; // opening
                int s = rnd.Next(-12, 13);
                bricks.Add((new Rectangle(Math.Max(x, 160) + 1, y + 1, 26, 12), C(74 + s, 48 + s / 2, 40 + s / 2)));
            }
        }
    }

    public override void Update(float dt)
    {
        t += dt;
        fire.Update(dt);
        candleTimer -= dt;
        if (candleTimer <= 0) { candleTarget = Rf(0.7f, 1.2f); candleTimer = Rf(0.08f, 0.3f); }
        candle += (candleTarget - candle) * Math.Min(1, dt * 8);
    }

    public override void Draw()
    {
        Raylib.DrawRectangleGradientV(0, 0, W, H, C(30, 22, 18), C(16, 11, 9));

        // Painting above the mantel
        Raylib.DrawRectangle(285, 8, 70, 62, C(62, 42, 28));
        Raylib.DrawRectangleGradientV(290, 13, 60, 52, C(40, 48, 62), C(26, 34, 40));
        Raylib.DrawTriangle(new Vector2(302, 65), new Vector2(322, 38), new Vector2(342, 65), C(18, 24, 28));

        // Stone surround
        Raylib.DrawRectangle(160, 100, 320, 260, C(40, 28, 24));
        foreach (var (r, c) in bricks) Raylib.DrawRectangleRec(r, c);

        // Mantel
        Raylib.DrawRectangle(138, 84, 364, 18, C(62, 40, 27));
        Raylib.DrawRectangle(138, 84, 364, 3, C(88, 60, 40));

        // Opening and hearth
        Raylib.DrawRectangle(215, 175, 210, 160, C(9, 6, 5));
        Raylib.DrawRectangleGradientV(220, 180, 200, 150, C(20, 12, 10), C(8, 5, 4));
        Raylib.DrawRectangle(148, 335, 344, 25, C(78, 68, 64));
        Raylib.DrawRectangle(148, 335, 344, 2, C(110, 98, 92));

        // Candles on the mantel
        foreach (int cx in new[] { 190, 455 })
        {
            Raylib.DrawRectangle(cx - 3, 66, 6, 18, C(205, 195, 175));
            Raylib.DrawEllipse(cx, 62, 2.5f, 5 * candle, C(255, 210, 120));
        }

        // Logs behind and under the flames
        Raylib.DrawRectangleRounded(new Rectangle(255, 308, 130, 20), 0.5f, 6, C(52, 32, 19));
        Raylib.DrawRectangleRounded(new Rectangle(275, 295, 110, 18), 0.5f, 6, C(66, 41, 24));
        fire.Draw();
        Raylib.DrawRectangleRounded(new Rectangle(242, 320, 156, 15), 0.5f, 6, C(44, 27, 16));

        // Warm light spilling into the room
        Raylib.BeginBlendMode(BlendMode.Additive);
        float f = fire.Flicker;
        Glow(320, 300, 340, C(255, 110, 30, (int)(60 * f)));
        Glow(320, 300, 160, C(255, 170, 60, (int)(55 * f)));
        foreach (int cx in new[] { 190, 455 }) Glow(cx, 62, 55, C(255, 190, 90, (int)(55 * candle)));
        Raylib.EndBlendMode();

        Vignette();
    }
}

// ---------------------------------------------------------------------------
// 2. Snowy cabin window
// ---------------------------------------------------------------------------
public class CabinScene : Scene
{
    public override string Name => "Snowy Cabin";
    public override (Layer, string, float)[] Layers => new[]
    {
        (Layer.Crackle, "Fireplace", 0.5f), (Layer.Wind, "Winter wind", 0.5f),
    };

    const int Wx = 235, Wy = 40, Ww = 340, Wh = 210;
    readonly Fire fire = new(105, 330, 56, 0.75f);
    struct Flake { public float X, Y, Speed, Size, Phase, Alpha; }
    readonly Flake[] flakes = new Flake[170];
    readonly float[] pineX = new float[9];
    readonly float[] pineH = new float[9];
    float t;

    public CabinScene()
    {
        for (int i = 0; i < flakes.Length; i++)
        {
            float depth = Rf(0, 1);
            flakes[i] = new Flake
            {
                X = Rf(Wx, Wx + Ww), Y = Rf(Wy, Wy + Wh), Speed = 12 + depth * 38,
                Size = 0.8f + depth * 1.8f, Phase = Rf(0, 6.28f), Alpha = 110 + depth * 140
            };
        }
        for (int i = 0; i < pineX.Length; i++)
        {
            pineX[i] = Wx + 25 + i * (Ww - 50) / (pineX.Length - 1) + Rf(-14, 14);
            pineH[i] = Rf(42, 80);
        }
    }

    public override void Update(float dt)
    {
        t += dt;
        fire.Update(dt);
        for (int i = 0; i < flakes.Length; i++)
        {
            ref Flake f = ref flakes[i];
            f.Y += f.Speed * dt;
            f.X += (MathF.Sin(t * 0.8f + f.Phase) * 9 + 7) * dt * (0.4f + f.Size * 0.3f);
            if (f.Y > Wy + Wh) { f.Y = Wy - 4; f.X = Rf(Wx - 20, Wx + Ww); }
            if (f.X > Wx + Ww + 6) f.X = Wx - 6;
        }
    }

    public override void Draw()
    {
        // Interior wall (wood planks) and floor
        Raylib.DrawRectangle(0, 0, W, H, C(34, 23, 16));
        for (int x = 0; x < W; x += 32)
        {
            Raylib.DrawRectangle(x, 0, 2, 300, C(20, 13, 9));
            Raylib.DrawRectangle(x + 2, 0, 1, 300, C(48, 33, 23));
        }
        Raylib.DrawRectangleGradientV(0, 300, W, 60, C(22, 15, 11), C(10, 7, 5));
        for (int y = 306; y < H; y += 14) Raylib.DrawRectangle(0, y, W, 1, C(8, 5, 4));

        // Stone hearth on the left
        Raylib.DrawRectangle(22, 200, 170, 160, C(58, 52, 50));
        for (int row = 0; row < 11; row++)
            for (int x = 22 + (row % 2) * 14; x < 192; x += 28)
                Raylib.DrawRectangleLines(x, 200 + row * 14, Math.Min(28, 192 - x), 14, C(38, 33, 31));
        Raylib.DrawRectangle(14, 192, 186, 12, C(64, 42, 28));
        Raylib.DrawRectangle(48, 252, 114, 108, C(8, 5, 4));
        Raylib.DrawRectangleRounded(new Rectangle(66, 336, 78, 14), 0.5f, 6, C(52, 32, 19));
        fire.Draw();

        // The view outside
        Raylib.BeginScissorMode(Wx, Wy, Ww, Wh);
        Raylib.DrawRectangleGradientV(Wx, Wy, Ww, (int)(Wh * 0.85f), C(10, 16, 34), C(58, 72, 100));
        Glow(Wx + Ww * 0.7f, Wy + 40, 90, C(150, 170, 210, 40));
        float groundY = Wy + Wh * 0.78f;
        Raylib.DrawEllipse(Wx + 60, (int)groundY + 18, 150, 26, C(120, 138, 168));
        Raylib.DrawEllipse(Wx + 260, (int)groundY + 22, 190, 30, C(110, 128, 158));
        Raylib.DrawRectangle(Wx, (int)groundY + 10, Ww, Wh, C(116, 134, 164));
        for (int i = 0; i < pineX.Length; i++)
        {
            float sway = MathF.Sin(t * 0.6f + i) * 1.2f;
            DrawPine(pineX[i] + sway, groundY + 6, pineH[i], pineH[i] * 0.55f, C(18, 32, 42), C(150, 166, 190), sway);
        }
        foreach (var f in flakes)
            Raylib.DrawCircleV(new Vector2(f.X, f.Y), f.Size, C(235, 242, 255, (int)f.Alpha));
        // frost creeping in from the corners
        Glow(Wx, Wy + Wh, 70, C(220, 235, 255, 70));
        Glow(Wx + Ww, Wy + Wh, 70, C(220, 235, 255, 70));
        Glow(Wx, Wy, 45, C(220, 235, 255, 40));
        Glow(Wx + Ww, Wy, 45, C(220, 235, 255, 40));
        Raylib.EndScissorMode();

        // Window frame, mullions and sill
        Raylib.DrawRectangleLinesEx(new Rectangle(Wx - 10, Wy - 10, Ww + 20, Wh + 20), 10, C(66, 44, 28));
        Raylib.DrawRectangle(Wx + Ww / 2 - 4, Wy, 8, Wh, C(60, 40, 26));
        Raylib.DrawRectangle(Wx, Wy + (int)(Wh * 0.45f) - 4, Ww, 8, C(60, 40, 26));
        Raylib.DrawRectangle(Wx - 22, Wy + Wh + 8, Ww + 44, 12, C(76, 52, 34));
        Raylib.DrawRectangle(Wx - 22, Wy + Wh + 8, Ww + 44, 2, C(104, 74, 50));

        // Lighting
        Raylib.BeginBlendMode(BlendMode.Additive);
        float fl = fire.Flicker;
        Glow(105, 300, 270, C(255, 110, 30, (int)(62 * fl)));
        Glow(105, 320, 120, C(255, 170, 60, (int)(50 * fl)));
        Glow(Wx + Ww / 2, Wy + Wh / 2, 250, C(90, 130, 190, 20));
        Raylib.EndBlendMode();

        Vignette();
    }

    static void DrawPine(float x, float baseY, float h, float w, Color body, Color snow, float sway)
    {
        Raylib.DrawRectangle((int)x - 2, (int)(baseY - 8), 4, 10, C(30, 22, 18));
        for (int k = 0; k < 4; k++)
        {
            float topY = baseY - h + k * h * 0.2f;
            float botY = topY + h * 0.38f;
            float half = w * (0.35f + 0.22f * k) * 0.5f;
            float apexX = x + sway * (1 - k * 0.2f);
            Raylib.DrawTriangle(new Vector2(apexX, topY), new Vector2(x - half, botY), new Vector2(x + half, botY), body);
            Raylib.DrawTriangle(new Vector2(apexX, topY), new Vector2(x - half * 0.45f, topY + (botY - topY) * 0.45f),
                new Vector2(x + half * 0.45f, topY + (botY - topY) * 0.45f), Lerp(body, snow, 0.55f));
        }
    }
}

// ---------------------------------------------------------------------------
// 3. Night forest
// ---------------------------------------------------------------------------
public class ForestScene : Scene
{
    public override string Name => "Night Forest";
    public override (Layer, string, float)[] Layers => new[]
    {
        (Layer.Crickets, "Crickets", 0.5f), (Layer.Wind, "Wind in leaves", 0.35f), (Layer.Birds, "Forest ambience", 0.35f),
    };

    struct Tree { public float X, H, W, Phase; }
    readonly Tree[][] layers = new Tree[3][];
    readonly Color[] layerColors = { C(16, 36, 42), C(10, 26, 31), C(5, 15, 19) };
    readonly float[] layerBase = { 248, 278, 316 };
    readonly (float x, float y, float tw)[] stars = new (float, float, float)[55];
    struct Mist { public float X, Y, W, Speed; }
    readonly Mist[] mist = new Mist[7];
    struct Fly { public float X, Y, Ang, Speed, Phase, Rate; }
    readonly Fly[] flies = new Fly[24];
    readonly float[] grassX = new float[130];
    float t;

    public ForestScene()
    {
        for (int l = 0; l < 3; l++)
        {
            var list = new List<Tree>();
            float step = 34 - l * 7;
            for (float x = -20; x < W + 30; x += step + Rf(-6, 8))
                list.Add(new Tree { X = x, H = Rf(70, 110) + l * 40, W = Rf(34, 48) + l * 14, Phase = Rf(0, 6.28f) });
            layers[l] = list.ToArray();
        }
        for (int i = 0; i < stars.Length; i++) stars[i] = (Rf(0, W), Rf(0, 170), Rf(0, 6.28f));
        for (int i = 0; i < mist.Length; i++)
            mist[i] = new Mist { X = Rf(0, W), Y = Rf(235, 310), W = Rf(180, 300), Speed = Rf(3, 9) };
        for (int i = 0; i < flies.Length; i++)
            flies[i] = new Fly { X = Rf(0, W), Y = Rf(200, 335), Ang = Rf(0, 6.28f), Speed = Rf(6, 16), Phase = Rf(0, 6.28f), Rate = Rf(0.5f, 1.3f) };
        for (int i = 0; i < grassX.Length; i++) grassX[i] = Rf(0, W);
    }

    public override void Update(float dt)
    {
        t += dt;
        for (int i = 0; i < mist.Length; i++)
        {
            mist[i].X += mist[i].Speed * dt;
            if (mist[i].X - mist[i].W > W) mist[i].X = -mist[i].W;
        }
        for (int i = 0; i < flies.Length; i++)
        {
            ref Fly f = ref flies[i];
            f.Ang += Rf(-2.2f, 2.2f) * dt;
            f.X += MathF.Cos(f.Ang) * f.Speed * dt;
            f.Y += MathF.Sin(f.Ang) * f.Speed * 0.6f * dt;
            if (f.X < -10) f.X = W + 10; else if (f.X > W + 10) f.X = -10;
            f.Y = Math.Clamp(f.Y, 190, 345);
        }
    }

    public override void Draw()
    {
        Raylib.DrawRectangleGradientV(0, 0, W, 290, C(5, 12, 26), C(30, 62, 70));
        foreach (var (x, y, tw) in stars)
        {
            int a = (int)(110 + 100 * MathF.Sin(t * 1.3f + tw));
            Raylib.DrawRectangle((int)x, (int)y, 1, 1, C(230, 240, 255, a));
        }
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(510, 72, 110, C(150, 180, 200, 40));
        Raylib.EndBlendMode();
        Raylib.DrawCircle(510, 72, 17, C(228, 236, 222));
        Raylib.DrawCircle(504, 68, 4, C(205, 214, 202));
        Raylib.DrawCircle(516, 78, 3, C(208, 217, 205));

        for (int l = 0; l < 3; l++)
        {
            foreach (var tr in layers[l])
            {
                float sway = MathF.Sin(t * (0.5f + l * 0.12f) + tr.Phase) * (1.2f + l * 1.3f);
                DrawPine(tr.X, layerBase[l], tr.H, tr.W, layerColors[l], sway);
            }
            // mist drifting between the tree layers
            Raylib.BeginBlendMode(BlendMode.Additive);
            for (int m = l; m < mist.Length; m += 3)
                Raylib.DrawEllipse((int)mist[m].X, (int)mist[m].Y, mist[m].W, 16, C(70, 100, 110, 12));
            Raylib.EndBlendMode();
        }

        // Ground and grass blades
        Raylib.DrawRectangleGradientV(0, 316, W, 44, C(4, 11, 13), C(2, 6, 7));
        foreach (float gx in grassX)
        {
            float s = MathF.Sin(t * 1.1f + gx * 0.05f) * 3;
            float h = 9 + (gx * 7 % 9);
            Raylib.DrawLineV(new Vector2(gx, 330 + (gx * 3 % 28)), new Vector2(gx + s, 330 + (gx * 3 % 28) - h), C(8, 22, 24));
        }

        // Fireflies
        Raylib.BeginBlendMode(BlendMode.Additive);
        foreach (var f in flies)
        {
            float b = MathF.Max(0, MathF.Sin(t * f.Rate * 1.6f + f.Phase));
            b *= b;
            if (b < 0.02f) continue;
            Glow(f.X, f.Y, 7, C(200, 255, 110, (int)(150 * b)));
            Raylib.DrawCircleV(new Vector2(f.X, f.Y), 1, C(255, 255, 200, (int)(255 * b)));
        }
        Raylib.EndBlendMode();

        Vignette(170);
    }

    static void DrawPine(float x, float baseY, float h, float w, Color c, float sway)
    {
        Raylib.DrawRectangle((int)x - 2, (int)(baseY - 10), 5, 40, c);
        const int levels = 5;
        for (int k = 0; k < levels; k++)
        {
            float topY = baseY - h + k * h * 0.17f;
            float botY = topY + h * 0.36f;
            float half = w * (0.3f + 0.17f * k) * 0.5f;
            float apexX = x + sway * (1 - (float)k / levels);
            Raylib.DrawTriangle(new Vector2(apexX, topY), new Vector2(x - half, botY), new Vector2(x + half, botY), c);
        }
    }
}

// ---------------------------------------------------------------------------
// 4. Rainy library
// ---------------------------------------------------------------------------
public class LibraryScene : Scene
{
    public override string Name => "Rainy Library";
    public override (Layer, string, float)[] Layers => new[]
    {
        (Layer.Rain, "Rain", 0.6f), (Layer.Thunder, "Distant thunder", 0.3f), (Layer.Clock, "Clock", 0.12f),
    };

    const int Wx = 200, Wy = 30, Ww = 240, Wh = 215;
    readonly AmbientAudio audio;
    readonly List<(Rectangle r, Color c, int band)> books = new();
    struct Streak { public float X, Y, Speed, Len; }
    readonly Streak[] streaks = new Streak[110];
    struct Drop { public float X, Y, Size, Speed, Stall; }
    readonly Drop[] drops = new Drop[46];
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

        for (int i = 0; i < streaks.Length; i++)
            streaks[i] = new Streak { X = Rf(Wx, Wx + Ww), Y = Rf(Wy, Wy + Wh), Speed = Rf(330, 520), Len = Rf(8, 20) };
        for (int i = 0; i < drops.Length; i++) drops[i] = NewDrop(true);
        for (int i = 0; i < bokeh.Length; i++)
            bokeh[i] = (Rf(Wx, Wx + Ww), Rf(Wy + 110, Wy + Wh - 25), Rf(4, 10),
                rnd.Next(3) == 0 ? C(120, 160, 220, 38) : C(255, 190, 100, 42));
    }

    static Drop NewDrop(bool anywhere) => new()
    {
        X = Rf(Wx + 4, Wx + Ww - 4), Y = anywhere ? Rf(Wy, Wy + Wh) : Wy - 2, Size = Rf(1.2f, 3.2f),
        Speed = 0, Stall = Rf(0.5f, 6f)
    };

    public override void Update(float dt)
    {
        t += dt;
        lampFlick += (1 + MathF.Sin(t * 3.1f) * 0.02f + Rf(-0.02f, 0.02f) - lampFlick) * 0.2f;

        for (int i = 0; i < streaks.Length; i++)
        {
            streaks[i].Y += streaks[i].Speed * dt;
            streaks[i].X -= 40 * dt;
            if (streaks[i].Y > Wy + Wh || streaks[i].X < Wx) { streaks[i].Y = Wy - 10; streaks[i].X = Rf(Wx, Wx + Ww + 40); }
        }
        for (int i = 0; i < drops.Length; i++)
        {
            ref Drop d = ref drops[i];
            d.Stall -= dt;
            if (d.Stall <= 0)
            {
                d.Speed = Rf(18, 55);
                d.Stall = Rf(0.3f, 4f);
                if (Gfx.R.Next(3) == 0) d.Speed = 0;
            }
            d.Y += d.Speed * dt;
            d.Speed *= 1 - Math.Min(1, dt * 1.5f);
            if (d.Y > Wy + Wh) drops[i] = NewDrop(false);
        }

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
        foreach (var s in streaks)
            Raylib.DrawLineV(new Vector2(s.X, s.Y), new Vector2(s.X + 0.12f * s.Len, s.Y - s.Len), C(170, 190, 220, 55));
        // drops on the glass
        foreach (var d in drops)
        {
            if (d.Speed > 8)
                Raylib.DrawLineV(new Vector2(d.X, d.Y - d.Speed * 0.7f), new Vector2(d.X, d.Y), C(150, 175, 210, 30));
            Raylib.DrawCircleV(new Vector2(d.X, d.Y), d.Size, C(150, 175, 210, 55));
            Raylib.DrawCircleV(new Vector2(d.X - d.Size * 0.3f, d.Y - d.Size * 0.3f), d.Size * 0.4f, C(235, 245, 255, 130));
        }
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
