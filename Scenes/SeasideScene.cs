using System.Numerics;
using Raylib_cs;
using static Gfx;

// Seaside at night: moonlit waves rolling in, foam on the sand, and a lighthouse whose beam sweeps slowly past.
public class SeasideScene : Scene
{
    public override string Name => "Seaside Lighthouse";
    protected override (Layer, string, float)[] SoundLayers => new[]
    {
        (Layer.Waves, "Waves", 0.7f), (Layer.Wind, "Sea breeze", 0.2f),
    };

    const int Horizon = 190, Shore = 300;
    const float LampX = 548, LampY = 112;
    readonly (float x, float y, float tw)[] stars = new (float, float, float)[70];
    readonly (float x, float row, float ph, float rate)[] glitter = new (float, float, float, float)[60];
    struct Gull { public float X, Y, Speed, Phase; }
    readonly Gull[] gulls = new Gull[3];
    float t;

    // Lighthouse paint for this launch (red, navy, pine green, mustard).
    static readonly Color[] Paints = { C(100, 38, 34), C(40, 62, 112), C(38, 84, 60), C(140, 108, 44) };
    readonly Color paint = Paints[Launch.Pick(Paints.Length)];

    public SeasideScene()
    {
        for (int i = 0; i < stars.Length; i++) stars[i] = (Rf(0, W), Rf(0, 150), Rf(0, 6.28f));
        for (int i = 0; i < glitter.Length; i++)
            glitter[i] = (Rf(-1, 1), Rf(0, 1), Rf(0, 6.28f), Rf(0.6f, 1.6f));
        for (int i = 0; i < gulls.Length; i++)
            gulls[i] = new Gull { X = Rf(0, W), Y = Rf(50, 130), Speed = Rf(6, 12), Phase = Rf(0, 6.28f) };
    }

    public override void Update(float dt)
    {
        t += dt;
        for (int i = 0; i < gulls.Length; i++)
        {
            gulls[i].X -= gulls[i].Speed * dt;
            if (gulls[i].X < -20) { gulls[i].X = W + 20; gulls[i].Y = Rf(50, 130); }
        }
    }

    // Rare event: the lights of a far ship slide along the horizon, one way or the other, over about 100 seconds.
    const float ShipSpeed = 7;
    protected override float RareLength => (W + 60) / ShipSpeed;
    int shipDir = 1;
    protected override void OnRareStart() => shipDir = R.Next(2) == 0 ? 1 : -1;

    void DrawShip()
    {
        float x = (shipDir > 0 ? -30 : W + 30) + shipDir * ShipSpeed * RareT;
        float y = Horizon;
        Raylib.DrawRectangle((int)x - 7, (int)y - 3, 14, 3, C(10, 12, 22));      // hull
        Raylib.DrawRectangle((int)x - 2, (int)y - 6, 4, 3, C(12, 14, 26));       // bridge
        var side = shipDir > 0 ? C(255, 70, 60) : C(70, 255, 120);              // red or green side light, whichever faces us
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(x, y - 7, 7, C(255, 240, 200, 150));                                // masthead light
        Glow(x + shipDir * 5, y - 2, 5, WithA(side, 120));
        Glow(x, y + 2, 12, C(255, 220, 160, 22));                                // faint reflection
        Raylib.EndBlendMode();
    }

    public override void Draw()
    {
        // Sky, stars, moon
        Raylib.DrawRectangleGradientV(0, 0, W, Horizon, C(6, 10, 30), C(52, 58, 92));
        foreach (var (x, y, tw) in stars)
            Raylib.DrawRectangle((int)x, (int)y, 1, 1, C(230, 238, 255, (int)(110 + 100 * MathF.Sin(t * 1.1f + tw))));
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(150, 72, 110, C(150, 175, 215, 44));
        Raylib.EndBlendMode();
        Raylib.DrawCircle(150, 72, 16, C(230, 236, 222));

        // Gulls
        foreach (var g in gulls)
        {
            float flap = MathF.Sin(t * 2.4f + g.Phase) * 3;
            var c = C(150, 160, 185);
            Raylib.DrawLineV(new Vector2(g.X - 6, g.Y - flap), new Vector2(g.X, g.Y + 1), c);
            Raylib.DrawLineV(new Vector2(g.X, g.Y + 1), new Vector2(g.X + 6, g.Y - flap), c);
        }

        // Sea with moving wave lines
        Raylib.DrawRectangleGradientV(0, Horizon, W, Shore - Horizon, C(18, 30, 60), C(8, 14, 30));
        if (RareT >= 0) DrawShip();
        for (int r = 0; r < 14; r++)
        {
            float k = r / 13f;
            float y0 = Horizon + 4 + k * k * (Shore - Horizon - 14);
            float amp = 0.6f + k * 2.5f;
            Vector2 prev = new(0, y0);
            for (int x = 8; x <= W; x += 8)
            {
                var p = new Vector2(x, y0 + MathF.Sin(x * (0.025f - k * 0.01f) + t * (0.5f + k * 0.5f) + r) * amp);
                Raylib.DrawLineV(prev, p, C(70, 100, 150, (int)(30 + 50 * k)));
                prev = p;
            }
        }
        // Moonlight glitter on the water
        Raylib.BeginBlendMode(BlendMode.Additive);
        foreach (var (gx, row, ph, rate) in glitter)
        {
            float y = Horizon + 4 + row * row * (Shore - Horizon - 20);
            float x = 150 + gx * (14 + row * 70);
            float a = MathF.Max(0, MathF.Sin(t * rate + ph));
            Raylib.DrawRectangle((int)x, (int)y, 2 + (int)(row * 6), 1, C(220, 235, 255, (int)(110 * a)));
        }
        Raylib.EndBlendMode();

        // Rocks and lighthouse
        Tri(new Vector2(440, Shore + 6), new Vector2(520, 244), new Vector2(640, Shore + 6), C(16, 16, 24));
        Raylib.DrawRectangle(440, Shore - 20, 200, 30, C(14, 14, 22));
        Tri(new Vector2(LampX - 22, 252), new Vector2(LampX - 14, 128), new Vector2(LampX + 14, 128), paint);
        Tri(new Vector2(LampX - 22, 252), new Vector2(LampX + 14, 128), new Vector2(LampX + 22, 252), paint);
        for (int s = 0; s < 3; s++)
        {
            float y0 = 140 + s * 36;
            float half0 = 16 + (y0 - 128) * 0.074f, half1 = 16 + (y0 + 14 - 128) * 0.074f;
            Raylib.DrawRectangle((int)(LampX - (half0 + half1) / 2), (int)y0, (int)(half0 + half1), 14, C(122, 118, 112));
        }
        Raylib.DrawRectangle((int)LampX - 20, 122, 40, 6, C(20, 20, 28));
        Raylib.DrawRectangle((int)LampX - 11, 104, 22, 18, C(250, 220, 140));
        Tri(new Vector2(LampX - 15, 104), new Vector2(LampX, 90), new Vector2(LampX + 15, 104), C(40, 30, 34));

        // Beach: wet sand and foam sliding in and out
        Raylib.DrawRectangleGradientV(0, Shore, W, H - Shore, C(70, 62, 62), C(34, 30, 34));
        float surge = MathF.Sin(t * 0.45f) * 9;
        for (int x = 0; x < W; x += 4)
        {
            float y = Shore + 6 + surge + MathF.Sin(x * 0.03f + t * 0.8f) * 3;
            Raylib.DrawRectangle(x, (int)y, 4, 3, C(190, 208, 222, 90));
            Raylib.DrawRectangle(x, (int)y + 3, 4, 8, C(40, 52, 76, 70));
        }

        // Lighthouse light and sweeping beam
        float ang = t * 0.35f;
        float dx = MathF.Cos(ang), facing = -MathF.Sin(ang);
        float len = 760 * dx;
        float vis = 0.25f + 0.75f * MathF.Abs(dx);
        Raylib.BeginBlendMode(BlendMode.Additive);
        Tri(new Vector2(LampX, LampY), new Vector2(LampX + len, LampY - 34 * vis), new Vector2(LampX + len, LampY + 34 * vis), C(255, 235, 170, (int)(34 * vis)));
        Tri(new Vector2(LampX, LampY), new Vector2(LampX + len * 0.8f, LampY - 14 * vis), new Vector2(LampX + len * 0.8f, LampY + 14 * vis), C(255, 240, 190, (int)(30 * vis)));
        float lampGlow = 70 + 130 * MathF.Pow(MathF.Max(0, facing), 4);
        Glow(LampX, LampY, 60, C(255, 225, 150, (int)lampGlow));
        Raylib.EndBlendMode();

        Vignette(160);
    }
}
