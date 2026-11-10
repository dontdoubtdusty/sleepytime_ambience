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

        // Planet rising from the bottom right: atmosphere, body, lit side, cloud bands, night side
        Vector2 pc = new(Wx + 360, Wy + Wh + 120);
        const float pr = 215;
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(pc.X, pc.Y, pr + 45, C(80, 150, 255, 46));
        Raylib.EndBlendMode();
        Raylib.DrawCircleV(pc, pr, C(20, 44, 78));
        for (int b = 0; b < 7; b++)
        {
            float dy = -pr + 20 + b * 22 + MathF.Sin(t * 0.1f + b) * 2;
            float half = MathF.Sqrt(MathF.Max(0, pr * pr - dy * dy));
            Raylib.DrawLineEx(new Vector2(pc.X - half, pc.Y + dy), new Vector2(pc.X + half, pc.Y + dy), 5, C(60, 100, 150, 46));
        }
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(pc.X - 70, pc.Y - 120, pr * 0.9f, C(90, 160, 210, 70));
        Raylib.EndBlendMode();
        Glow(pc.X + 110, pc.Y + 40, 260, C(0, 0, 10, 190)); // night side
        Raylib.EndScissorMode();

        // Window frame with rivets
        Raylib.DrawRectangleLinesEx(new Rectangle(Wx - 14, Wy - 14, Ww + 28, Wh + 28), 14, C(58, 70, 86));
        Raylib.DrawRectangleLinesEx(new Rectangle(Wx - 2, Wy - 2, Ww + 4, Wh + 4), 2, C(30, 38, 50));
        for (int x = Wx - 8; x <= Wx + Ww + 8; x += 40)
        {
            Raylib.DrawCircle(x, Wy - 8, 2, C(110, 124, 142));
            Raylib.DrawCircle(x, Wy + Wh + 8, 2, C(110, 124, 142));
        }

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
