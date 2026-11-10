using System.Numerics;
using Raylib_cs;
using static Gfx;

// ---------------------------------------------------------------------------
// Snowy cabin window
// ---------------------------------------------------------------------------
public class CabinScene : Scene
{
    public override string Name => "Snowy Cabin";
    protected override (Layer, string, float)[] SoundLayers => new[]
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

