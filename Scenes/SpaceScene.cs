using System.Numerics;
using Raylib_cs;
using static Gfx;

// A quiet observation window on a ship in orbit: drifting stars, soft nebula colour and a planet below.
public class SpaceScene : Scene
{
    public override string Name => "Observation Window";
    protected override (Layer, string, float)[] SoundLayers => new[]
    {
        (Layer.Hum, "Ship hum", 0.5f),
    };

    const int Wx = 70, Wy = 36, Ww = 500, Wh = 230;
    struct Star { public float X, Y, Speed, Size, Phase; }
    readonly Star[] stars = new Star[170];
    float t;

    // Planet types, one per launch: ocean world, ringed amber giant, red desert world, icy violet world.
    // Body colour, cloud-band colour, atmosphere glow, lit-side glow, whether it has a ring.
    static readonly (Color Body, Color Band, Color Air, Color Lit, bool Ring)[] Planets =
    {
        (C(20, 44, 78), C(60, 100, 150, 46), C(80, 150, 255, 46), C(90, 160, 210, 70), false),
        (C(120, 84, 48), C(190, 150, 96, 60), C(255, 190, 110, 40), C(240, 200, 140, 60), true),
        (C(96, 40, 30), C(150, 80, 56, 40), C(255, 120, 80, 36), C(230, 140, 100, 56), false),
        (C(56, 52, 108), C(130, 150, 210, 50), C(150, 130, 255, 44), C(170, 190, 255, 64), true),
    };
    readonly (Color Body, Color Band, Color Air, Color Lit, bool Ring) planet = Planets[Launch.Pick(Planets.Length)];

    public SpaceScene()
    {
        for (int i = 0; i < stars.Length; i++)
        {
            float depth = Rf(0, 1);
            stars[i] = new Star { X = Rf(Wx, Wx + Ww), Y = Rf(Wy, Wy + Wh), Speed = 1.5f + depth * 5, Size = 0.6f + depth * 1.2f, Phase = Rf(0, 6.28f) };
        }
    }

    public override void Update(float dt)
    {
        t += dt;
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].X -= stars[i].Speed * dt;
            if (stars[i].X < Wx) { stars[i].X = Wx + Ww; stars[i].Y = Rf(Wy, Wy + Wh); }
        }
    }

    // A flat ring around the planet. The far half is drawn before the planet body and the near half after it, so the planet sits inside the ring.
    void DrawRing(Vector2 c, float pr, bool back)
    {
        for (int band = 0; band < 3; band++)
        {
            float rx = pr * (1.5f + band * 0.1f), ry = rx * 0.2f;
            var col = band == 1 ? WithA(planet.Lit, 90) : WithA(planet.Band, 150);
            Vector2 prev = default; bool first = true;
            for (int s = 0; s <= 90; s++)
            {
                float a = s / 90f * MathF.PI + (back ? MathF.PI : 0);
                var p = new Vector2(c.X + MathF.Cos(a) * rx, c.Y + MathF.Sin(a) * ry);
                if (!first) Raylib.DrawLineEx(prev, p, 5, col);
                prev = p; first = false;
            }
        }
    }

    // Rare event: a comet drifts slowly across the window from the upper right, with a long pale tail (about half a minute).
    const float CometSpeed = 20;
    protected override float RareLength => (Ww + 200) / CometSpeed;
    Vector2 cometStart, cometDir;
    protected override void OnRareStart()
    {
        cometStart = new Vector2(Wx + Ww + 80, Wy + Rf(20, 110));
        cometDir = Vector2.Normalize(new Vector2(-1, Rf(0.12f, 0.3f)));
    }

    public override void Draw()
    {
        // Interior: dark panels, seams and a console strip
        Raylib.DrawRectangleGradientV(0, 0, W, H, C(26, 32, 42), C(12, 15, 22));
        for (int x = 0; x < W; x += 80) Raylib.DrawRectangle(x, 0, 2, H, C(16, 20, 28));
        for (int x = 0; x < W; x += 80) for (int y = 20; y < H; y += 60) Raylib.DrawCircle(x + 8, y, 1.5f, C(52, 62, 76));

        // The view
        Raylib.BeginScissorMode(Wx, Wy, Ww, Wh);
        Raylib.DrawRectangleGradientV(Wx, Wy, Ww, Wh, C(2, 3, 10), C(8, 10, 26));
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(Wx + 140 + MathF.Sin(t * 0.05f) * 20, Wy + 70, 190, C(120, 60, 170, 20));
        Glow(Wx + 360 + MathF.Cos(t * 0.04f) * 20, Wy + 40, 160, C(40, 120, 150, 16));
        foreach (var s in stars)
        {
            int a = (int)(150 + 100 * MathF.Sin(t * 0.9f + s.Phase));
            Raylib.DrawCircleV(new Vector2(s.X, s.Y), s.Size, C(235, 240, 255, a));
        }
        Raylib.EndBlendMode();

        if (RareT >= 0)
        {
            float ca = MathF.Min(1, RareT / 3) * MathF.Min(1, (RareLength - RareT) / 3);
            Raylib.BeginBlendMode(BlendMode.Additive);
            Streak(cometStart + cometDir * (CometSpeed * RareT), cometDir, 130, 2.4f, C(185, 218, 255), 170 * ca);
            Raylib.EndBlendMode();
        }
        // Planet rising from the bottom right: atmosphere, body, lit side, cloud bands, night side
        // Ringed planets are smaller and sit higher so the ring is visible inside the window.
        Vector2 pc = new(Wx + 360, Wy + Wh + (planet.Ring ? -5 : 120));
        float pr = planet.Ring ? 125 : 215;
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(pc.X, pc.Y, pr + 45, planet.Air);
        Raylib.EndBlendMode();
        if (planet.Ring) DrawRing(pc, pr, back: true);
        Raylib.DrawCircleV(pc, pr, planet.Body);
        for (int b = 0; b < 7; b++)
        {
            float dy = -pr + 20 + b * 22 + MathF.Sin(t * 0.1f + b) * 2;
            float half = MathF.Sqrt(MathF.Max(0, pr * pr - dy * dy));
            Raylib.DrawLineEx(new Vector2(pc.X - half, pc.Y + dy), new Vector2(pc.X + half, pc.Y + dy), 5, planet.Band);
        }
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(pc.X - 70, pc.Y - 120, pr * 0.9f, planet.Lit);
        Raylib.EndBlendMode();
        Glow(pc.X + 110, pc.Y + 40, 260, C(0, 0, 10, 190)); // night side
        if (planet.Ring) DrawRing(pc, pr, back: false);
        Raylib.EndScissorMode();

        // Window frame with rivets
        Raylib.DrawRectangleLinesEx(new Rectangle(Wx - 14, Wy - 14, Ww + 28, Wh + 28), 14, C(58, 70, 86));
        Raylib.DrawRectangleLinesEx(new Rectangle(Wx - 2, Wy - 2, Ww + 4, Wh + 4), 2, C(30, 38, 50));
        for (int x = Wx - 8; x <= Wx + Ww + 8; x += 40)
        {
            Raylib.DrawCircle(x, Wy - 8, 2, C(110, 124, 142));
            Raylib.DrawCircle(x, Wy + Wh + 8, 2, C(110, 124, 142));
        }

        // An LED clock on the wall beside the window (silent): a dark housing with red seven-segment digits
        Raylib.DrawRectangle(586, 58, 52, 32, C(6, 7, 10));
        Raylib.DrawRectangleLines(586, 58, 52, 32, C(48, 58, 72));
        Clocks.DrawLed(591, 65, 8, 17, C(255, 62, 40));

        // Console strip with slowly pulsing lights, and a mug
        Raylib.DrawRectangle(0, 292, W, 68, C(20, 25, 34));
        Raylib.DrawRectangle(0, 290, W, 3, C(70, 84, 102));
        Raylib.DrawRectangle(300, 276, 14, 14, C(200, 196, 186));
        Raylib.BeginBlendMode(BlendMode.Additive);
        for (int i = 0; i < 12; i++)
        {
            float a = 0.5f + 0.5f * MathF.Sin(t * (0.3f + i * 0.07f) + i * 1.7f);
            Color c = i % 3 == 0 ? C(255, 170, 60) : i % 3 == 1 ? C(80, 220, 150) : C(90, 160, 255);
            float x = 60 + i * 46;
            Raylib.DrawRectangle((int)x, 316, 6, 4, WithA(c, 150 + 100 * a));
            Glow(x + 3, 318, 12, WithA(c, 50 * a));
        }
        Glow(Wx + Ww / 2, Wy + Wh / 2, 280, C(70, 100, 170, 16));
        Raylib.EndBlendMode();

        Vignette(170);
    }
}
