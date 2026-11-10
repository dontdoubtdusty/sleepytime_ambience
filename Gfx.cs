using System.Numerics;
using Raylib_cs;

public static class Gfx
{
    public const int W = 640, H = 360;
    public static readonly Random R = new();

    public static float Rf(float a, float b) => a + (float)R.NextDouble() * (b - a);
    public static Color C(int r, int g, int b, int a = 255) => new Color(r, g, b, a);
    public static Color WithA(Color c, float a) => new Color((int)c.R, (int)c.G, (int)c.B, (int)Math.Clamp(a, 0, 255));

    public static Color Lerp(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0, 1);
        return new Color(
            (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t), (int)(a.A + (b.A - a.A) * t));
    }

    /// <summary>Soft radial glow. Use inside additive blend mode for light.</summary>
    public static void Glow(float x, float y, float r, Color c)
    {
        Raylib.DrawCircleGradient(new Vector2(x, y), r, c, new Color((int)c.R, (int)c.G, (int)c.B, 0));
    }

    /// <summary>Filled triangle that works whichever way the points are ordered.</summary>
    public static void Tri(Vector2 a, Vector2 b, Vector2 c, Color col)
    {
        float cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        if (cross > 0) Raylib.DrawTriangle(a, c, b, col); else Raylib.DrawTriangle(a, b, c, col);
    }

    public static void Vignette(int strength = 150)
    {
        var dark = new Color(0, 0, 0, strength);
        var clear = new Color(0, 0, 0, 0);
        Raylib.DrawRectangleGradientV(0, 0, W, 70, dark, clear);
        Raylib.DrawRectangleGradientV(0, H - 70, W, 70, clear, dark);
        Raylib.DrawRectangleGradientH(0, 0, 90, H, dark, clear);
        Raylib.DrawRectangleGradientH(W - 90, 0, 90, H, clear, dark);
    }
}

/// <summary>Particle fire: additive soft blobs that rise, shrink and cool, plus drifting embers.</summary>
public class Fire
{
    struct P { public float X, Y, Vx, Vy, Life, Max, Size; }
    readonly List<P> flames = new();
    readonly List<P> embers = new();
    readonly float cx, cy, width, scale;
    float t, acc, emberAcc, flTarget = 1, flTimer;
    public float Flicker { get; private set; } = 1;

    public Fire(float cx, float cy, float width, float scale)
    {
        this.cx = cx; this.cy = cy; this.width = width; this.scale = scale;
    }

    public void Update(float dt)
    {
        t += dt;
        flTimer -= dt;
        if (flTimer <= 0) { flTarget = Gfx.Rf(0.75f, 1.15f); flTimer = Gfx.Rf(0.05f, 0.22f); }
        Flicker += (flTarget - Flicker) * Math.Min(1, dt * 9);

        acc += dt * 95;
        while (acc >= 1)
        {
            acc--;
            float u = (Gfx.Rf(-1, 1) + Gfx.Rf(-1, 1)) * 0.5f; // bias toward center
            flames.Add(new P
            {
                X = cx + u * width * 0.5f, Y = cy + Gfx.Rf(-3, 3),
                Vx = Gfx.Rf(-5, 5) * scale, Vy = -Gfx.Rf(50, 105) * scale,
                Max = Gfx.Rf(0.6f, 1.2f), Size = Gfx.Rf(9, 17) * scale * (1 - Math.Abs(u) * 0.55f)
            });
        }
        for (int i = flames.Count - 1; i >= 0; i--)
        {
            var p = flames[i];
            p.Life += dt;
            if (p.Life >= p.Max) { flames.RemoveAt(i); continue; }
            p.X += p.Vx * dt + MathF.Sin(t * 7 + p.Y * 0.12f) * 9 * dt * scale;
            p.Y += p.Vy * dt;
            p.Vx += (cx - p.X) * 0.8f * dt; // pull inward for a tapered tongue
            flames[i] = p;
        }

        emberAcc += dt * 6;
        while (emberAcc >= 1)
        {
            emberAcc--;
            embers.Add(new P
            {
                X = cx + Gfx.Rf(-width * 0.4f, width * 0.4f), Y = cy - Gfx.Rf(0, 15 * scale),
                Vx = Gfx.Rf(-8, 8) * scale, Vy = -Gfx.Rf(25, 60) * scale,
                Max = Gfx.Rf(2f, 4.2f), Size = Gfx.Rf(0.8f, 1.8f)
            });
        }
        for (int i = embers.Count - 1; i >= 0; i--)
        {
            var p = embers[i];
            p.Life += dt;
            if (p.Life >= p.Max) { embers.RemoveAt(i); continue; }
            p.X += (p.Vx + MathF.Sin(t * 2.5f + i) * 10) * dt;
            p.Y += p.Vy * dt;
            embers[i] = p;
        }
    }

    public void Draw()
    {
        Raylib.BeginBlendMode(BlendMode.Additive);
        foreach (var p in flames)
        {
            float tt = p.Life / p.Max;
            float r = p.Size * (1 - tt * 0.65f);
            Color c = tt < 0.35f
                ? Gfx.Lerp(Gfx.C(255, 225, 140), Gfx.C(255, 150, 40), tt / 0.35f)
                : Gfx.Lerp(Gfx.C(255, 150, 40), Gfx.C(190, 35, 8), (tt - 0.35f) / 0.65f);
            c = Gfx.WithA(c, 85 * (1 - tt) * Flicker);
            Gfx.Glow(p.X, p.Y, r, c);
        }
        foreach (var p in embers)
        {
            float tt = p.Life / p.Max;
            var c = Gfx.C(255, (int)(170 - 90 * tt), 40, (int)(230 * (1 - tt)));
            Raylib.DrawCircleV(new Vector2(p.X, p.Y), p.Size, c);
        }
        Raylib.EndBlendMode();
    }
}
