using System.Numerics;
using Raylib_cs;
using static Gfx;

// Inside a tent in the rain: warm lantern light on the canvas, a sleeping bag and boots,
// and the wet forest night visible through the open flap.
public class TentScene : Scene
{
    public override string Name => "Tent in the Rain";
    protected override (Layer, string, float)[] SoundLayers => new[]
    {
        (Layer.Rain, "Rain on canvas", 0.6f), (Layer.Wind, "Wind", 0.2f), (Layer.Crickets, "Crickets", 0.0f),
    };

    static readonly Vector2 Apex = new(320, 30), BL = new(190, 360), BR = new(450, 360);
    readonly RainWindow rain = new(0, 0, 640, 360, 150, 0);
    readonly float[] treeX = new float[16];
    readonly float[] treeH = new float[16];
    struct Run { public float X, Y, Speed, Life; }
    readonly List<Run> runs = new();
    float t, lantern = 1, runAcc;

    public TentScene()
    {
        for (int i = 0; i < treeX.Length; i++) { treeX[i] = 190 + i * 17 + Rf(-6, 6); treeH[i] = Rf(90, 170); }
    }

    public override void Update(float dt)
    {
        t += dt;
        rain.Update(dt);
        lantern += (1 + MathF.Sin(t * 2.7f) * 0.03f + Rf(-0.04f, 0.04f) - lantern) * 0.15f;

        // Drops running down the inside of the canvas now and then.
        runAcc += dt * 0.6f;
        while (runAcc >= 1) { runAcc--; runs.Add(new Run { X = Rf(20, 600), Y = Rf(40, 120), Speed = Rf(10, 22), Life = 0 }); }
        for (int i = runs.Count - 1; i >= 0; i--)
        {
            var r = runs[i];
            r.Life += dt; r.Y += r.Speed * dt;
            if (r.Y > 330 || r.Life > 12) runs.RemoveAt(i); else runs[i] = r;
        }
    }

    public override void Draw()
    {
        // The night outside, seen through the flap
        Raylib.DrawRectangleGradientV(0, 0, W, H, C(8, 12, 22), C(26, 36, 44));
        for (int i = 0; i < treeX.Length; i++)
        {
            float x = treeX[i], h = treeH[i], sway = MathF.Sin(t * 0.5f + i) * 2;
            for (int k = 0; k < 4; k++)
            {
                float top = 330 - h + k * h * 0.2f, bot = top + h * 0.36f, half = (14 + k * 8) * 0.9f;
                Tri(new Vector2(x + sway * (1 - k * 0.25f), top), new Vector2(x - half, bot), new Vector2(x + half, bot), C(6, 14, 16));
            }
        }
        Raylib.DrawRectangleGradientV(0, 300, W, 60, C(8, 16, 16), C(4, 8, 8));
        rain.DrawStreaks();

        // Canvas: everything around the opening
        Color canvas = C(76, 56, 38);
        Raylib.DrawRectangle(0, 0, W, 30, canvas);
        Tri(new Vector2(0, 30), new Vector2(320, 30), BL, C(70, 51, 35));
        Tri(new Vector2(0, 30), BL, new Vector2(0, 360), C(52, 38, 27));
        Tri(new Vector2(640, 30), new Vector2(320, 30), BR, C(70, 51, 35));
        Tri(new Vector2(640, 30), BR, new Vector2(640, 360), C(52, 38, 27));
        Raylib.DrawRectangleGradientV(0, 30, 640, 330, C(0, 0, 0, 0), C(10, 6, 4, 120));
        // seams and ties
        Raylib.DrawLineEx(new Vector2(0, 30), new Vector2(190, 360), 2, C(36, 26, 18));
        Raylib.DrawLineEx(new Vector2(640, 30), new Vector2(450, 360), 2, C(36, 26, 18));
        Raylib.DrawLineEx(new Vector2(100, 30), new Vector2(95, 360), 1, C(40, 29, 20));
        Raylib.DrawLineEx(new Vector2(540, 30), new Vector2(545, 360), 1, C(40, 29, 20));
        Raylib.DrawLineEx(new Vector2(320, 0), new Vector2(320, 30), 3, C(36, 26, 18));
        // drops running on the canvas
        foreach (var r in runs)
        {
            Raylib.DrawLineV(new Vector2(r.X, r.Y - 12), new Vector2(r.X, r.Y), C(180, 150, 110, 30));
            Raylib.DrawCircleV(new Vector2(r.X, r.Y), 1.8f, C(200, 170, 130, 70));
        }

        // Floor: sleeping bag, boots, mug
        Raylib.DrawRectangleGradientV(0, 318, W, 42, C(30, 22, 16), C(16, 12, 9));
        Raylib.DrawRectangleRounded(new Rectangle(40, 296, 250, 52), 0.45f, 8, C(36, 44, 70));
        Raylib.DrawRectangleRounded(new Rectangle(40, 296, 70, 52), 0.45f, 8, C(58, 66, 94));
        for (int i = 0; i < 4; i++) Raylib.DrawLineEx(new Vector2(130 + i * 40, 300), new Vector2(125 + i * 40, 344), 1, C(36, 48, 70));
        Raylib.DrawRectangleRounded(new Rectangle(330, 322, 16, 24), 0.4f, 6, C(60, 40, 28));
        Raylib.DrawRectangleRounded(new Rectangle(350, 326, 16, 20), 0.4f, 6, C(60, 40, 28));
        Raylib.DrawRectangle(520, 328, 14, 16, C(200, 196, 186));

        // Lantern hanging from the apex
        Raylib.DrawLineEx(new Vector2(320, 30), new Vector2(320, 96), 1, C(30, 24, 20));
        Raylib.DrawRectangle(310, 96, 20, 6, C(30, 26, 24));
        Raylib.DrawRectangle(311, 102, 18, 26, C(255, 214, 130));
        Raylib.DrawRectangle(310, 128, 20, 4, C(30, 26, 24));

        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(320, 114, 330, C(255, 170, 70, (int)(46 * lantern)));
        Glow(320, 114, 110, C(255, 205, 120, (int)(52 * lantern)));
        Raylib.EndBlendMode();

        Vignette(170);
    }
}
