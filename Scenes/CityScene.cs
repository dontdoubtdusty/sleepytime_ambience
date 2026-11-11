using System.Numerics;
using Raylib_cs;
using static Gfx;

// A rainy city window at night: layered skyline, slow neon glows, a few cars far below, and a small
// plant and a lamp in the room.
public class CityScene : Scene
{
    public override string Name => "City Window";
    protected override (Layer, string, float)[] SoundLayers => new[]
    {
        (Layer.Rain, "Rain", 0.55f), (Layer.Wind, "Wind", 0.08f),
    };

    const int Wx = 150, Wy = 34, Ww = 340, Wh = 220;
    struct Building { public float X, W, H; public int Layer; }
    readonly List<Building> buildings = new();
    readonly bool[] lit = new bool[900];
    readonly RainWindow rain = new(Wx, Wy, Ww, Wh, 120, 50);
    struct Car { public float X, Speed; public bool Red; }
    readonly Car[] cars = new Car[7];
    // Neon sign colours for this launch: (first sign, second sign).
    static readonly (Color A, Color B)[] Neons =
    {
        (C(255, 80, 160), C(60, 220, 230)), (C(255, 170, 60), C(90, 230, 150)), (C(170, 110, 255), C(255, 120, 80)), (C(255, 70, 70), C(90, 150, 255)),
    };
    readonly (Color A, Color B) neon = Neons[Launch.Pick(Neons.Length)];
    float t, lampFlick = 1, toggleTimer;

    public CityScene()
    {
        for (int layer = 0; layer < 2; layer++)
        {
            float x = Wx - 10;
            while (x < Wx + Ww + 10)
            {
                float w = Rf(24, 50), h = layer == 0 ? Rf(60, 150) : Rf(40, 100);
                buildings.Add(new Building { X = x, W = w, H = h, Layer = layer });
                x += w + Rf(0, 6);
            }
        }
        for (int i = 0; i < lit.Length; i++) lit[i] = R.NextDouble() < 0.4;
        for (int i = 0; i < cars.Length; i++)
            cars[i] = new Car { X = Rf(Wx, Wx + Ww), Speed = Rf(10, 26) * (R.Next(2) == 0 ? -1 : 1), Red = R.Next(2) == 0 };
    }

    public override void Update(float dt)
    {
        t += dt;
        rain.Update(dt);
        lampFlick += (1 + Rf(-0.02f, 0.02f) - lampFlick) * 0.2f;
        toggleTimer -= dt;
        if (toggleTimer <= 0) { lit[R.Next(lit.Length)] ^= true; toggleTimer = Rf(0.3f, 1.5f); }
        for (int i = 0; i < cars.Length; i++)
        {
            cars[i].X += cars[i].Speed * dt;
            if (cars[i].X < Wx - 6) cars[i].X = Wx + Ww + 6;
            if (cars[i].X > Wx + Ww + 6) cars[i].X = Wx - 6;
        }
    }

    // Rare event: a few small, soft fireworks go off far away over the skyline (about 14 seconds).
    protected override float RareLength => 14;
    readonly (float X, float Y, float At, Color Col)[] bursts = new (float, float, float, Color)[5];
    protected override void OnRareStart()
    {
        Color[] cols = { C(255, 190, 120), C(150, 220, 255), C(255, 150, 190), C(190, 255, 170), C(220, 190, 255) };
        for (int i = 0; i < bursts.Length; i++)
            bursts[i] = (Rf(Wx + 40, Wx + Ww - 40), Rf(Wy + 25, Wy + 100), i * 2.3f + Rf(0, 1.2f), cols[R.Next(cols.Length)]);
    }

    // Each burst: 26 sparks fly out, slow down, droop and fade over 2.4 seconds.
    void DrawFireworks()
    {
        Raylib.BeginBlendMode(BlendMode.Additive);
        foreach (var b in bursts)
        {
            float age = RareT - b.At;
            if (age < 0 || age > 2.4f) continue;
            float fade = 1 - age / 2.4f, r = 24 * (1 - MathF.Exp(-age * 2.2f));
            for (int i = 0; i < 26; i++)
            {
                float ang = i / 26f * 2 * MathF.PI + MathF.Sin(i * 12.9f) * 0.12f, rr = r * (0.8f + 0.2f * MathF.Sin(i * 7.1f));
                var p = new Vector2(b.X + MathF.Cos(ang) * rr, b.Y + MathF.Sin(ang) * rr + age * age * 5);
                Raylib.DrawCircleV(p, 1.1f, WithA(b.Col, 200 * fade * fade));
            }
            Glow(b.X, b.Y, 26, WithA(b.Col, 26 * fade * fade));
        }
        Raylib.EndBlendMode();
    }

    public override void Draw()
    {
        // Room
        Raylib.DrawRectangleGradientV(0, 0, W, H, C(30, 22, 24), C(14, 10, 12));

        // The view
        int groundY = Wy + Wh - 22;
        Raylib.BeginScissorMode(Wx, Wy, Ww, Wh);
        Raylib.DrawRectangleGradientV(Wx, Wy, Ww, Wh, C(18, 14, 38), C(78, 50, 82));
        if (RareT >= 0) DrawFireworks();
        int idx = 0;
        foreach (var b in buildings)
        {
            Color body = b.Layer == 0 ? C(40, 30, 62) : C(22, 16, 38);
            int top = groundY - (int)b.H;
            Raylib.DrawRectangle((int)b.X, top, (int)b.W, (int)b.H + 20, body);
            for (int wy = top + 6; wy < groundY - 6; wy += 9)
                for (int wx = (int)b.X + 4; wx < b.X + b.W - 5; wx += 8)
                {
                    if (lit[idx++ % lit.Length])
                        Raylib.DrawRectangle(wx, wy, 3, 4, b.Layer == 0 ? C(255, 200, 110, 190) : C(210, 160, 100, 120));
                }
        }
        // Road and cars
        Raylib.DrawRectangle(Wx, groundY, Ww, 30, C(12, 10, 20));
        // A clock tower in the distance with a softly lit face (silent)
        Raylib.DrawRectangle(Wx + 214, groundY - 170, 24, 170, C(26, 20, 44));
        Tri(new Vector2(Wx + 212, groundY - 170), new Vector2(Wx + 240, groundY - 170), new Vector2(Wx + 226, groundY - 192), C(26, 20, 44));
        Clocks.DrawFace(Wx + 226, groundY - 152, 8, C(60, 46, 70), C(226, 206, 150), C(50, 36, 30), null);

        Raylib.BeginBlendMode(BlendMode.Additive);
        foreach (var c in cars)
        {
            Color col = c.Red ? C(255, 60, 50, 150) : C(255, 240, 200, 150);
            Glow(c.X, groundY + 8, 8, col);
            Raylib.DrawRectangle((int)c.X - 1, groundY + 8, 2, 1, WithA(col, 255));
        }
        // Neon glows and a slow aircraft beacon
        float pulse = 0.75f + 0.25f * MathF.Sin(t * 0.7f);
        Glow(Wx + 90, groundY - 70, 38, WithA(neon.A, 70 * pulse));
        Glow(Wx + 250, groundY - 50, 34, WithA(neon.B, 60 * (1.5f - pulse)));
        if (MathF.Sin(t * 1.4f) > 0.6f) Glow(Wx + 200, groundY - 150, 9, C(255, 40, 40, 200));
        Raylib.EndBlendMode();
        rain.DrawStreaks();
        rain.DrawGlassDrops();
        Raylib.EndScissorMode();

        // Window frame and sill
        Raylib.DrawRectangleLinesEx(new Rectangle(Wx - 9, Wy - 9, Ww + 18, Wh + 18), 9, C(60, 42, 34));
        Raylib.DrawRectangle(Wx + Ww / 2 - 3, Wy, 6, Wh, C(54, 38, 30));
        Raylib.DrawRectangle(Wx - 24, Wy + Wh + 9, Ww + 48, 12, C(78, 56, 42));

        // Plant and mug on the sill
        Raylib.DrawRectangle(Wx + 20, Wy + Wh - 14, 26, 24, C(150, 80, 56));
        for (int l = 0; l < 5; l++)
        {
            float a = -1.2f + l * 0.6f;
            Raylib.DrawEllipse((int)(Wx + 33 + MathF.Sin(a) * 14), (int)(Wy + Wh - 22 - MathF.Cos(a) * 12), 6, 12, C(40, 110 + l * 6, 64));
        }
        Raylib.DrawRectangle(Wx + Ww - 60, Wy + Wh - 4, 14, 14, C(206, 200, 190));

        // Lamp light from the room
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(60, 300, 260, C(255, 175, 80, (int)(46 * lampFlick)));
        Glow(Wx + Ww / 2, Wy + Wh / 2, 260, C(120, 80, 170, 14));
        Raylib.EndBlendMode();

        Vignette(170);
    }
}
