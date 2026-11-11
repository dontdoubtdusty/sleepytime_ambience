using System.Numerics;
using Raylib_cs;
using static Gfx;

// ---------------------------------------------------------------------------
// Hot spring in the snow: a steaming pool ringed with rocks, snow falling, mountains behind.
// ---------------------------------------------------------------------------
public class HotSpringScene : Scene
{
    public override string Name => "Hot Spring";
    protected override (Layer, string, float)[] SoundLayers => new[]
    {
        (Layer.Water, "Spring water", 0.5f), (Layer.Wind, "Winter wind", 0.2f),
    };
    public override (Evt, float, float)[] Events => new[] { (Evt.Owl, 80f, 180f), (Evt.Chimes, 60f, 150f) };

    struct Flake { public float X, Y, Speed, Size, Phase; }
    struct Steam { public float X, Y, Vx, Vy, Size, Life, Max; }
    readonly Flake[] flakes = new Flake[130];
    readonly List<Steam> steam = new();
    readonly (float x, float y, float r)[] rocks = new (float, float, float)[34];
    readonly (float x, float y)[] stars = new (float, float)[60];
    float t, steamAcc, lantern = 1, lanternTarget = 1, lanternTimer;

    const float PoolX = 320, PoolY = 282, PoolRx = 215, PoolRy = 46;

    public HotSpringScene()
    {
        for (int i = 0; i < flakes.Length; i++)
            flakes[i] = new Flake { X = Rf(0, W), Y = Rf(0, H), Speed = Rf(10, 26), Size = Rf(0.8f, 2), Phase = Rf(0, 6.28f) };
        for (int i = 0; i < rocks.Length; i++)
        {
            float a = i / (float)rocks.Length * 6.28f + Rf(-0.05f, 0.05f);
            rocks[i] = (PoolX + MathF.Cos(a) * (PoolRx + Rf(-4, 10)), PoolY + MathF.Sin(a) * (PoolRy + Rf(-2, 8)), Rf(9, 17));
        }
        for (int i = 0; i < stars.Length; i++) stars[i] = (Rf(0, W), Rf(0, 120));
        rocks = rocks.OrderBy(r => r.y).ToArray(); // draw back rocks first
    }

    public override void Update(float dt)
    {
        t += dt;
        for (int i = 0; i < flakes.Length; i++)
        {
            ref Flake f = ref flakes[i];
            f.Y += f.Speed * dt;
            f.X += MathF.Sin(t * 0.8f + f.Phase) * 8 * dt;
            if (f.Y > H) { f.Y = -4; f.X = Rf(0, W); }
        }
        // Steam rises from the whole pool, widens and fades
        steamAcc += dt * 9;
        while (steamAcc >= 1)
        {
            steamAcc--;
            steam.Add(new Steam { X = PoolX + Rf(-PoolRx * 0.8f, PoolRx * 0.8f), Y = PoolY + Rf(-8, 8), Vx = Rf(-4, 4), Vy = -Rf(9, 18), Size = Rf(14, 26), Max = Rf(6, 10) });
        }
        for (int i = steam.Count - 1; i >= 0; i--)
        {
            var s = steam[i];
            s.Life += dt;
            if (s.Life >= s.Max) { steam.RemoveAt(i); continue; }
            s.X += (s.Vx + MathF.Sin(t * 0.5f + s.Y * 0.05f) * 5) * dt;
            s.Y += s.Vy * dt;
            steam[i] = s;
        }
        lanternTimer -= dt;
        if (lanternTimer <= 0) { lanternTarget = Rf(0.85f, 1.1f); lanternTimer = Rf(0.2f, 0.7f); }
        lantern += (lanternTarget - lantern) * Math.Min(1, dt * 4);
    }

    public override void Draw()
    {
        // Night sky and mountains
        Raylib.DrawRectangleGradientV(0, 0, W, 210, C(6, 10, 28), C(44, 58, 92));
        foreach (var (x, y) in stars) Raylib.DrawRectangle((int)x, (int)y, 1, 1, C(220, 230, 255, 130));
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(470, 60, 90, C(150, 170, 210, 26));
        Raylib.EndBlendMode();
        Raylib.DrawCircle(470, 60, 12, C(222, 230, 240));
        Tri(new Vector2(-40, 215), new Vector2(150, 70), new Vector2(330, 215), C(18, 26, 46));
        Tri(new Vector2(150, 70), new Vector2(104, 105), new Vector2(193, 105), C(120, 138, 172));
        Tri(new Vector2(240, 215), new Vector2(430, 100), new Vector2(680, 215), C(12, 19, 36));
        Tri(new Vector2(430, 100), new Vector2(384, 128), new Vector2(491, 128), C(110, 128, 162));

        // Snowy ground
        Raylib.DrawRectangleGradientV(0, 205, W, 155, C(70, 84, 112), C(30, 38, 56));

        // Pool: dark teal water with a slow shimmer
        Raylib.DrawEllipse((int)PoolX, (int)PoolY, PoolRx, PoolRy, C(22, 70, 84));
        Raylib.DrawEllipse((int)PoolX, (int)(PoolY + 4), PoolRx * 0.85f, PoolRy * 0.7f, C(30, 96, 108));
        Raylib.BeginBlendMode(BlendMode.Additive);
        for (int i = 0; i < 7; i++)
        {
            float w = 60 + 20 * MathF.Sin(t * 0.3f + i);
            Raylib.DrawEllipse((int)(PoolX - 150 + i * 50 + MathF.Sin(t * 0.2f + i * 2) * 14), (int)(PoolY + MathF.Sin(i * 3.1f) * 20), w, 3, C(160, 220, 230, 14));
        }
        Raylib.EndBlendMode();

        // Rocks around the edge with snow on top (back half first so the front ones overlap the water)
        foreach (var r in rocks)
        {
            Raylib.DrawEllipse((int)r.x, (int)r.y, r.r, r.r * 0.7f, C(46, 50, 62));
            Raylib.DrawEllipse((int)r.x, (int)(r.y - r.r * 0.3f), r.r * 0.85f, r.r * 0.4f, C(176, 190, 214));
        }

        // Two stone lanterns glowing warmly at the pool edges
        foreach (float lx in new[] { 92f, 548f })
        {
            Raylib.DrawRectangle((int)lx - 4, 262, 8, 22, C(40, 44, 54));
            Raylib.DrawRectangle((int)lx - 9, 252, 18, 12, C(54, 58, 70));
            Raylib.DrawRectangle((int)lx - 5, 255, 10, 7, C(255, 200, 120));
            Raylib.DrawRectangle((int)lx - 11, 249, 22, 4, C(40, 44, 54));
            Raylib.BeginBlendMode(BlendMode.Additive);
            Glow(lx, 258, 60, C(255, 170, 80, (int)(70 * lantern)));
            Raylib.EndBlendMode();
        }

        // Steam
        Raylib.BeginBlendMode(BlendMode.Additive);
        foreach (var s in steam)
        {
            float p = s.Life / s.Max;
            Glow(s.X, s.Y, s.Size * (0.7f + p * 0.9f), C(190, 205, 225, (int)(16 * MathF.Sin(p * MathF.PI))));
        }
        Raylib.EndBlendMode();

        // Falling snow
        foreach (var f in flakes) Raylib.DrawCircleV(new Vector2(f.X, f.Y), f.Size * 0.6f, C(235, 240, 255, 200));

        Vignette(150);
    }
}
