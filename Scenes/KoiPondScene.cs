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
        public Color[] Colors = new Color[Segs];
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
        Color orange = C(214, 104, 42), white = C(196, 190, 180), dark = C(40, 36, 40), gold = C(206, 156, 54);
        Color[][] patterns =
        {
            new[] { orange, orange, white, orange, orange, white, orange, orange, orange },
            new[] { white, white, orange, white, white, dark, white, white, white },
            new[] { gold, gold, gold, white, gold, gold, gold, gold, gold },
            new[] { orange, white, white, orange, white, white, orange, white, white },
            new[] { orange, orange, orange, dark, orange, orange, white, orange, orange },
            new[] { white, orange, white, white, orange, white, white, white, white },
        };
        for (int i = 0; i < fish.Length; i++)
        {
            var k = new Koi { Angle = Rf(0, 6.28f), Speed = Rf(14, 22), Size = Rf(0.9f, 1.3f), Colors = patterns[i] };
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
        foreach (var k in fish)
        {
            var tail = k.Body[Segs - 1];
            var tailDir = k.Body[Segs - 2] - tail;
            float ta = MathF.Atan2(tailDir.Y, tailDir.X);
            float sway = MathF.Sin(t * 2.2f + k.Size * 5) * 0.35f;
            var fin1 = tail - new Vector2(MathF.Cos(ta + sway + 0.45f), MathF.Sin(ta + sway + 0.45f)) * 16 * k.Size;
            var fin2 = tail - new Vector2(MathF.Cos(ta + sway - 0.45f), MathF.Sin(ta + sway - 0.45f)) * 16 * k.Size;
            Tri(k.Body[Segs - 2], fin1, fin2, C(220, 170, 125, 150));
            for (int s = Segs - 1; s >= 0; s--)
            {
                float w = (s == 0 ? 4.4f : s <= 3 ? 5.4f : 5.4f - (s - 3) * 0.55f) * k.Size;
                Raylib.DrawCircleV(k.Body[s], Math.Max(1.5f, w), k.Colors[s]);
            }
            Raylib.DrawCircleV(k.Body[0] + new Vector2(MathF.Cos(k.Angle + 1.1f), MathF.Sin(k.Angle + 1.1f)) * 3, 0.8f, C(20, 20, 20));
            Raylib.DrawCircleV(k.Body[0] + new Vector2(MathF.Cos(k.Angle - 1.1f), MathF.Sin(k.Angle - 1.1f)) * 3, 0.8f, C(20, 20, 20));
        }

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
