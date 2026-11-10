using System.Numerics;
using Raylib_cs;
using static Gfx;

// Rain seen through a window: falling streaks outside and drops that stall and slide on the glass.
// Shared by every rainy scene. Call DrawStreaks() for the outside view and DrawGlassDrops() on top of it.
public class RainWindow
{
    struct Streak { public float X, Y, Speed, Len; }
    struct Drop { public float X, Y, Size, Speed, Stall; }

    readonly float x, y, w, h;
    readonly Streak[] streaks;
    readonly Drop[] drops;

    public RainWindow(float x, float y, float w, float h, int streakCount = 110, int dropCount = 46)
    {
        this.x = x; this.y = y; this.w = w; this.h = h;
        streaks = new Streak[streakCount];
        drops = new Drop[dropCount];
        for (int i = 0; i < streaks.Length; i++)
            streaks[i] = new Streak { X = Rf(x, x + w), Y = Rf(y, y + h), Speed = Rf(330, 520), Len = Rf(8, 20) };
        for (int i = 0; i < drops.Length; i++) drops[i] = NewDrop(true);
    }

    Drop NewDrop(bool anywhere) => new()
    {
        X = Rf(x + 4, x + w - 4), Y = anywhere ? Rf(y, y + h) : y - 2, Size = Rf(1.2f, 3.2f),
        Speed = 0, Stall = Rf(0.5f, 6f)
    };

    public void Update(float dt)
    {
        for (int i = 0; i < streaks.Length; i++)
        {
            streaks[i].Y += streaks[i].Speed * dt;
            streaks[i].X -= 40 * dt;
            if (streaks[i].Y > y + h || streaks[i].X < x) { streaks[i].Y = y - 10; streaks[i].X = Rf(x, x + w + 40); }
        }
        for (int i = 0; i < drops.Length; i++)
        {
            ref Drop d = ref drops[i];
            d.Stall -= dt;
            if (d.Stall <= 0)
            {
                d.Speed = Rf(18, 55);
                d.Stall = Rf(0.3f, 4f);
                if (R.Next(3) == 0) d.Speed = 0;
            }
            d.Y += d.Speed * dt;
            d.Speed *= 1 - Math.Min(1, dt * 1.5f);
            if (d.Y > y + h) drops[i] = NewDrop(false);
        }
    }

    /// <summary>Thin falling rain lines (the view outside the glass).</summary>
    public void DrawStreaks()
    {
        foreach (var s in streaks)
            Raylib.DrawLineV(new Vector2(s.X, s.Y), new Vector2(s.X + 0.12f * s.Len, s.Y - s.Len), C(170, 190, 220, 55));
    }

    /// <summary>Drops sitting on or sliding down the glass.</summary>
    public void DrawGlassDrops()
    {
        foreach (var d in drops)
        {
            if (d.Speed > 8)
                Raylib.DrawLineV(new Vector2(d.X, d.Y - d.Speed * 0.7f), new Vector2(d.X, d.Y), C(150, 175, 210, 30));
            Raylib.DrawCircleV(new Vector2(d.X, d.Y), d.Size, C(150, 175, 210, 55));
            Raylib.DrawCircleV(new Vector2(d.X - d.Size * 0.3f, d.Y - d.Size * 0.3f), d.Size * 0.4f, C(235, 245, 255, 130));
        }
    }
}
