using System.Numerics;
using Raylib_cs;
using static Gfx;

// A quiet aquarium: slow fish, swaying plants, rising bubbles and soft light from above.
public class AquariumScene : Scene
{
    public override string Name => "Aquarium";
    protected override (Layer, string, float)[] SoundLayers => new[]
    {
        (Layer.Bubbles, "Bubbles", 0.5f), (Layer.Underwater, "Underwater hum", 0.35f),
    };

    struct Fish { public float X, Y, BaseY, Speed, Size, Phase; public int Dir; public Color Body, Fin; }
    struct Bubble { public float X, Y, Size, Speed, Phase; }
    readonly Fish[] fish = new Fish[8];
    readonly List<Bubble> bubbles = new();
    readonly float[] plantX = { 70, 110, 250, 410, 560, 600 };
    readonly float[] plantH = { 120, 90, 100, 130, 110, 80 };
    float t, bubbleAcc;

    public AquariumScene()
    {
        Color[] bodies = { C(230, 120, 50), C(70, 130, 225), C(240, 200, 70), C(214, 84, 112), C(180, 192, 205) };
        for (int i = 0; i < fish.Length; i++)
        {
            var b = bodies[i % bodies.Length];
            fish[i] = new Fish
            {
                X = Rf(40, 600), BaseY = Rf(70, 280), Speed = Rf(9, 24), Size = Rf(0.8f, 1.5f), Phase = Rf(0, 6.28f),
                Dir = R.Next(2) == 0 ? -1 : 1, Body = b, Fin = Lerp(b, C(255, 255, 255), 0.25f)
            };
            fish[i].Y = fish[i].BaseY;
        }
    }

    public override void Update(float dt)
    {
        t += dt;
        for (int i = 0; i < fish.Length; i++)
        {
            ref Fish f = ref fish[i];
            f.X += f.Dir * f.Speed * dt;
            f.Y = f.BaseY + MathF.Sin(t * 0.6f + f.Phase) * 10;
            if (f.X < 24) { f.Dir = 1; f.BaseY = Rf(70, 280); }
            if (f.X > W - 24) { f.Dir = -1; f.BaseY = Rf(70, 280); }
        }
        bubbleAcc += dt * 6;
        while (bubbleAcc >= 1)
        {
            bubbleAcc--;
            bubbles.Add(new Bubble { X = 520 + Rf(-3, 3), Y = 322, Size = Rf(1.2f, 3.2f), Speed = Rf(24, 44), Phase = Rf(0, 6.28f) });
        }
        for (int i = bubbles.Count - 1; i >= 0; i--)
        {
            var b = bubbles[i];
            b.Y -= b.Speed * dt;
            b.X += MathF.Sin(t * 3 + b.Phase) * 6 * dt;
            if (b.Y < 22) bubbles.RemoveAt(i); else bubbles[i] = b;
        }
    }

    public override void Draw()
    {
        // Water
        Raylib.DrawRectangleGradientV(0, 0, W, H, C(14, 78, 98), C(4, 30, 50));

        // Light shafts from the lid
        Raylib.BeginBlendMode(BlendMode.Additive);
        for (int i = 0; i < 6; i++)
        {
            float x = 60 + i * 110 + MathF.Sin(t * 0.3f + i * 1.3f) * 22;
            float a = 10 + 6 * MathF.Sin(t * 0.5f + i);
            Tri(new Vector2(x - 12, 14), new Vector2(x + 12, 14), new Vector2(x - 60, 330), C(190, 235, 235, (int)a));
            Tri(new Vector2(x + 12, 14), new Vector2(x - 48, 330), new Vector2(x - 60, 330), C(190, 235, 235, (int)a));
        }
        Raylib.EndBlendMode();

        // Sand, rocks and pebbles
        Raylib.DrawRectangleGradientV(0, 322, W, 38, C(140, 122, 92), C(70, 58, 46));
        Raylib.DrawEllipse(160, 324, 46, 22, C(70, 76, 88));
        Raylib.DrawEllipse(185, 318, 26, 16, C(88, 94, 106));
        Raylib.DrawEllipse(430, 326, 36, 14, C(62, 68, 80));
        for (int i = 0; i < 40; i++)
        {
            float x = (i * 97 % 640), y = 330 + (i * 53 % 24);
            Raylib.DrawEllipse((int)x, (int)y, 3 + i % 3, 2, C(110 + i % 5 * 10, 98, 80));
        }
        // Air stone
        Raylib.DrawRectangle(516, 322, 8, 6, C(50, 50, 56));

        // Plants swaying
        for (int p = 0; p < plantX.Length; p++)
        {
            Vector2 prev = new(plantX[p], 326);
            int segs = 9;
            for (int s = 1; s <= segs; s++)
            {
                float u = s / (float)segs;
                var cur = new Vector2(plantX[p] + MathF.Sin(t * 0.8f + p + u * 2.6f) * 10 * u, 326 - u * plantH[p]);
                Raylib.DrawLineEx(prev, cur, 5 - u * 3, Lerp(C(24, 96, 62), C(70, 170, 100), u));
                prev = cur;
            }
        }

        // Fish
        foreach (var f in fish)
        {
            float s = f.Size, d = f.Dir;
            float wag = MathF.Sin(t * 5 + f.Phase) * 3 * s;
            Tri(new Vector2(f.X - d * 10 * s, f.Y), new Vector2(f.X - d * 19 * s, f.Y - 6 * s + wag), new Vector2(f.X - d * 19 * s, f.Y + 6 * s + wag), f.Fin);
            Tri(new Vector2(f.X - 3 * s, f.Y - 5 * s), new Vector2(f.X + 4 * s, f.Y - 5 * s), new Vector2(f.X - d * 2 * s, f.Y - 10 * s), f.Fin);
            Raylib.DrawEllipse((int)f.X, (int)f.Y, 12 * s, 6 * s, f.Body);
            Raylib.DrawEllipse((int)f.X, (int)(f.Y + 2 * s), 9 * s, 3 * s, Lerp(f.Body, C(255, 245, 225), 0.35f));
            Raylib.DrawCircle((int)(f.X + d * 7 * s), (int)(f.Y - 1.5f * s), 1.6f * s, C(10, 10, 14));
        }

        // Bubbles
        foreach (var b in bubbles)
        {
            Raylib.DrawCircleLines((int)b.X, (int)b.Y, b.Size, C(210, 240, 250, 150));
            Raylib.DrawCircleV(new Vector2(b.X - b.Size * 0.3f, b.Y - b.Size * 0.3f), b.Size * 0.3f, C(255, 255, 255, 190));
        }

        // Tank: glass edges, lid light
        Raylib.DrawRectangle(0, 0, W, 14, C(18, 20, 26));
        Raylib.DrawRectangle(0, 346, W, 14, C(18, 20, 26));
        Raylib.DrawRectangle(0, 0, 8, H, C(18, 20, 26));
        Raylib.DrawRectangle(W - 8, 0, 8, H, C(18, 20, 26));
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(320, 10, 380, C(190, 230, 230, 30));
        Raylib.EndBlendMode();

        Vignette(150);
    }
}
