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

    // The moon for this launch: size and tint (pale silver, harvest gold, cool blue, small and bright).
    static readonly (float R, Color Face, Color Halo)[] Moons =
    {
        (17, C(228, 236, 222), C(150, 180, 200)), (22, C(240, 214, 150), C(210, 170, 90)),
        (14, C(206, 222, 242), C(130, 160, 220)), (26, C(222, 214, 226), C(170, 150, 200)),
    };
    readonly (float R, Color Face, Color Halo) moon = Moons[Launch.Pick(Moons.Length)];

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

    // Rare event: a deer walks slowly through the clearing in front of the dark trees, stops to graze, and walks on (about 55 seconds).
    const float HopSpeed = 22, SitAt = 12, SitLen = 4;
    protected override float RareLength => (W + 80) / HopSpeed + SitLen;
    int rabbitDir = 1;
    protected override void OnRareStart() => rabbitDir = R.Next(2) == 0 ? 1 : -1;

    protected override bool AnimalsVisible => RareT >= 0;
    protected override void DrawAnimals() => DrawRabbit();

    // Rare event: a rabbit hops across the clearing, sits up for a moment, then hops on (about 33 seconds).
    void DrawRabbit()
    {
        if (HideAnimals) return;
        float d = rabbitDir;
        bool sitting = RareT >= SitAt && RareT < SitAt + SitLen;
        float walked = RareT < SitAt ? RareT : sitting ? SitAt : RareT - SitLen;
        float x = (d > 0 ? -40 : W + 40) + d * HopSpeed * walked;
        float y = 306;                                           // where the feet touch the grass
        float lift = sitting ? 0 : MathF.Abs(MathF.Sin(walked * 2.9f)) * 9;   // each hop is a small arc
        if (!Sprites.Begin(0, new Vector2(x, y - 20), 90, 80)) return;
        Color fur = C(78, 80, 86), rim = C(130, 138, 150), white = C(196, 198, 204);
        Rlgl.PushMatrix(); Rlgl.Translatef(x, y, 0); Rlgl.Scalef(2f, 2f, 1); Rlgl.Translatef(-x, -y, 0);   // drawn 2x
        float by = y - 6 - lift;
        Raylib.DrawCircleV(new Vector2(x - d * 9, by - 1), 2.2f, white);                        // cotton tail
        Raylib.DrawEllipse((int)x, (int)by, sitting ? 7 : 9, sitting ? 8 : 6, fur);             // round body
        Raylib.DrawEllipse((int)(x + d * 3), (int)(by + 4), 6, 2.5f, fur);                      // back foot
        float hy = sitting ? by - 9 : by - 3;
        var head = new Vector2(x + d * (sitting ? 5 : 9), hy);
        Raylib.DrawCircleV(head, 4.2f, fur);
        float twitch = sitting ? MathF.Sin(RareT * 6) * 1.2f : 0;                                // ears twitch while it sits
        Raylib.DrawLineEx(head + new Vector2(-d, -3), head + new Vector2(-d * 3 + twitch, -12), 2.4f, fur);
        Raylib.DrawLineEx(head + new Vector2(d * 1.5f, -3), head + new Vector2(d * 0.5f + twitch, -12), 2.4f, fur);
        Raylib.DrawCircleV(head + new Vector2(d * 4, 1), 0.9f, white);                           // nose
        Raylib.DrawLineEx(new Vector2(x - d * 6, by - 5), new Vector2(x + d * 5, by - 6), 1, rim);   // moonlit back
        Rlgl.PopMatrix();
        Sprites.End();
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
        Glow(510, 72, 80 + moon.R * 2, WithA(moon.Halo, 40));
        Raylib.EndBlendMode();
        Raylib.DrawCircle(510, 72, moon.R, moon.Face);
        Raylib.DrawCircle((int)(510 - moon.R * 0.35f), (int)(72 - moon.R * 0.25f), moon.R * 0.24f, Shade(moon.Face, 0.9f));
        Raylib.DrawCircle((int)(510 + moon.R * 0.35f), (int)(72 + moon.R * 0.35f), moon.R * 0.18f, Shade(moon.Face, 0.91f));

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

        if (RareT >= 0) DrawRabbit();

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

