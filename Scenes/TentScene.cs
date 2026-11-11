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
        (Layer.TentRain, "Rain inside the tent", 0.6f), (Layer.Wind, "Wind", 0.2f), (Layer.Crickets, "Crickets", 0.0f),
    };

    static readonly Vector2 Apex = new(320, 30), BL = new(190, 360), BR = new(450, 360);
    readonly RainWindow rain = new(0, 0, 640, 360, 150, 0);
    readonly float[] treeX = new float[16];
    readonly float[] treeH = new float[16];
    struct Run { public float X, Y, Speed, Life; }
    readonly List<Run> runs = new();
    float t, lantern = 1, runAcc;

    // Canvas colour for this launch (sand, olive, rust, slate blue); the seams and shadows are shades of it.
    static readonly Color[] Canvases = { C(76, 56, 38), C(66, 74, 42), C(112, 60, 36), C(48, 64, 88) };
    readonly Color canvas = Canvases[Launch.Pick(Canvases.Length)];

    public TentScene()
    {
        for (int i = 0; i < treeX.Length; i++) { treeX[i] = 190 + i * 17 + Rf(-6, 6); treeH[i] = Rf(90, 170); }
    }

    // How far a point is outside the tent opening (the triangle from the apex down to the two bottom corners): negative means inside the gap.
    static float CanvasClearance(float x, float y) => MathF.Abs(x - Apex.X) - (BR.X - Apex.X) * (y - Apex.Y) / (BR.Y - Apex.Y);

    public override void Update(float dt)
    {
        t += dt;
        rain.Update(dt);
        lantern += (1 + MathF.Sin(t * 2.7f) * 0.03f + Rf(-0.04f, 0.04f) - lantern) * 0.15f;

        // Drops running down the inside of the canvas now and then.
        runAcc += dt * 0.6f;
        while (runAcc >= 1)
        {
            runAcc--;
            float x = Rf(20, 600), y = Rf(40, 120);
            if (CanvasClearance(x, y) > 40) runs.Add(new Run { X = x, Y = y, Speed = Rf(10, 22), Life = 0 }); // not in or near the opening
        }
        for (int i = runs.Count - 1; i >= 0; i--)
        {
            var r = runs[i];
            r.Life += dt; r.Y += r.Speed * dt;
            // a drop that reaches the opening is gone
            if (r.Y > 330 || r.Life > 12 || CanvasClearance(r.X, r.Y) < 3) runs.RemoveAt(i);
            else runs[i] = r;
        }
    }

    // Rare event: a raccoon wanders past the tent opening, stops to sniff, and goes on its way (about 22 seconds).
    // Only its outline shows against the dark, with a glint of lantern light in its eye; the canvas hides it at the edges.
    const float RaccoonSpeed = 15, SniffAt = 8, SniffLen = 3.5f;
    protected override float RareLength => 300 / RaccoonSpeed + SniffLen;
    int coonDir = 1;
    protected override void OnRareStart() => coonDir = R.Next(2) == 0 ? 1 : -1;

    protected override bool AnimalsVisible => RareT >= 0;
    protected override void DrawAnimals() => DrawRaccoon();

    void DrawRaccoon()
    {
        if (HideAnimals) return;
        float d = coonDir;
        bool sniff = RareT >= SniffAt && RareT < SniffAt + SniffLen;
        float walked = RareT < SniffAt ? RareT : sniff ? SniffAt : RareT - SniffLen;
        float x = (d > 0 ? 170 : 470) + d * RaccoonSpeed * walked;
        float y = 300;
        Color body = C(84, 86, 94), fur = C(120, 122, 130), dark = C(40, 42, 50), pale = C(188, 190, 194), rim = C(150, 154, 164);
        if (!Sprites.Begin(0, new Vector2(x - d * 17, y - 8), 160, 76)) return;
        Rlgl.PushMatrix(); Rlgl.Translatef(x, y, 0); Rlgl.Scalef(2.2f, 2.2f, 1); Rlgl.Translatef(-x, -y, 0);   // drawn 2.2x
        Vector2 P(float u, float v) => new(x + d * u, y + v);                                                   // u forward, v down, in raccoon units
        float ph = t * 4.5f;
        // Fluffy tail first: a chain of soft rings (dark bands, grey bands, dark tip) trailing low behind the rump.
        for (int i = 6; i >= 0; i--)
            Raylib.DrawCircleV(P(-12 - i * 3.1f, -7 + i * 0.9f + (sniff ? 0 : MathF.Sin(t * 1.6f + i * 0.5f) * 0.5f)), 3.9f - i * 0.2f, i == 6 || i % 2 == 1 ? dark : fur);
        // Far legs, a little darker, then the arched body (high rump, lower shoulders) with a lit back and a pale belly.
        foreach (var (u, sw) in new[] { (-7f, 1f), (8f, -1f) })
        {
            float px = u + (sniff ? 0 : MathF.Sin(ph + (sw > 0 ? 3.1f : 0)) * 2.5f);
            Raylib.DrawLineEx(P(u, -3), P(px, 5), 2.6f, Shade(body, 0.7f));
            Raylib.DrawCircleV(P(px + 0.4f, 5), 1.8f, Shade(dark, 0.8f));
        }
        Raylib.DrawEllipseV(P(-4, -7.5f), 9.5f, 7.5f, body);
        Raylib.DrawEllipseV(P(5, -6.2f), 8.5f, 6.4f, body);
        Raylib.DrawEllipseV(P(0, -9.5f), 11, 3.6f, fur);                                                          // grizzled lighter back
        Raylib.DrawEllipseV(P(1, -2.2f), 9, 2.4f, Shade(pale, 0.7f));                                              // belly
        // Near legs with dark paws.
        foreach (var (u, sw) in new[] { (-5f, 0f), (10f, 3.1f) })
        {
            float px = u + (sniff ? 0 : MathF.Sin(ph + sw) * 2.5f);
            Raylib.DrawLineEx(P(u, -3), P(px, 5), 3f, body);
            Raylib.DrawCircleV(P(px + 0.5f, 5), 2f, dark);
        }
        // Head: round skull, a long pale muzzle ending in a dark nose, a dark mask with a pale brow, round ears and a cheek ruff.
        var h = P(14.5f, -8.5f + (sniff ? 3.5f : 0));
        float dd = d;
        Raylib.DrawCircleV(h + new Vector2(-dd * 2f, -4.6f), 2.3f, body);                                            // ears
        Raylib.DrawCircleV(h + new Vector2(dd * 2.6f, -4.3f), 2.3f, body);
        Raylib.DrawCircleV(h + new Vector2(-dd * 2f, -4.4f), 1.1f, C(150, 140, 140));
        Raylib.DrawCircleV(h + new Vector2(dd * 2.6f, -4.1f), 1.1f, C(150, 140, 140));
        Raylib.DrawEllipseV(h + new Vector2(dd * 0.5f, 2.4f), 5, 2.6f, pale);                                        // cheek ruff
        Raylib.DrawCircleV(h, 5.2f, fur);                                                                           // skull
        Raylib.DrawEllipseV(h + new Vector2(dd * 5f, 1.6f), 4.2f, 2.8f, pale);                                       // muzzle
        Raylib.DrawEllipseV(h + new Vector2(dd * 1.6f, -0.5f), 4.6f, 2.3f, dark);                                    // mask
        Raylib.DrawEllipseV(h + new Vector2(dd * 1.6f, -3f), 2.8f, 0.9f, pale);                                      // pale brow
        Raylib.DrawCircleV(h + new Vector2(dd * 8.6f, 1f), 1.3f, C(20, 20, 26));                                     // nose
        Rlgl.PopMatrix();
        Sprites.End();
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
        if (RareT >= 0) DrawRaccoon();

        // Canvas: everything around the opening
        Raylib.DrawRectangle(0, 0, W, 30, canvas);
        Tri(new Vector2(0, 30), new Vector2(320, 30), BL, Shade(canvas, 0.92f));
        Tri(new Vector2(0, 30), BL, new Vector2(0, 360), Shade(canvas, 0.68f));
        Tri(new Vector2(640, 30), new Vector2(320, 30), BR, Shade(canvas, 0.92f));
        Tri(new Vector2(640, 30), BR, new Vector2(640, 360), Shade(canvas, 0.68f));
        Raylib.DrawRectangleGradientV(0, 30, 640, 330, C(0, 0, 0, 0), C(10, 6, 4, 120));
        // seams and ties
        Raylib.DrawLineEx(new Vector2(0, 30), new Vector2(190, 360), 2, Shade(canvas, 0.47f));
        Raylib.DrawLineEx(new Vector2(640, 30), new Vector2(450, 360), 2, Shade(canvas, 0.47f));
        Raylib.DrawLineEx(new Vector2(100, 30), new Vector2(95, 360), 1, Shade(canvas, 0.52f));
        Raylib.DrawLineEx(new Vector2(540, 30), new Vector2(545, 360), 1, Shade(canvas, 0.52f));
        Raylib.DrawLineEx(new Vector2(320, 0), new Vector2(320, 30), 3, Shade(canvas, 0.47f));
        // drops running on the canvas
        foreach (var r in runs)
        {
            float edge = Math.Clamp((CanvasClearance(r.X, r.Y) - 3) / 14f, 0, 1); // fades out as it nears the opening
            Raylib.DrawLineV(new Vector2(r.X, r.Y - 12), new Vector2(r.X, r.Y), C(180, 150, 110, (int)(30 * edge)));
            Raylib.DrawCircleV(new Vector2(r.X, r.Y), 1.8f, C(200, 170, 130, (int)(70 * edge)));
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
