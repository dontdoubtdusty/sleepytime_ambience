using System.Numerics;
using Raylib_cs;
using static Gfx;

// ---------------------------------------------------------------------------
// Koi pond garden, seen from above: slow koi, lily pads, drifting petals, ripples and soft lantern light.
// ---------------------------------------------------------------------------
public class KoiPondScene : Scene
{
    public override string Name => "Koi Pond";
    protected override (Layer, string, float)[] SoundLayers => new[]
    {
        (Layer.Water, "Pond water", 0.45f), (Layer.Crickets, "Crickets", 0.3f), (Layer.Wind, "Breeze", 0.15f),
    };
    public override (Evt, float, float)[] Events => new[] { (Evt.Chimes, 40f, 110f), (Evt.Owl, 90f, 200f) };

    // The pond is a rounded rectangle; the fish turn back when they near its edge.
    static readonly Rectangle Pond = new(34, 26, 572, 308);
    const int Segs = 9;

    class Koi
    {
        public Vector2 Head; public float Angle, Turn, Speed, Size, TurnTimer;
        public Vector2[] Body = new Vector2[Segs];
        public Color Base;                                         // the main body colour
        public (float U, float Lat, float Rad, Color Col)[] Patches = Array.Empty<(float, float, float, Color)>();   // patches: along the body 0..1, sideways -1..1, size, colour
    }
    readonly Koi[] fish = new Koi[6];
    struct Pad { public float X, Y, R, Rot; public bool Flower; }
    readonly Pad[] pads = new Pad[9];
    struct Petal { public float X, Y, Vx, Vy, Rot; }
    readonly Petal[] petals = new Petal[9];
    readonly List<(float x, float y, float age)> ripples = new();
    readonly (float x, float y, float r)[] stones = new (float, float, float)[70];
    float t, rippleTimer = 3, lantern = 1;

    public KoiPondScene()
    {
        // Koi colours for this launch: (main, pale, dark, gold) - classic orange, vermilion, pale gold, or rose and plum.
        (Color, Color, Color, Color)[] sets =
        {
            (C(214, 104, 42), C(196, 190, 180), C(40, 36, 40), C(206, 156, 54)),
            (C(226, 70, 42), C(214, 206, 196), C(24, 22, 28), C(230, 170, 60)),
            (C(220, 176, 90), C(220, 214, 196), C(70, 78, 92), C(236, 206, 140)),
            (C(222, 120, 110), C(230, 214, 190), C(62, 40, 60), C(236, 172, 100)),
        };
        var (orange, white, dark, gold) = sets[Launch.Pick(sets.Length)];
        var none = Array.Empty<(float, float, float, Color)>();
        (Color Base, (float U, float Lat, float Rad, Color Col)[] Patches)[] looks =
        {
            (white, new[] { (0.10f, 0f, 0.9f, orange), (0.38f, 0.25f, 0.75f, orange), (0.64f, -0.2f, 0.65f, orange) }),                  // kohaku: white with red patches
            (white, new[] { (0.07f, 0f, 0.55f, orange) }),                                                                              // tancho: one round red spot on the head
            (gold, none),                                                                                                               // ogon: all one metallic colour
            (white, new[] { (0.12f, -0.1f, 0.85f, dark), (0.36f, 0.2f, 0.8f, orange), (0.6f, 0f, 0.65f, dark), (0.8f, 0.1f, 0.5f, orange) }),  // showa: white, red and black
            (orange, new[] { (0.3f, 0f, 0.7f, Shade(orange, 1.12f)) }),                                                                 // plain orange
            (white, new[] { (0.15f, 0.15f, 0.85f, orange), (0.45f, -0.25f, 0.8f, orange), (0.7f, 0.15f, 0.5f, dark), (0.55f, 0.3f, 0.35f, dark) }),  // sanke
        };
        for (int i = 0; i < fish.Length; i++)
        {
            var k = new Koi { Angle = Rf(0, 6.28f), Speed = Rf(14, 22), Size = Rf(0.9f, 1.3f), Base = looks[i].Base, Patches = looks[i].Patches };
            k.Head = new Vector2(Rf(Pond.X + 80, Pond.X + Pond.Width - 80), Rf(Pond.Y + 60, Pond.Y + Pond.Height - 60));
            for (int s = 0; s < Segs; s++) k.Body[s] = k.Head - new Vector2(MathF.Cos(k.Angle), MathF.Sin(k.Angle)) * (s * 8);
            fish[i] = k;
        }
        for (int i = 0; i < pads.Length; i++)
            pads[i] = new Pad { X = Rf(Pond.X + 40, Pond.X + Pond.Width - 40), Y = Rf(Pond.Y + 30, Pond.Y + Pond.Height - 30), R = Rf(14, 24), Rot = Rf(0, 6.28f), Flower = i % 3 == 0 };
        for (int i = 0; i < petals.Length; i++)
            petals[i] = new Petal { X = Rf(Pond.X, Pond.X + Pond.Width), Y = Rf(Pond.Y, Pond.Y + Pond.Height), Vx = Rf(-3, 3), Vy = Rf(-2, 2), Rot = Rf(0, 3) };
        var rnd = new Random(5);
        for (int i = 0; i < stones.Length; i++)
        {
            float u = i / (float)stones.Length * 4;
            float x, y;
            if (u < 1) { x = Pond.X + u * Pond.Width; y = Pond.Y; }
            else if (u < 2) { x = Pond.X + Pond.Width; y = Pond.Y + (u - 1) * Pond.Height; }
            else if (u < 3) { x = Pond.X + (3 - u) * Pond.Width; y = Pond.Y + Pond.Height; }
            else { x = Pond.X; y = Pond.Y + (4 - u) * Pond.Height; }
            stones[i] = (x + (float)rnd.NextDouble() * 4 - 2, y + (float)rnd.NextDouble() * 4 - 2, 7 + (float)rnd.NextDouble() * 6);
        }
    }

    public override void Update(float dt)
    {
        t += dt;
        foreach (var k in fish)
        {
            // Wander: pick a new gentle turn now and then; steer back toward the middle near the edge.
            k.TurnTimer -= dt;
            if (k.TurnTimer <= 0) { k.Turn = Rf(-0.6f, 0.6f); k.TurnTimer = Rf(1.5f, 4f); }
            var centre = new Vector2(Pond.X + Pond.Width / 2, Pond.Y + Pond.Height / 2);
            bool near = k.Head.X < Pond.X + 60 || k.Head.X > Pond.X + Pond.Width - 60 || k.Head.Y < Pond.Y + 50 || k.Head.Y > Pond.Y + Pond.Height - 50;
            if (near)
            {
                float want = MathF.Atan2(centre.Y - k.Head.Y, centre.X - k.Head.X);
                float diff = MathF.IEEERemainder(want - k.Angle, 6.2832f);
                k.Angle += Math.Clamp(diff, -1f, 1f) * dt * 1.4f;
            }
            else k.Angle += k.Turn * dt;
            k.Head += new Vector2(MathF.Cos(k.Angle), MathF.Sin(k.Angle)) * k.Speed * dt;
            // Each body segment follows the one in front, keeping a fixed spacing.
            k.Body[0] = k.Head;
            for (int s = 1; s < Segs; s++)
            {
                var d = k.Body[s] - k.Body[s - 1];
                float len = d.Length();
                if (len > 0.001f) k.Body[s] = k.Body[s - 1] + d / len * (7.5f * k.Size);
            }
        }
        for (int i = 0; i < petals.Length; i++)
        {
            ref Petal p = ref petals[i];
            p.X += (p.Vx + MathF.Sin(t * 0.3f + i) * 2) * dt;
            p.Y += p.Vy * dt;
            p.Rot += dt * 0.1f;
            if (p.X < Pond.X || p.X > Pond.X + Pond.Width) p.Vx = -p.Vx;
            if (p.Y < Pond.Y || p.Y > Pond.Y + Pond.Height) p.Vy = -p.Vy;
        }
        rippleTimer -= dt;
        if (rippleTimer <= 0) { ripples.Add((Rf(Pond.X + 30, Pond.X + Pond.Width - 30), Rf(Pond.Y + 30, Pond.Y + Pond.Height - 30), 0)); rippleTimer = Rf(4, 10); }
        for (int i = ripples.Count - 1; i >= 0; i--)
        {
            var r = ripples[i];
            r.age += dt;
            if (r.age > 4) ripples.RemoveAt(i); else ripples[i] = r;
        }
        lantern = 0.92f + 0.08f * MathF.Sin(t * 1.3f);
    }

    // Rare event: a dragonfly flies in, rests on a lily pad for a while, then flies off (about 26 seconds).
    protected override float RareLength => 26;
    Vector2 dfFrom, dfTo, dfAway; int dfPad;
    protected override void OnRareStart()
    {
        dfPad = R.Next(pads.Length);
        var pad = new Vector2(pads[dfPad].X, pads[dfPad].Y);
        bool left = R.Next(2) == 0;
        dfFrom = new Vector2(left ? -30 : W + 30, Rf(20, 340));
        dfAway = new Vector2(left ? W + 30 : -30, Rf(20, 340));
        dfTo = pad;
    }

    protected override bool AnimalsVisible => true;
    protected override void DrawAnimals()
    {
        for (int i = 0; i < fish.Length; i++) DrawKoi(i);
        if (RareT >= 0) DrawDragonfly();
    }

    // One koi: a fin tail, then a chain of circles for the body (tail first so the head lies on top), and two eyes.
    void DrawKoi(int ki)
    {
        if (HideAnimals) return;
        var k = fish[ki];
        if (!Sprites.Begin(ki, (k.Body[0] + k.Body[Segs - 1]) / 2, 120, 120)) return;
        // A smooth point along the spine: f runs from 0 (nose) to Segs-1 (tail), blended between the body points (Catmull-Rom curve).
        Vector2 Spine(float f)
        {
            int i = Math.Clamp((int)f, 0, Segs - 2);
            float u = f - i;
            var p0 = k.Body[Math.Max(i - 1, 0)]; var p1 = k.Body[i]; var p2 = k.Body[i + 1]; var p3 = k.Body[Math.Min(i + 2, Segs - 1)];
            return 0.5f * (2 * p1 + (p2 - p0) * u + (2 * p0 - 5 * p1 + 4 * p2 - p3) * u * u + (3 * p1 - p0 - 3 * p2 + p3) * u * u * u);
        }
        float size = k.Size;
        Color fin = WithA(Lerp(k.Base, C(250, 246, 238), 0.7f), 200);     // fins are always pale and see-through
        // Tail fin: a broad, flowing fan with two lobes, swaying slowly behind the body.
        var tail = k.Body[Segs - 1];
        var back = tail - k.Body[Segs - 2];
        float ta = MathF.Atan2(back.Y, back.X) + MathF.Sin(t * 2.2f + size * 5) * 0.28f;
        Vector2 Dir(float a) => new(MathF.Cos(a), MathF.Sin(a));
        var root = Spine(Segs - 2.4f);
        var lobeA = tail + Dir(ta + 0.5f) * 18 * size; var lobeB = tail + Dir(ta - 0.5f) * 18 * size;
        var midA = tail + Dir(ta + 0.22f) * 15 * size; var midB = tail + Dir(ta - 0.22f) * 15 * size;
        var notch = tail + Dir(ta) * 9 * size;
        var wideA = tail + Dir(ta + 1.0f) * 7 * size; var wideB = tail + Dir(ta - 1.0f) * 7 * size;
        Tri(root, wideA, lobeA, fin); Tri(root, lobeA, midA, fin); Tri(root, midA, notch, fin);
        Tri(root, notch, midB, fin); Tri(root, midB, lobeB, fin); Tri(root, lobeB, wideB, fin);
        // Pectoral fins: wide white fans just behind the head, each waving a little.
        foreach (float side in new[] { -1f, 1f })
        {
            float fa = k.Angle + side * (2.15f + MathF.Sin(t * 2.6f + side) * 0.14f);
            var at = Spine(1.3f);
            var tip = at + Dir(fa) * 11 * size;
            var front = at + Dir(fa + side * 0.55f) * 8 * size;
            var rear = at + Dir(fa - side * 0.65f) * 9 * size;
            Tri(at, front, tip, fin); Tri(at, tip, rear, fin);
        }
        // Body: a plump torpedo, round at the head, widest about a quarter of the way back, tapering to a slim tail.
        float Radius(float u) => (u < 0.16f ? 4.6f + 2.2f * MathF.Sin(u / 0.16f * MathF.PI / 2) : 6.8f - 5.0f * MathF.Pow((u - 0.16f) / 0.84f, 1.15f)) * size;
        const float Step = 0.22f;
        for (float f = Segs - 1.2f; f >= 0; f -= Step)
            Raylib.DrawCircleV(Spine(f), Radius(f / (Segs - 1)), k.Base);
        // Patches: a few overlapping blobs each, kept inside the outline of the body.
        foreach (var (pu, lat, rad, col) in k.Patches)
        {
            float f = pu * (Segs - 1), r = Radius(pu);
            var c0 = Spine(f);
            var tan = Spine(f + 0.1f) - c0; tan /= MathF.Max(0.001f, tan.Length());
            var nrm = new Vector2(-tan.Y, tan.X);
            float pr = rad * r * (1 - 0.5f * MathF.Abs(lat));
            var centre = c0 + nrm * lat * r * 0.55f;
            Raylib.DrawCircleV(centre, pr, col);
            Raylib.DrawCircleV(centre + tan * pr * 0.7f, pr * 0.8f, col);
            Raylib.DrawCircleV(centre - tan * pr * 0.6f, pr * 0.7f, col);
        }
        // A soft highlight along the back, and two small dark eyes.
        for (float f = 1.2f; f < Segs - 3.2f; f += 0.9f)
            Raylib.DrawCircleV(Spine(f) + new Vector2(-0.6f, -0.9f) * size, Radius(f / (Segs - 1)) * 0.45f, new Color(255, 255, 255, 22));
        foreach (float side in new[] { -1f, 1f })
            Raylib.DrawCircleV(k.Body[0] + Dir(k.Angle + side * 0.95f) * 3.6f * size, 0.95f * size, C(20, 20, 24));
        Sprites.End();
    }

    void DrawDragonfly()
    {
        if (HideAnimals) return;
        // 0 to 7 s: fly in; 7 to 19 s: rest on the pad; 19 to 26 s: fly away.
        float u; Vector2 a, b;
        if (RareT < 7) { a = dfFrom; b = dfTo; u = RareT / 7; }
        else if (RareT < 19) { a = b = dfTo; u = 0; }
        else { a = dfTo; b = dfAway; u = (RareT - 19) / 7; }
        u = u * u * (3 - 2 * u);
        var p = a + (b - a) * u;
        bool resting = RareT >= 7 && RareT < 19;
        var dir = RareT < 19 ? dfTo - dfFrom : dfAway - dfTo;
        float ang = MathF.Atan2(dir.Y, dir.X);
        if (!resting) p += new Vector2(MathF.Sin(t * 1.3f) * 3, MathF.Cos(t * 1.7f) * 3);
        var fwd = new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * 1.4f; var side = new Vector2(-fwd.Y, fwd.X); // 1.4: drawn a little larger so it reads at this size
        if (!Sprites.Begin(20, p, 56, 56)) return;
        // shadow on the water, a little off to the side
        var sh = p + new Vector2(4, 6);
        Raylib.DrawLineEx(sh - fwd * 8, sh + fwd * 6, 2, C(0, 20, 20, 50));
        // body: tail, thorax, head
        // body: a long slim tail that tapers (a row of small circles), a round thorax and a head
        for (int i = 0; i < 12; i++)
            Raylib.DrawCircleV(p - fwd * (1.5f + i * 0.75f), 1.15f - i * 0.03f, i % 4 == 3 ? C(70, 170, 200) : C(40, 120, 150));
        Raylib.DrawCircleV(p + fwd * 2.5f, 1.9f, C(60, 160, 190));
        Raylib.DrawCircleV(p + fwd * 5.2f, 1.6f, C(30, 80, 100));
        // two pairs of leaf-shaped see-through wings, held out flat; a slow small tremble keeps them alive without flashing
        float tr = resting ? MathF.Sin(t * 1.5f) * 0.04f : MathF.Sin(t * 9) * 0.1f;
        foreach (float s in new[] { 1f, -1f })
        {
            for (int pair = 0; pair < 2; pair++)
            {
                var root = p + fwd * (pair == 0 ? 3f : 0.5f);
                var tip = root + fwd * (pair == 0 ? 1f + tr * 8 : -1.5f - tr * 8) + side * s * (pair == 0 ? 11f : 10f);
                var mid = (root + tip) / 2;
                var w = fwd * 1.5f;                                    // half the wing's width
                Color glass = C(214, 238, 246, pair == 0 ? 170 : 150);
                Tri(root, mid + w, mid - w, glass); Tri(mid + w, tip, mid - w, glass);
                Raylib.DrawLineEx(root, tip, 0.5f, C(240, 250, 255, 210));
            }
        }
        Sprites.End();
    }

    public override void Draw()
    {
        // Mossy stone surround, then the water
        Raylib.DrawRectangle(0, 0, W, H, C(26, 34, 28));
        Raylib.DrawRectangleRounded(Pond, 0.18f, 12, C(12, 44, 50));
        Raylib.DrawRectangleRounded(new Rectangle(Pond.X + 10, Pond.Y + 10, Pond.Width - 20, Pond.Height - 20), 0.18f, 12, C(16, 58, 62));
        foreach (var s in stones) Raylib.DrawCircle((int)s.x, (int)s.y, s.r, C(58, 62, 58));
        foreach (var s in stones) Raylib.DrawCircle((int)s.x - 1, (int)s.y - 2, s.r * 0.6f, C(78, 82, 76));

        // Slow dappled light moving over the water
        Raylib.BeginBlendMode(BlendMode.Additive);
        for (int i = 0; i < 12; i++)
        {
            float x = Pond.X + 60 + (i * 97) % (Pond.Width - 120) + MathF.Sin(t * 0.25f + i) * 22;
            float y = Pond.Y + 40 + (i * 61) % (Pond.Height - 80) + MathF.Cos(t * 0.2f + i * 1.7f) * 16;
            Raylib.DrawEllipse((int)x, (int)y, 40, 16, C(110, 190, 190, 9));
        }
        Raylib.EndBlendMode();

        // Koi, drawn tail first so the head lies on top
        for (int i = 0; i < fish.Length; i++) DrawKoi(i);

        // Lily pads (a round pad with a wedge cut out) and a few flowers
        foreach (var p in pads)
        {
            Raylib.DrawCircle((int)p.X, (int)p.Y, p.R, C(34, 96, 56));
            Raylib.DrawCircle((int)p.X - 2, (int)p.Y - 2, p.R * 0.7f, C(44, 114, 66));
            var a = new Vector2(p.X, p.Y);
            Tri(a, a + new Vector2(MathF.Cos(p.Rot - 0.15f), MathF.Sin(p.Rot - 0.15f)) * (p.R + 2), a + new Vector2(MathF.Cos(p.Rot + 0.15f), MathF.Sin(p.Rot + 0.15f)) * (p.R + 2), C(16, 58, 62));
            if (p.Flower)
            {
                for (int k = 0; k < 6; k++)
                {
                    float ang = k * 1.047f + p.Rot;
                    Raylib.DrawCircle((int)(p.X + MathF.Cos(ang) * 4), (int)(p.Y + MathF.Sin(ang) * 4), 3.2f, C(240, 170, 190));
                }
                Raylib.DrawCircle((int)p.X, (int)p.Y, 2.4f, C(250, 220, 120));
            }
        }

        foreach (var p in petals) Raylib.DrawEllipse((int)p.X, (int)p.Y, 3, 1.6f, C(244, 190, 200, 200));

        if (RareT >= 0) DrawDragonfly();

        // Ripples: rings that widen and fade
        foreach (var r in ripples)
        {
            float rr = r.age * 12;
            int a = (int)(110 * (1 - r.age / 4));
            Raylib.DrawEllipseLines((int)r.x, (int)r.y, rr, rr * 0.9f, C(170, 220, 220, a));
            if (r.age > 0.6f) Raylib.DrawEllipseLines((int)r.x, (int)r.y, rr - 7, (rr - 7) * 0.9f, C(170, 220, 220, a / 2));
        }

        // Warm lantern light spilling in from two corners
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(20, 20, 130, C(255, 170, 80, (int)(34 * lantern)));
        Glow(620, 340, 130, C(255, 170, 80, (int)(30 * lantern)));
        Raylib.EndBlendMode();
        Raylib.DrawCircle(20, 20, 6, C(255, 214, 150));
        Raylib.DrawCircle(620, 340, 6, C(255, 214, 150));

        Vignette(150);
    }
}
