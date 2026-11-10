using System.Numerics;
using Raylib_cs;
using static Gfx;

// Night train: a compartment window onto slowly passing hills, village lights and telephone poles,
// with a tiny rocking motion and a warm lamp on the table.
public class TrainScene : Scene
{
    public override string Name => "Night Train";
    protected override (Layer, string, float)[] SoundLayers => new[]
    {
        (Layer.Train, "Train rumble", 0.6f),
    };

    const int Wx = 180, Wy = 50, Ww = 280, Wh = 170;
    const float Period = 1200; // world width the hills and lights repeat over
    readonly (float x, float y, float tw)[] stars = new (float, float, float)[50];
    readonly float[] lightX = new float[26];
    readonly float[] lightY = new float[26];
    readonly float[] lightOn = new float[26];
    float t, scroll, flick = 1;

    public TrainScene()
    {
        for (int i = 0; i < stars.Length; i++) stars[i] = (Rf(Wx, Wx + Ww), Rf(Wy, Wy + 80), Rf(0, 6.28f));
        for (int i = 0; i < lightX.Length; i++) { lightX[i] = Rf(0, Period); lightY[i] = Rf(8, 26); lightOn[i] = Rf(0.5f, 1); }
    }

    public override void Update(float dt)
    {
        t += dt;
        scroll += dt * 40;
        flick += (1 + Rf(-0.03f, 0.03f) - flick) * 0.2f;
    }

    // Height of a rolling hill line at a world x position.
    static float Hill(float wx, float baseY, float a, float f1, float f2, float ph)
        => baseY - a * (0.6f * MathF.Sin(wx * f1 + ph) + 0.4f * MathF.Sin(wx * f2 + ph * 2.3f));

    void DrawHills(float speed, float baseY, float a, float f1, float f2, float ph, Color c)
    {
        for (int sx = Wx; sx < Wx + Ww; sx += 2)
        {
            float top = Hill(sx + scroll * speed, baseY, a, f1, f2, ph);
            Raylib.DrawRectangle(sx, (int)top, 2, Wy + Wh - (int)top + 4, c);
        }
    }

    public override void Draw()
    {
        // Compartment: wall, panelling, bench
        Raylib.DrawRectangleGradientV(0, 0, W, H, C(44, 29, 23), C(26, 17, 14));
        for (int y = 40; y < 300; y += 26) Raylib.DrawRectangle(0, y, W, 1, C(30, 19, 15));
        Raylib.DrawRectangleGradientV(0, 290, W, 70, C(84, 30, 36), C(40, 14, 18));
        for (int x = 20; x < W; x += 40) Raylib.DrawCircle(x, 320, 1.5f, C(130, 60, 60));
        Raylib.DrawRectangle(0, 286, W, 6, C(62, 40, 28));

        float sway = MathF.Sin(t * 2.3f) * 0.5f + MathF.Sin(t * 5.1f) * 0.2f;

        // The view outside
        Raylib.BeginScissorMode(Wx, Wy, Ww, Wh);
        Raylib.DrawRectangleGradientV(Wx, Wy, Ww, Wh, C(6, 8, 22), C(44, 42, 74));
        foreach (var (x, y, tw) in stars)
            Raylib.DrawRectangle((int)x, (int)y, 1, 1, C(230, 235, 255, (int)(120 + 100 * MathF.Sin(t * 1.2f + tw))));
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(Wx + 215, Wy + 40, 70, C(160, 180, 220, 36));
        Raylib.EndBlendMode();
        Raylib.DrawCircle(Wx + 215, Wy + 40, 11, C(226, 232, 222));

        Raylib.BeginMode2D(new Camera2D(Vector2.Zero, new Vector2(0, sway), 0, 1));
        DrawHills(0.15f, Wy + 125, 22, 0.011f, 0.027f, 1.3f, C(24, 26, 56));
        DrawHills(0.4f, Wy + 140, 20, 0.017f, 0.041f, 4.1f, C(16, 18, 40));
        // Village lights on the middle hills
        Raylib.BeginBlendMode(BlendMode.Additive);
        for (int i = 0; i < lightX.Length; i++)
        {
            float sx = ((lightX[i] - scroll * 0.4f) % Period + Period) % Period - 300 + Wx;
            if (sx < Wx - 4 || sx > Wx + Ww + 4) continue;
            float top = Hill(sx + scroll * 0.4f, Wy + 140, 20, 0.017f, 0.041f, 4.1f) + lightY[i] * 0.4f;
            Glow(sx, top, 5, C(255, 190, 90, (int)(130 * lightOn[i])));
            Raylib.DrawRectangle((int)sx, (int)top, 1, 1, C(255, 230, 170));
        }
        Raylib.EndBlendMode();
        DrawHills(1.0f, Wy + 160, 14, 0.022f, 0.06f, 2.2f, C(8, 10, 22));

        // Telephone poles and sagging wires
        const float poleGap = 260;
        for (int i = -1; i < 3; i++)
        {
            float px = Wx + ((i * poleGap - scroll * 2.4f) % (poleGap * 3) + poleGap * 3) % (poleGap * 3) - poleGap;
            Raylib.DrawRectangle((int)px - 2, Wy + 70, 4, 100, C(10, 8, 12));
            Raylib.DrawRectangle((int)px - 12, Wy + 76, 24, 3, C(10, 8, 12));
            for (int wire = -1; wire <= 1; wire += 2)
            {
                Vector2 prev = new(px + wire * 10, Wy + 78);
                for (int s = 1; s <= 10; s++)
                {
                    float u = s / 10f;
                    var p = new Vector2(px + wire * 10 + u * poleGap, Wy + 78 + MathF.Sin(u * MathF.PI) * 10);
                    Raylib.DrawLineV(prev, p, C(14, 12, 18));
                    prev = p;
                }
            }
        }
        Raylib.EndMode2D();
        Raylib.EndScissorMode();

        // Curtains and frame
        Raylib.DrawRectangleGradientH(Wx - 52, Wy - 18, 56, Wh + 56, C(88, 28, 36), C(60, 18, 26));
        Raylib.DrawRectangleGradientH(Wx + Ww - 4, Wy - 18, 56, Wh + 56, C(60, 18, 26), C(88, 28, 36));
        for (int k = 0; k < 5; k++)
        {
            Raylib.DrawRectangle(Wx - 48 + k * 11, Wy - 18, 2, Wh + 56, C(46, 12, 20, 140));
            Raylib.DrawRectangle(Wx + Ww + 2 + k * 11, Wy - 18, 2, Wh + 56, C(46, 12, 20, 140));
        }
        Raylib.DrawRectangleLinesEx(new Rectangle(Wx - 6, Wy - 6, Ww + 12, Wh + 12), 6, C(70, 46, 30));
        Raylib.DrawRectangle(Wx - 62, Wy - 26, Ww + 124, 8, C(76, 52, 34));

        // Table, teacup and lamp
        Raylib.DrawRectangle(Wx - 30, Wy + Wh + 14, Ww + 60, 12, C(86, 58, 38));
        Raylib.DrawRectangle(Wx - 30, Wy + Wh + 14, Ww + 60, 2, C(116, 82, 54));
        Raylib.DrawRectangle(Wx + 60, Wy + Wh - 2, 14, 16, C(214, 208, 198));
        Raylib.DrawRectangle(Wx + 214, Wy + Wh - 22, 3, 36, C(40, 30, 22));
        Tri(new Vector2(Wx + 215, Wy + Wh - 40), new Vector2(Wx + 201, Wy + Wh - 18), new Vector2(Wx + 229, Wy + Wh - 18), C(214, 170, 100));

        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(Wx + 215, Wy + Wh - 20, 240, C(255, 175, 80, (int)(58 * flick)));
        Glow(Wx + 215, Wy + Wh - 20, 70, C(255, 200, 120, (int)(55 * flick)));
        Glow(Wx + Ww / 2, Wy + Wh / 2, 230, C(90, 100, 170, 14));
        Raylib.EndBlendMode();

        Vignette(165);
    }
}
