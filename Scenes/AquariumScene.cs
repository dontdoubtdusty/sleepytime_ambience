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

    struct Fish { public float X, Y, BaseY, Speed, Size, Phase; public int Dir, Kind; public float Face; public Color Body, Fin; }
    struct Bubble { public float X, Y, Size, Speed, Phase; }
    readonly Fish[] fish = new Fish[8];
    readonly List<Bubble> bubbles = new();
    readonly float[] plantX = { 70, 110, 250, 410, 560, 600 };
    readonly float[] plantH = { 120, 90, 100, 130, 110, 80 };
    float t, bubbleAcc;

    public AquariumScene()
    {
        // Fish colours for this launch: tropical, warm reef, or cool silver and blue.
        Color[][] sets =
        {
            new[] { C(230, 120, 50), C(70, 130, 225), C(240, 200, 70), C(214, 84, 112), C(180, 192, 205) },
            new[] { C(236, 96, 60), C(244, 170, 70), C(220, 70, 100), C(250, 214, 110), C(200, 120, 170) },
            new[] { C(150, 176, 214), C(96, 140, 200), C(190, 200, 214), C(70, 110, 170), C(130, 190, 190) },
        };
        Color[] bodies = sets[Launch.Pick(sets.Length)];
        for (int i = 0; i < fish.Length; i++)
        {
            var b = bodies[i % bodies.Length];
            fish[i] = new Fish
            {
                X = Rf(40, 600), BaseY = Rf(70, 280), Speed = Rf(9, 24), Size = Rf(1.0f, 1.45f), Phase = Rf(0, 6.28f), Kind = i % 4,
                Dir = R.Next(2) == 0 ? -1 : 1, Body = b, Fin = Lerp(b, C(255, 255, 255), 0.25f)
            };
            fish[i].Y = fish[i].BaseY; fish[i].Face = fish[i].Dir;
        }
    }

    public override void Update(float dt)
    {
        t += dt;
        for (int i = 0; i < fish.Length; i++)
        {
            ref Fish f = ref fish[i];
            // Face eases from -1 to 1 (and back), so a turn is a slow swing through end-on: the fish slows, narrows, and swims off the other way.
            f.Face += (f.Dir - f.Face) * Math.Min(1, dt * 1.6f);
            f.X += f.Face * f.Speed * dt;
            float wantY = f.BaseY + MathF.Sin(t * 0.6f + f.Phase) * 10;
            f.Y += (wantY - f.Y) * Math.Min(1, dt * 1.5f);                         // depth changes glide instead of jumping
            if (f.X < 24 && f.Dir < 0) { f.Dir = 1; f.BaseY = Rf(70, 280); }
            if (f.X > W - 24 && f.Dir > 0) { f.Dir = -1; f.BaseY = Rf(70, 280); }
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

    // Rare event: a sea turtle glides across the tank, flippers beating slowly (about 55 seconds).
    const float TurtleSpeed = 13;
    protected override float RareLength => (W + 120) / TurtleSpeed;
    int turtleDir = 1; float turtleY;
    protected override void OnRareStart() { turtleDir = R.Next(2) == 0 ? 1 : -1; turtleY = Rf(90, 220); }

    protected override bool AnimalsVisible => true;
    protected override void DrawAnimals()
    {
        for (int i = 0; i < fish.Length; i++) DrawFish(i);
        if (RareT >= 0) DrawTurtle();
    }

    // One fish. Painted smooth through Sprites; the soft neon glow of the tetras is added afterwards in the normal pass.
    void DrawFish(int i)
    {
        if (HideAnimals) return;
        var f = fish[i];
        if (Sprites.Begin(i, new Vector2(f.X, f.Y), 96, 64))
        {
            PaintFish(f);
            Sprites.End();
        }
        if (!Sprites.Preparing && f.Kind == 0 && f.Body.R + f.Body.G + f.Body.B < 520)    // pale fish would blow out, so they get no glow
        {
            Raylib.BeginBlendMode(BlendMode.Additive);
            Glow(f.X, f.Y, 20 * f.Size, WithA(f.Body, 14));
            Raylib.EndBlendMode();
        }
    }

    // The fish picture, side-on, in four body types taken from real aquarium fish: 0 a deep neon tetra with a forked tail,
    // 1 a round goldfish with a flowing tail, 2 a tall tang disc, 3 a small banded clownfish.
    // The body is a smooth profile (deepest part a bit forward, slim at the tail root, rounded at the snout) drawn as
    // strips, so colour bands (pale back, pale belly, stripes) are just strips between two heights of that same profile.
    void PaintFish(Fish f)
    {
        float s = f.Size, d = f.Face;
        (float L, float H, float Peak)[] shapes = { (28, 7.6f, 0.42f), (24, 8.6f, 0.5f), (24, 11f, 0.5f), (20, 6.6f, 0.45f) };
        var (L, H, peak) = shapes[f.Kind];
        float e = MathF.Log(0.5f) / MathF.Log(peak);
        float wag = MathF.Sin(t * 5 + f.Phase) * 2.5f, flutter = MathF.Sin(t * 7 + f.Phase);
        Vector2 V(float u, float v) => new(f.X + d * u * s, f.Y + v * s);                        // u forward along the fish, v down
        float U(float tt) => (tt - 0.5f) * L;                                                    // tt runs 0 (tail root) to 1 (snout)
        float Depth(float tt) => 2 * H * (0.14f + 0.86f * MathF.Pow(MathF.Max(0, MathF.Sin(MathF.PI * MathF.Pow(tt, e))), 0.85f));
        float Y(float tt, float fr) => Depth(tt) * (fr - 0.52f);                                 // fr: 0 at the back edge, 1 at the belly edge
        Vector2 P(float tt, float fr) => V(U(tt), Y(tt, fr));
        void Band(float t0, float t1, float f0, float f1, Color c)
        {
            const int N = 14;
            for (int k = 0; k < N; k++)
            {
                float a = t0 + (t1 - t0) * k / N, b = t0 + (t1 - t0) * (k + 1) / N;
                Tri(P(a, f0), P(b, f0), P(b, f1), c); Tri(P(a, f0), P(b, f1), P(a, f1), c);
            }
        }
        // A fin standing on the back (side -1) or belly (side +1): base from t0 to t1, tip reaching tipT along the body, h away from it.
        void Fin(float side, float t0, float t1, float tipT, float h, Color c)
        {
            float fr = side < 0 ? 0 : 1;
            var tip = V(U(tipT), Y(tipT, fr) + side * h);
            Tri(P(t0, fr), P(t1, fr), tip, c);
            Raylib.DrawLineEx(P((t0 + t1) / 2, fr), tip, 0.5f * s, new Color(255, 255, 255, 60));       // one fin ray
        }
        Color fin = WithA(f.Fin, 205), light = Lerp(f.Body, C(255, 255, 255), 0.28f), belly = Lerp(f.Body, C(255, 245, 225), 0.45f);

        // Tail first, so the body lies over its root.
        float tx = -L / 2;
        var root = V(tx + 1, 0);
        switch (f.Kind)
        {
            case 0:  // forked
                Tri(root, V(tx - 12, -9 + wag), V(tx - 5.5f, wag * 0.5f), fin); Tri(root, V(tx - 12, 9 + wag), V(tx - 5.5f, wag * 0.5f), fin); break;
            case 1:  // flowing double fan
                Tri(root, V(tx - 15, -11 + wag), V(tx - 10, -3 + wag * 0.6f), fin); Tri(root, V(tx - 10, -3 + wag * 0.6f), V(tx - 6, 0), fin);
                Tri(root, V(tx - 6, 0), V(tx - 10, 3 + wag * 0.6f), fin); Tri(root, V(tx - 10, 3 + wag * 0.6f), V(tx - 15, 11 + wag), fin); break;
            case 2:  // short crescent
                Tri(root, V(tx - 7, -7 + wag * 0.6f), V(tx - 3.5f, 0), fin); Tri(root, V(tx - 7, 7 + wag * 0.6f), V(tx - 3.5f, 0), fin); break;
            default: // rounded paddle
                Tri(root, V(tx - 6, -5 + wag * 0.5f), V(tx - 8, wag * 0.5f), fin); Tri(root, V(tx - 8, wag * 0.5f), V(tx - 6, 5 + wag * 0.5f), fin); break;
        }
        // Back and belly fins.
        switch (f.Kind)
        {
            case 0: Fin(-1, 0.34f, 0.6f, 0.28f, 10, fin); Fin(1, 0.28f, 0.6f, 0.2f, 8, fin); break;
            case 1: Fin(-1, 0.22f, 0.7f, 0.1f, 11, fin); Fin(1, 0.3f, 0.55f, 0.22f, 6, fin); break;
            case 2: Fin(-1, 0.18f, 0.78f, 0.14f, 8, fin); Fin(1, 0.2f, 0.78f, 0.16f, 7, fin); break;
            default: Fin(-1, 0.25f, 0.72f, 0.3f, 4.5f, fin); Fin(1, 0.4f, 0.6f, 0.36f, 3, fin); break;
        }
        // Body, pale back and belly, and the markings of each type.
        Band(0, 1, 0, 1, f.Body);
        Band(0.12f, 0.92f, 0, 0.3f, light);
        Band(0.1f, 0.95f, 0.72f, 1, belly);
        if (f.Kind == 0) Band(0.2f, 0.86f, 0.4f, 0.48f, WithA(Shade(f.Body, 0.7f), 130));          // the lateral stripe of a tetra
        if (f.Kind == 3)
        {
            Band(0.46f, 0.58f, 0, 1, C(250, 246, 238)); Band(0.12f, 0.19f, 0, 1, C(250, 246, 238));   // white bands
            Band(0.46f, 0.47f, 0, 1, WithA(Shade(f.Body, 0.6f), 160)); Band(0.57f, 0.58f, 0, 1, WithA(Shade(f.Body, 0.6f), 160));
        }
        // Pectoral fin, a little gill line, a small mouth and a big gentle eye.
        var pb = P(0.7f, 0.62f);
        Tri(pb, P(0.66f, 0.7f), V(U(0.5f) + flutter, Y(0.7f, 0.62f) + 5.5f), WithA(f.Fin, 190));
        Raylib.DrawLineEx(P(0.76f, 0.2f), P(0.72f, 0.5f), 0.6f * s, WithA(Shade(f.Body, 0.6f), 110));
        Raylib.DrawLineEx(P(0.72f, 0.5f), P(0.76f, 0.8f), 0.6f * s, WithA(Shade(f.Body, 0.6f), 110));
        Raylib.DrawLineEx(P(0.995f, 0.55f), P(0.94f, 0.6f), 0.7f * s, WithA(Shade(f.Body, 0.45f), 200));
        var eye = P(0.85f, 0.38f);
        float er = (f.Kind == 2 ? 2.0f : 2.3f) * s;
        Raylib.DrawCircleV(eye, er + 0.8f * s, Lerp(f.Body, C(255, 255, 255), 0.65f));
        Raylib.DrawCircleV(eye, er, C(22, 26, 44));
        Raylib.DrawCircleV(eye, er * 0.6f, C(4, 4, 8));
        Raylib.DrawCircleV(eye + new Vector2(-0.5f, -0.6f) * s, 0.55f * s, C(255, 255, 255));
    }

    void DrawTurtle()
    {
        if (HideAnimals) return;
        float d = turtleDir;
        float x = (d > 0 ? -60 : W + 60) + d * TurtleSpeed * RareT;
        float y = turtleY + MathF.Sin(t * 0.4f) * 8;
        float beat = MathF.Sin(t * 1.1f);                    // slow flipper stroke, -1 to 1
        if (!Sprites.Begin(20, new Vector2(x + d * 3, y + 6), 110, 56)) return;
        Color skin = C(122, 138, 92), shell = C(86, 104, 58), scute = C(66, 82, 44), rim = C(150, 164, 112);
        // A flipper is a few shrinking circles along a curve: rounded at the tip, wide at the shoulder.
        void Flipper(Vector2 root, Vector2 tip, float w, Color c)
        {
            for (int i = 0; i <= 8; i++)
            {
                float u = i / 8f;
                var p = Vector2.Lerp(root, tip, u) + new Vector2(0, MathF.Sin(u * MathF.PI) * -2f);
                Raylib.DrawCircleV(p, w * (1 - 0.65f * u), c);
            }
        }
        // far flippers and tail first, so the body sits over them
        Flipper(new Vector2(x - d * 9, y + 5), new Vector2(x - d * 22, y + 16 + beat * 5), 3.6f, Shade(skin, 0.75f));
        Flipper(new Vector2(x + d * 13, y + 4), new Vector2(x + d * 29, y + 14 - beat * 6), 3.8f, Shade(skin, 0.75f));
        Flipper(new Vector2(x - d * 19, y + 3), new Vector2(x - d * 28, y + 6), 1.8f, skin);                  // tail
        // shell: a pale rim, the dome, and the scute pattern on top
        Raylib.DrawEllipseV(new Vector2(x, y), 22, 15, rim);
        Raylib.DrawEllipseV(new Vector2(x, y), 20.5f, 13.5f, shell);
        Raylib.DrawEllipseV(new Vector2(x, y - 1), 12, 7.5f, scute);
        Raylib.DrawEllipseV(new Vector2(x - d * 11, y), 5, 6, scute);
        Raylib.DrawEllipseV(new Vector2(x + d * 11, y), 5, 6, scute);
        Raylib.DrawEllipseV(new Vector2(x, y - 1), 6.5f, 3.6f, Shade(shell, 1.1f));
        // head with a gentle face
        var head = new Vector2(x + d * 25, y - 2);
        Raylib.DrawLineEx(new Vector2(x + d * 17, y - 1), head, 6, skin);                               // neck
        Raylib.DrawCircleV(head, 6.2f, skin);
        Raylib.DrawCircleV(head + new Vector2(d * 3, -2), 1.3f, C(10, 10, 14));                          // eye
        Raylib.DrawLineEx(head + new Vector2(d * 3, 2), head + new Vector2(d * 5.6f, 1.6f), 0.8f, Shade(skin, 0.6f));   // smile
        // near flippers over the body
        Flipper(new Vector2(x - d * 8, y + 7), new Vector2(x - d * 20, y + 20 - beat * 6), 4.2f, skin);
        Flipper(new Vector2(x + d * 12, y + 5), new Vector2(x + d * 28, y + 18 + beat * 6), 4.4f, skin);
        Sprites.End();
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
        for (int i = 0; i < fish.Length; i++) DrawFish(i);

        if (RareT >= 0) DrawTurtle();

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
