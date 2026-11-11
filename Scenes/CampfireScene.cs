using System.Numerics;
using Raylib_cs;
using static Gfx;

// ---------------------------------------------------------------------------
// Campfire under the stars: a small fire in a clearing, a wide starry sky, pines all around.
// ---------------------------------------------------------------------------
public class CampfireScene : Scene
{
    public override string Name => "Campfire";
    protected override (Layer, string, float)[] SoundLayers => new[]
    {
        (Layer.Crackle, "Campfire", 0.7f), (Layer.Crickets, "Crickets", 0.4f), (Layer.Wind, "Night breeze", 0.2f),
    };
    // Log pops now and then, and an owl far off.
    public override (Evt, float, float)[] Events => new[] { (Evt.Pop, 6f, 25f), (Evt.Owl, 70f, 160f) };

    readonly Fire fire = new(320, 292, 52, 1.1f);
    readonly (float x, float y, float tw, float size)[] stars = new (float, float, float, float)[190];
    readonly (float x, float h, float w, float phase)[] pines;
    float t, glow = 1, glowTarget = 1, glowTimer;

    public CampfireScene()
    {
        for (int i = 0; i < stars.Length; i++) stars[i] = (Rf(0, W), Rf(0, 235), Rf(0, 6.28f), Rf(0, 1) < 0.1f ? 2 : 1);
        var list = new List<(float, float, float, float)>();
        for (float x = -10; x < W + 20; x += Rf(26, 40))
        {
            if (x > 190 && x < 450) continue; // keep the clearing open
            list.Add((x, Rf(120, 190), Rf(46, 66), Rf(0, 6.28f)));
        }
        pines = list.ToArray();
    }

    public override void Update(float dt)
    {
        t += dt;
        fire.Update(dt);
        glowTimer -= dt;
        if (glowTimer <= 0) { glowTarget = Rf(0.8f, 1.15f); glowTimer = Rf(0.1f, 0.35f); }
        glow += (glowTarget - glow) * Math.Min(1, dt * 6);
    }

    public override void Draw()
    {
        // Sky, with a faint band of the Milky Way across it
        Raylib.DrawRectangleGradientV(0, 0, W, 260, C(3, 6, 20), C(26, 36, 64));
        Raylib.BeginBlendMode(BlendMode.Additive);
        for (int i = 0; i < 9; i++)
            Raylib.DrawEllipse(150 + i * 55, 40 + i * 24, 120, 22, C(70, 80, 120, 9));
        Raylib.EndBlendMode();
        foreach (var s in stars)
        {
            int a = (int)(120 + 110 * MathF.Sin(t * 1.1f + s.tw));
            Raylib.DrawRectangle((int)s.x, (int)s.y, (int)s.size, (int)s.size, C(235, 240, 255, a));
        }

        // Far dark ridge, then the pines in front of it
        Raylib.DrawRectangle(0, 238, W, 60, C(6, 12, 20));
        foreach (var p in pines)
            DrawPine(p.x, 300, p.h, p.w, C(5, 11, 15), MathF.Sin(t * 0.5f + p.phase) * 1.5f);

        // Ground, warmed near the fire
        Raylib.DrawRectangleGradientV(0, 290, W, 70, C(14, 18, 16), C(6, 8, 8));
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(320, 300, 130, C(255, 130, 50, (int)(26 * glow)));
        Glow(320, 270, 210, C(255, 140, 60, (int)(40 * glow)));
        Raylib.EndBlendMode();

        // Ring of stones, crossed logs and a log to sit on each side
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * 6.28f;
            Raylib.DrawCircle((int)(320 + MathF.Cos(a) * 48), (int)(304 + MathF.Sin(a) * 11), 5, C(52, 50, 54));
        }
        Raylib.DrawRectanglePro(new Rectangle(320, 300, 80, 10), new Vector2(40, 5), -14, C(52, 30, 20));
        Raylib.DrawRectanglePro(new Rectangle(320, 300, 80, 10), new Vector2(40, 5), 14, C(66, 38, 24));
        Raylib.DrawRectangle(110, 318, 90, 16, C(44, 28, 20));
        Raylib.DrawRectangle(440, 322, 90, 16, C(44, 28, 20));
        Raylib.BeginBlendMode(BlendMode.Additive);
        Raylib.DrawRectangle(110, 318, 90, 3, C(200, 110, 50, (int)(50 * glow)));
        Raylib.DrawRectangle(440, 322, 90, 3, C(200, 110, 50, (int)(50 * glow)));
        Raylib.EndBlendMode();

        // A small tent in the dark on the left, lit on the side facing the fire
        Tri(new Vector2(40, 304), new Vector2(110, 304), new Vector2(75, 250), C(12, 18, 24));
        Tri(new Vector2(75, 250), new Vector2(110, 304), new Vector2(84, 304), C(60, 40, 30, (int)(120 * glow)));

        fire.Draw();
        Vignette(170);
    }
}
