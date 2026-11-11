using System.Numerics;
using Raylib_cs;
using static Gfx;

// The opening screen: a dark room, one slow brush circle (an enso) drawing itself, and one calm line.
// Nothing here is really loading (the random looks are picked instantly); it is a few seconds of quiet
// that eases the picture and the sound in. Any key or click skips ahead to the fade.
public class Intro
{
    const float DrawFrom = 0.4f, DrawSecs = 3.2f; // the circle starts and takes this long to close
    const float HoldEnd = 4.2f, FadeSecs = 1.8f;  // when the fade into the scene begins, and how long it lasts

    // Short lines about rest. Original wording, so nothing needs crediting.
    static readonly string[] Quotes =
    {
        "Nothing is waiting for you tonight.",
        "Let the day finish without you.",
        "Rest is not a reward. It is part of the work.",
        "The night is long enough for everything to be fine.",
        "Breathe out slowly. The room will hold you.",
        "You have done enough for one day.",
        "Let your shoulders fall. Let the quiet in.",
        "Sleep arrives when you stop calling it.",
        "The world keeps turning without your help tonight.",
        "There is nothing to solve before morning.",
        "Even the sea rests between waves.",
        "Be as still as a lantern in a window.",
        "Tonight, you are allowed to simply be tired.",
        "The stars are in no hurry. Neither are you.",
        "Close your eyes. The night will keep watch.",
        "Quiet is not empty. It is full of rest.",
        "Put the day down. It will wait for you.",
        "The rain does not rush, and it still arrives.",
        "You are the sky. Everything else – it's just the weather.|Pema Chödrön",
    };

    // A line may end with "|Name" to credit a real author; the credit is drawn smaller underneath.
    static readonly string[] Picked = Quotes[int.TryParse(Environment.GetEnvironmentVariable("SLEEPY_QUOTE"), out var qi) ? Math.Clamp(qi, 0, Quotes.Length - 1) : Random.Shared.Next(Quotes.Length)].Split('|');
    readonly string quote = Picked[0];
    readonly string? author = Picked.Length > 1 ? Picked[1] : null;
    readonly bool enabled;
    float t;

    /// <param name="enabled">False skips the whole screen (screenshots and tests).</param>
    public Intro(bool enabled) { this.enabled = enabled; if (!enabled) t = 99; }

    /// <summary>How visible the scene is: 0 while the intro holds, rising to 1 as it fades away. Sound follows it.</summary>
    public float Level => Smooth(Math.Clamp((t - HoldEnd) / FadeSecs, 0, 1));

    /// <summary>True while the intro still covers enough of the screen that the panel should stay out of the way.</summary>
    public bool Covering => t < HoldEnd + FadeSecs * 0.6f;

    public bool Done => t >= HoldEnd + FadeSecs;

    static float Smooth(float x) => x * x * (3 - 2 * x);

    /// <summary>Advances time. A key or click while the circle is still drawing jumps straight to the fade.</summary>
    public void Update(float dt, bool skip)
    {
        if (Done) return;
        t += dt;
        if (skip && t < HoldEnd && t > 0.3f) t = HoldEnd;
    }

    /// <summary>Draws the black cover, the circle and the line, over everything else.</summary>
    public void Draw(int sw, int sh)
    {
        if (Done) return;
        float level = Level;
        float cover = 1 - level;
        Raylib.DrawRectangle(0, 0, sw, sh, new Color(8, 6, 6, (int)(cover * 255)));

        float unit = sh / 720f;
        float cx = sw / 2f, cy = sh * 0.42f, radius = 100 * unit;
        float elements = Math.Clamp(t / 0.8f, 0, 1) * cover; // everything fades in at the start and out at the end
        var ink = new Color(236, 218, 190, 255);

        // The circle: a brush stroke that starts thick and presses lighter as it goes round.
        float p = Smooth(Math.Clamp((t - DrawFrom) / DrawSecs, 0, 1));
        float sweep = p * MathF.PI * 2 * 0.93f, start = -MathF.PI * 0.62f;
        int steps = Math.Max(1, (int)(p * 400));
        for (int i = 0; i <= steps; i++)
        {
            float u = i / (float)steps;
            float a = start + sweep * u;
            float r = radius * (1 + 0.025f * MathF.Sin(a * 2.3f + 0.7f) + u * 0.04f);
            var pos = new Vector2(cx + MathF.Cos(a) * r, cy + MathF.Sin(a) * r);
            float width = (11 - 8 * u * u) * unit;
            Raylib.DrawCircleV(pos, width * 0.5f, WithA(ink, 215 * elements * (0.55f + 0.45f * (1 - u * 0.6f))));
        }
        // A faint glow at the brush tip while it is still moving.
        if (p < 1)
        {
            float a = start + sweep;
            var tip = new Vector2(cx + MathF.Cos(a) * radius * 1.03f, cy + MathF.Sin(a) * radius * 1.03f);
            Raylib.BeginBlendMode(BlendMode.Additive);
            Glow(tip.X, tip.Y, 22 * unit, WithA(ink, 70 * elements));
            Raylib.EndBlendMode();
        }

        // The line, wrapped and centred under the circle, fading in once the circle is well under way.
        float qa = Math.Clamp((t - 1.4f) / 1.4f, 0, 1) * cover;
        float size = 24 * unit, maxW = Math.Min(sw * 0.7f, 640 * unit);
        var lines = UiText.Wrap(quote, size, maxW);
        float y = cy + radius + 56 * unit;
        foreach (var line in lines)
        {
            UiText.Draw(line, (sw - UiText.Measure(line, size)) / 2f, y, size, WithA(ink, 235 * qa));
            y += size * 1.45f;
        }
        if (author != null)
        {
            float asz = size * 0.75f;
            string credit = "– " + author;
            UiText.Draw(credit, (sw - UiText.Measure(credit, asz)) / 2f, y + size * 0.3f, asz, WithA(ink, 170 * qa));
        }
    }
}
