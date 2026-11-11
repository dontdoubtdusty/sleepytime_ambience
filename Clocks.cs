using System.Diagnostics;
using System.Numerics;
using Raylib_cs;
using static Gfx;

// The time every clock in every scene shows, plus a shared analog clock face.
// With real time off, the clocks start at a random time when the program opens and then run at normal speed,
// so nobody has to see what time it really is.
public static class Clocks
{
    /// <summary>Set from the settings each frame: true shows the computer's local time.</summary>
    public static bool RealTime;

    // Test only: SLEEPY_CLOCK_START=HH:MM:SS starts the clocks at that time (for example 11:59:40 to hear the 12 o'clock chime soon).
    static readonly double offset = TimeSpan.TryParse(Environment.GetEnvironmentVariable("SLEEPY_CLOCK_START"), out var start) ? start.TotalSeconds : Random.Shared.NextDouble() * 86400;
    static readonly Stopwatch watch = Stopwatch.StartNew();

    /// <summary>Seconds since midnight on the clock face (0 to 86400).</summary>
    public static double Seconds => RealTime ? DateTime.Now.TimeOfDay.TotalSeconds : (offset + watch.Elapsed.TotalSeconds) % 86400;

    /// <summary>The hour (0 to 23) the clocks show now.</summary>
    public static int Hour => (int)(Seconds / 3600);

    /// <summary>Hours on a 12-hour dial as a fraction (0 to 12), minutes as a fraction (0 to 60), and whole seconds (0 to 59).</summary>
    public static (float Hours, float Minutes, int Seconds) Hands()
    {
        double s = Seconds;
        float min = (float)(s / 60 % 60);
        return ((float)(s / 3600 % 12), min, (int)(s % 60));
    }

    /// <summary>The time as "HH:MM" for digital clocks.</summary>
    public static string Text()
    {
        double s = Seconds;
        return $"{(int)(s / 3600):00}:{(int)(s / 60 % 60):00}";
    }

    /// <summary>
    /// A round clock face: rim, face, twelve marks and three hands (the second hand ticks once a second, and can be left out).
    /// </summary>
    public static void DrawFace(float cx, float cy, float r, Color rim, Color face, Color ink, Color? secondHand)
    {
        var (hr, min, sec) = Hands();
        var c = new Vector2(cx, cy);
        Raylib.DrawCircleV(c, r + MathF.Max(2f, r * 0.14f), rim);
        Raylib.DrawCircleV(c, r, face);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.PI / 6;
            var d = new Vector2(MathF.Sin(a), -MathF.Cos(a));
            float inner = i % 3 == 0 ? 0.72f : 0.84f;
            Raylib.DrawLineEx(c + d * r * inner, c + d * r * 0.95f, i % 3 == 0 ? MathF.Max(1.5f, r * 0.08f) : 1f, ink);
        }
        void Hand(float turns, float len, float thick, Color col)
        {
            float a = turns * 2 * MathF.PI;
            Raylib.DrawLineEx(c, new Vector2(cx + MathF.Sin(a) * r * len, cy - MathF.Cos(a) * r * len), thick, col);
        }
        Hand(hr / 12f, 0.5f, MathF.Max(2f, r * 0.11f), ink);
        Hand(min / 60f, 0.75f, MathF.Max(1.5f, r * 0.08f), ink);
        if (secondHand is Color sc) Hand(sec / 60f, 0.85f, 0.8f, sc);
        Raylib.DrawCircleV(c, MathF.Max(1.6f, r * 0.09f), ink);
    }

    // Which of the seven segments (a top, b top right, c bottom right, d bottom, e bottom left, f top left, g middle; bit 0 = a) each digit lights.
    static readonly int[] Segments = { 0b0111111, 0b0000110, 0b1011011, 0b1001111, 0b1100110, 0b1101101, 0b1111101, 0b0000111, 0b1111111, 0b1101111 };

    /// <summary>
    /// The time as an LED seven-segment display (HH:MM): lit segments in the given colour with a soft glow, unlit ones barely visible.
    /// Call inside normal blending; the glow part switches to additive blending itself.
    /// </summary>
    public static void DrawLed(float x, float y, float digitW, float digitH, Color on)
    {
        string text = Text();
        float th = MathF.Max(1.5f, digitW * 0.24f), gap = digitW * 0.3f, half = digitH / 2;
        var off = WithA(on, 28);
        void Digit(float dx, int d)
        {
            int m = Segments[d];
            var rects = new[]
            {
                new Rectangle(dx + th, y, digitW - 2 * th, th),                       // a
                new Rectangle(dx + digitW - th, y + th, th, half - th),               // b
                new Rectangle(dx + digitW - th, y + half, th, half - th),             // c
                new Rectangle(dx + th, y + digitH - th, digitW - 2 * th, th),         // d
                new Rectangle(dx, y + half, th, half - th),                           // e
                new Rectangle(dx, y + th, th, half - th),                             // f
                new Rectangle(dx + th, y + half - th / 2, digitW - 2 * th, th),       // g
            };
            for (int s = 0; s < 7; s++) Raylib.DrawRectangleRec(rects[s], (m >> s & 1) == 1 ? on : off);
            Raylib.BeginBlendMode(BlendMode.Additive);
            for (int s = 0; s < 7; s++) if ((m >> s & 1) == 1) Glow(rects[s].X + rects[s].Width / 2, rects[s].Y + rects[s].Height / 2, th * 3.2f, WithA(on, 40));
            Raylib.EndBlendMode();
        }
        float cx = x;
        Digit(cx, text[0] - '0'); cx += digitW + gap;
        Digit(cx, text[1] - '0'); cx += digitW + gap;
        // colon
        Raylib.DrawRectangle((int)(cx + gap * 0.3f), (int)(y + half * 0.55f), (int)th, (int)th, on);
        Raylib.DrawRectangle((int)(cx + gap * 0.3f), (int)(y + half * 1.35f), (int)th, (int)th, on);
        cx += th + gap * 1.2f;
        Digit(cx, text[3] - '0'); cx += digitW + gap;
        Digit(cx, text[4] - '0');
    }
}
