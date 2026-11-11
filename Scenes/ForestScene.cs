using System.Numerics;
using Raylib_cs;
using static Gfx;

// ---------------------------------------------------------------------------
// Night forest
// ---------------------------------------------------------------------------
public class ForestScene : Scene
{
    public override string Name => "Night Forest";
    protected override (Layer, string, float)[] SoundLayers => new[]
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

}

