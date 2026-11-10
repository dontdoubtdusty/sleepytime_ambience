using System.Numerics;
using Raylib_cs;
using static Gfx;

// ---------------------------------------------------------------------------
// Fireplace
// ---------------------------------------------------------------------------
public class FireplaceScene : Scene
{
    public override string Name => "Fireplace";
    protected override (Layer, string, float)[] SoundLayers => new[]
    {
        (Layer.Crackle, "Fireplace", 0.8f), (Layer.Purr, "Cat purr", 0.2f),
    };

    readonly Fire fire = new(320, 316, 110, 1.75f);
    readonly List<(Rectangle r, Color c)> bricks = new();
    float t, candle = 1, candleTarget = 1, candleTimer, catEar, catEarTimer = 6;

    public FireplaceScene()
    {
        var rnd = new Random(7);
        for (int row = 0; 100 + row * 14 < H; row++)
        {
            int off = (row % 2) * 14;
            for (int x = 160 - off; x < 480; x += 28)
            {
                int y = 100 + row * 14;
                if (x + 14 > 215 && x + 14 < 425 && y + 6 > 175 && y + 6 < 335) continue; // opening
                int s = rnd.Next(-12, 13);
                bricks.Add((new Rectangle(Math.Max(x, 160) + 1, y + 1, 26, 12), C(74 + s, 48 + s / 2, 40 + s / 2)));
            }
        }
    }

    public override void Update(float dt)
    {
        t += dt;
        fire.Update(dt);
        candleTimer -= dt;
        if (candleTimer <= 0) { candleTarget = Rf(0.7f, 1.2f); candleTimer = Rf(0.08f, 0.3f); }
        candle += (candleTarget - candle) * Math.Min(1, dt * 8);
        catEarTimer -= dt;
        if (catEarTimer <= 0) { catEar = 0.35f; catEarTimer = Rf(8, 20); }
        catEar = Math.Max(0, catEar - dt);
    }

    // A tabby curled up asleep on the hearth rug: slow breathing and now and then an ear twitch.
    void DrawCat()
    {
        float breathe = MathF.Sin(t * 1.5f) * 0.9f;
        var fur = C(150, 98, 52); var dark = C(104, 66, 36); var light = C(210, 190, 160);
        Raylib.DrawEllipse(415, 350, 62, 9, C(104, 44, 40));
        Raylib.DrawEllipse(415, 350, 52, 6, C(132, 70, 54));
        Raylib.DrawEllipse(415, 341, 30, 12 + breathe, fur);
        Raylib.DrawEllipse(404, (int)(332 - breathe * 0.4f), 3, 6, dark);
        Raylib.DrawEllipse(414, (int)(330 - breathe * 0.4f), 3, 6, dark);
        Raylib.DrawEllipse(424, (int)(331 - breathe * 0.4f), 3, 6, dark);
        Raylib.DrawCircle(386, 343, 9, fur);
        Tri(new Vector2(379, 337), new Vector2(380, 327), new Vector2(387, 335), fur);
        float twitch = MathF.Sin(t * 45) * catEar * 5;
        Tri(new Vector2(388, 335), new Vector2(395 + twitch, 327), new Vector2(393, 337), fur);
        Raylib.DrawLine(381, 343, 386, 344, C(40, 25, 15));
        Raylib.DrawEllipse(412, 349, 24, 4, C(130, 84, 44));
        Raylib.DrawCircle(389, 349, 3, light);
        Raylib.DrawEllipse(396, 347, 6, 3, light);
    }

    public override void Draw()
    {
        Raylib.DrawRectangleGradientV(0, 0, W, H, C(30, 22, 18), C(16, 11, 9));

        // Painting above the mantel
        Raylib.DrawRectangle(285, 8, 70, 62, C(62, 42, 28));
        Raylib.DrawRectangleGradientV(290, 13, 60, 52, C(40, 48, 62), C(26, 34, 40));
        Raylib.DrawTriangle(new Vector2(302, 65), new Vector2(322, 38), new Vector2(342, 65), C(18, 24, 28));

        // Stone surround
        Raylib.DrawRectangle(160, 100, 320, 260, C(40, 28, 24));
        foreach (var (r, c) in bricks) Raylib.DrawRectangleRec(r, c);

        // Mantel
        Raylib.DrawRectangle(138, 84, 364, 18, C(62, 40, 27));
        Raylib.DrawRectangle(138, 84, 364, 3, C(88, 60, 40));

        // Opening and hearth
        Raylib.DrawRectangle(215, 175, 210, 160, C(9, 6, 5));
        Raylib.DrawRectangleGradientV(220, 180, 200, 150, C(20, 12, 10), C(8, 5, 4));
        Raylib.DrawRectangle(148, 335, 344, 25, C(78, 68, 64));
        Raylib.DrawRectangle(148, 335, 344, 2, C(110, 98, 92));

        DrawCat();

        // Candles on the mantel
        foreach (int cx in new[] { 190, 455 })
        {
            Raylib.DrawRectangle(cx - 3, 66, 6, 18, C(205, 195, 175));
            Raylib.DrawEllipse(cx, 62, 2.5f, 5 * candle, C(255, 210, 120));
        }

        // Logs behind and under the flames
        Raylib.DrawRectangleRounded(new Rectangle(255, 308, 130, 20), 0.5f, 6, C(52, 32, 19));
        Raylib.DrawRectangleRounded(new Rectangle(275, 295, 110, 18), 0.5f, 6, C(66, 41, 24));
        fire.Draw();
        Raylib.DrawRectangleRounded(new Rectangle(242, 320, 156, 15), 0.5f, 6, C(44, 27, 16));

        // Warm light spilling into the room
        Raylib.BeginBlendMode(BlendMode.Additive);
        float f = fire.Flicker;
        Glow(320, 300, 340, C(255, 110, 30, (int)(60 * f)));
        Glow(320, 300, 160, C(255, 170, 60, (int)(55 * f)));
        foreach (int cx in new[] { 190, 455 }) Glow(cx, 62, 55, C(255, 190, 90, (int)(55 * candle)));
        Raylib.EndBlendMode();

        Vignette();
    }
}

