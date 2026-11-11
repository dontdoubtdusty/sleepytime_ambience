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
        (Layer.Crackle, "Fireplace", 0.8f), (Layer.Purr, "Cat purr", 0.2f), (Layer.Grandfather, "Grandfather clock", 0.12f),
    };

    readonly Fire fire = new(320, 316, 110, 1.75f);
    readonly List<(Rectangle r, Color c)> bricks = new();
    float t, candle = 1, candleTarget = 1, candleTimer, catEar, catEarTimer = 6;

    readonly AmbientAudio audio;
    float pend; // pendulum position in its cycle (0 to 1), kept in step with the tick sound
    const float PendulumSecs = 2.451f, TickAt = 0.0398f; // length of the two-tick loop in grandfather.wav, and where its first tick falls in it

    public FireplaceScene(AmbientAudio audio)
    {
        this.audio = audio;
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
        UpdateSparks(dt);
        // The pendulum runs on its own and is nudged toward the tick loop's real position, so each tick lands at a swing's end.
        pend += dt / PendulumSecs;
        if (audio.LoopPhase(Layer.Grandfather, 0.15f) is float ap) { float err = ap - pend; err -= MathF.Round(err); pend += err * MathF.Min(1, dt * 3); }
        pend -= MathF.Floor(pend);
        candleTimer -= dt;
        if (candleTimer <= 0) { candleTarget = Rf(0.7f, 1.2f); candleTimer = Rf(0.08f, 0.3f); }
        candle += (candleTarget - candle) * Math.Min(1, dt * 8);
        catEarTimer -= dt;
        if (catEarTimer <= 0) { catEar = 0.35f; catEarTimer = Rf(8, 20); }
        catEar = Math.Max(0, catEar - dt);
    }

    // Body colours for the sleeping cat-dino, one chosen each launch: (body, spines and stripes, belly).
    static readonly (Color Fur, Color Dark, Color Light)[] CatColors =
    {
        (C(150, 98, 52), C(104, 66, 36), C(210, 190, 160)),   // ginger
        (C(98, 118, 142), C(62, 78, 104), C(190, 204, 214)),  // slate blue
        (C(104, 132, 78), C(66, 90, 52), C(206, 214, 170)),   // moss green
        (C(132, 96, 128), C(92, 62, 92), C(214, 194, 208)),   // plum
    };
    readonly (Color Fur, Color Dark, Color Light) cat = CatColors[Launch.Pick(CatColors.Length)];

    protected override bool AnimalsVisible => true;
    protected override void DrawAnimals() => DrawCat();

    // A little cat-like dinosaur curled up asleep on the hearth rug: slow breathing, a row of soft back spines,
    // a spade-tipped tail that sways, a snout, cat ears and now and then an ear twitch.
    void DrawCat()
    {
        if (HideAnimals) return;
        float breathe = MathF.Sin(t * 1.5f) * 0.9f;
        if (!Sprites.Begin(0, new Vector2(418, 326), 150, 64)) return;
        Rlgl.PushMatrix(); Rlgl.Translatef(0, -12, 0);                       // the whole cat is painted 12 px higher than the numbers below say
        var (fur, dark, light) = cat;
        Raylib.DrawEllipse(415, 350, 62, 9, C(104, 44, 40));
        Raylib.DrawEllipse(415, 350, 52, 6, C(132, 70, 54));
        Color pink = C(222, 140, 140);
        // Tail: a plump ringed tail that leaves the rump, runs along the rug and curls up, with a dark tip.
        for (int i = 10; i >= 0; i--)
        {
            float u = i / 10f, th = u * 2.2f;
            var p = new Vector2(442 + 14 * MathF.Sin(th) + MathF.Sin(t * 0.8f + u * 2) * u * 2f, 345 - 14 * (1 - MathF.Cos(th)) * 0.95f);
            Raylib.DrawCircleV(p, 5.4f - u * 3f, i == 10 || i % 3 == 2 ? dark : fur);
        }
        // Body: a round loaf with a lighter belly, a haunch with a tucked hind paw, and soft stripes across the back.
        Raylib.DrawEllipse(418, 340, 32, 13 + breathe, fur);
        Raylib.DrawEllipse(418, 347, 27, 5, Shade(light, 0.92f));
        Raylib.DrawCircleV(new Vector2(432, 341), 12 + breathe * 0.3f, Shade(fur, 1.1f));
        Raylib.DrawEllipse(424, 351, 7, 3, light);
        for (int k = 0; k < 4; k++)
        {
            float sx = 404 + k * 9;
            float u = (sx - 418) / 32f;
            float top = 340 - (13 + breathe) * MathF.Sqrt(MathF.Max(0, 1 - u * u));
            Raylib.DrawLineEx(new Vector2(sx, top + 1), new Vector2(sx + 2, top + 9), 2.6f, WithA(dark, 210));
        }
        // Soft dino plates along the spine, rising and falling with the breath, each with a pale tip.
        for (int k = 0; k < 6; k++)
        {
            float sx = 396 + k * 8;
            float u = (sx - 418) / 32f;
            float sy = 340 - (13 + breathe) * MathF.Sqrt(MathF.Max(0, 1 - u * u)) + 1.5f;
            float h = 8 - k * 0.6f;
            Tri(new Vector2(sx - 4, sy + 1), new Vector2(sx + 0.5f, sy - h), new Vector2(sx + 4, sy + 1), dark);
            Tri(new Vector2(sx - 1.4f, sy - h + 3), new Vector2(sx + 0.5f, sy - h), new Vector2(sx + 2.4f, sy - h + 3), Lerp(dark, light, 0.7f));
        }
        // Head: big and round resting low, with full cheeks, a pale muzzle, a blush, a closed smiling eye, pink-lined ears and two small crest plates.
        Raylib.DrawEllipse(400, 349, 14, 4, Shade(fur, 0.85f));
        float twitch = MathF.Sin(t * 45) * catEar * 4;
        Tri(new Vector2(379, 337), new Vector2(381, 324), new Vector2(391, 334), fur);
        Tri(new Vector2(381, 335), new Vector2(382, 328), new Vector2(388, 334), pink);
        Tri(new Vector2(390, 334), new Vector2(398 + twitch, 325), new Vector2(397, 338), fur);
        Tri(new Vector2(391.5f, 335), new Vector2(396 + twitch, 328.5f), new Vector2(395.5f, 337), pink);
        Raylib.DrawCircle(386, 343, 10, fur);
        Raylib.DrawEllipse(386, 347, 11.5f, 7, fur);
        Tri(new Vector2(383, 334), new Vector2(386, 328), new Vector2(389, 334), dark);
        Raylib.DrawEllipse(378, 348, 6.5f, 4.2f, light);
        Raylib.DrawCircleV(new Vector2(372.6f, 346.5f), 1.3f, pink);
        Raylib.DrawCircleV(new Vector2(381, 348), 2.6f, C(238, 140, 140, 70));
        Raylib.DrawLineEx(new Vector2(379, 341), new Vector2(382, 343.2f), 1.1f, dark);
        Raylib.DrawLineEx(new Vector2(382, 343.2f), new Vector2(385.5f, 341), 1.1f, dark);
        // Front paws tucked under the chin.
        Raylib.DrawEllipse(380, 353, 6, 3, light);
        Raylib.DrawEllipse(391, 353, 6, 3, light);
        Rlgl.PopMatrix();
        Sprites.End();
    }
    // A tall grandfather clock against the left wall: hood with the face, a glass door with a swinging pendulum, and a base.
    void DrawGrandfather()
    {
        var wood = C(92, 58, 36); var dark = C(54, 33, 21); var brass = C(196, 156, 74);
        Raylib.DrawRectangle(46, 96, 52, 228, wood);
        Raylib.DrawRectangle(42, 316, 60, 8, Shade(wood, 1.15f));
        Raylib.DrawRectangle(40, 324, 64, 22, dark);
        Raylib.DrawRectangle(40, 70, 64, 40, wood);
        Tri(new Vector2(40, 70), new Vector2(104, 70), new Vector2(72, 52), wood);
        Raylib.DrawRectangle(36, 108, 72, 5, Shade(wood, 1.2f));
        Clocks.DrawFace(72, 91, 15, brass, C(228, 216, 186), C(40, 28, 20), null);
        Raylib.DrawRectangle(56, 124, 32, 128, dark);
        Raylib.DrawRectangle(58, 126, 28, 124, C(30, 22, 18));
        // Pendulum: farthest left or right exactly when the clock ticks.
        float a = 0.14f * MathF.Cos((pend - TickAt) * 2 * MathF.PI);
        var pivot = new Vector2(72, 128);
        var bob = pivot + new Vector2(MathF.Sin(a), MathF.Cos(a)) * 92;
        Raylib.DrawLineEx(pivot, bob, 1.5f, C(150, 120, 60));
        Raylib.DrawCircleV(bob, 8, brass);
        Raylib.DrawCircleV(bob - new Vector2(2, 2), 3, C(236, 206, 130));
    }

    // The grandfather clock strikes the hour its own face shows (see Program.cs).
    public override bool Chimes => true;

    // Rare event: a log settles with a soft pop and sends up a shower of sparks (about 4 seconds).
    protected override float RareLength => 4;
    struct Spark { public float X, Y, Vx, Vy, Life, Max; }
    readonly List<Spark> sparks = new();
    protected override void OnRareStart()
    {
        audio.PlayEvent(Evt.Pop);
        for (int i = 0; i < 70; i++)
            sparks.Add(new Spark { X = Rf(290, 350), Y = Rf(296, 310), Vx = Rf(-45, 45), Vy = Rf(-150, -50), Life = 0, Max = Rf(1.2f, 2.8f) });
    }

    // Moves the sparks: they rise, slow down, drift and fade out.
    void UpdateSparks(float dt)
    {
        for (int i = sparks.Count - 1; i >= 0; i--)
        {
            var s = sparks[i];
            s.Life += dt; s.Vy += 70 * dt; s.X += s.Vx * dt; s.Y += s.Vy * dt;
            if (s.Life > s.Max || s.Y > 330) sparks.RemoveAt(i); else sparks[i] = s;
        }
    }

    // Draws the sparks as glowing dots, kept inside the hearth opening.
    void DrawSparks()
    {
        Raylib.BeginScissorMode(215, 150, 210, 185);
        Raylib.BeginBlendMode(BlendMode.Additive);
        foreach (var s in sparks)
        {
            float a = 1 - s.Life / s.Max;
            Raylib.DrawCircleV(new Vector2(s.X, s.Y), 1.3f, C(255, 190, 90, (int)(230 * a)));
            Glow(s.X, s.Y, 5, C(255, 140, 50, (int)(70 * a)));
        }
        Raylib.EndBlendMode();
        Raylib.EndScissorMode();
    }

    public override void Draw()
    {
        Raylib.DrawRectangleGradientV(0, 0, W, H, C(30, 22, 18), C(16, 11, 9));
        DrawGrandfather();

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


        // Candles on the mantel
        foreach (int cx in new[] { 190, 455 })
        {
            Raylib.DrawRectangle(cx - 3, 66, 6, 18, C(205, 195, 175));
            Raylib.DrawEllipse(cx, 62, 2.5f, 5 * candle, C(255, 210, 120));
        }

        // Logs behind and under the flames
        Raylib.DrawRectangleRounded(new Rectangle(255, 308, 130, 20), 0.5f, 6, C(52, 32, 19));
        float settle = RareT >= 0 && RareT < 1.6f ? 3 * MathF.Sin(MathF.PI * RareT / 1.6f) : 0; // the upper log sinks a little
        Raylib.DrawRectangleRounded(new Rectangle(275, 295 + settle, 110, 18), 0.5f, 6, C(66, 41, 24));
        fire.Draw();
        DrawSparks();
        Raylib.DrawRectangleRounded(new Rectangle(242, 320, 156, 15), 0.5f, 6, C(44, 27, 16));
        DrawCat();                                                                  // in front of the log, on the rug

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

