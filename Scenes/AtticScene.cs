using System.Numerics;
using Raylib_cs;
using static Gfx;

// ---------------------------------------------------------------------------
// Attic with a rain skylight: wooden rafters, a trunk and boxes, a small lamp, rain on the roof window,
// dust drifting in the light and a slow drip into a bucket.
// ---------------------------------------------------------------------------
public class AtticScene : Scene
{
    public override string Name => "Attic Skylight";
    protected override (Layer, string, float)[] SoundLayers => new[]
    {
        (Layer.RoofRain, "Rain on the roof", 0.75f), (Layer.Thunder, "Distant thunder", 0.3f), (Layer.Wind, "Wind", 0.15f),
    };
    // Thunder a long way off; no flash, the sky outside stays steady.
    public override (Evt, float, float)[] Events => new[] { (Evt.Thunder, 50f, 130f) };

    const float SkyX = 210, SkyY = 28, SkyW = 220, SkyH = 140;
    readonly RainWindow rain = new(SkyX, SkyY, SkyW, SkyH, 90, 40);
    struct Mote { public float X, Y, Vx, Vy, Phase; }
    readonly Mote[] motes = new Mote[44];
    float t, lamp = 1, lampTarget = 1, lampTimer, dripY = -10, dripWait = 2, ripple = -1;

    // Trunk and bucket for this launch: (trunk, trunk lid, bucket) in brown, green, blue or oxblood.
    static readonly (Color Trunk, Color Lid, Color Bucket)[] Looks =
    {
        (C(70, 44, 28), C(86, 54, 34), C(86, 92, 100)), (C(42, 64, 48), C(54, 80, 60), C(120, 70, 48)),
        (C(44, 56, 82), C(58, 72, 102), C(150, 154, 158)), (C(88, 38, 36), C(108, 50, 46), C(70, 96, 120)),
    };
    readonly (Color Trunk, Color Lid, Color Bucket) look = Looks[Launch.Pick(Looks.Length)];

    public AtticScene()
    {
        for (int i = 0; i < motes.Length; i++)
            motes[i] = new Mote { X = Rf(200, 440), Y = Rf(60, 330), Vx = Rf(-2, 2), Vy = Rf(1, 5), Phase = Rf(0, 6.28f) };
    }

    public override void Update(float dt)
    {
        t += dt;
        rain.Update(dt);
        for (int i = 0; i < motes.Length; i++)
        {
            ref Mote m = ref motes[i];
            m.X += (m.Vx + MathF.Sin(t * 0.4f + m.Phase) * 3) * dt;
            m.Y += m.Vy * dt;
            if (m.Y > 340) { m.Y = 60; m.X = Rf(200, 440); }
        }
        lampTimer -= dt;
        if (lampTimer <= 0) { lampTarget = Rf(0.9f, 1.05f); lampTimer = Rf(0.3f, 1.2f); }
        lamp += (lampTarget - lamp) * Math.Min(1, dt * 3);

        // A drip falls from a leak in the roof into the bucket, then rings spread on the water
        if (dripY < 0 && ripple < 0) { dripWait -= dt; if (dripWait <= 0) { dripY = 120; dripWait = Rf(2.5f, 5f); } }
        if (dripY >= 0) { dripY += 190 * dt; if (dripY > 300) { dripY = -10; ripple = 0; } }
        if (ripple >= 0) { ripple += dt; if (ripple > 1.6f) ripple = -1; }
    }

    // Rare event: a small bat flutters across the skylight against the stormy sky and is gone (about 8 seconds).
    protected override float RareLength => 8;

    protected override bool AnimalsVisible => RareT >= 0;
    protected override void DrawAnimals() => DrawBat();

    void DrawBat()
    {
        if (HideAnimals) return;
        float u = RareT / RareLength;                                        // 0 to 1 across the glass
        float x = SkyX - 20 + u * (SkyW + 40);
        float y = SkyY + 62 + MathF.Sin(u * 9) * 16 - MathF.Sin(u * MathF.PI) * 20;   // a swooping, wobbling path
        float flap = MathF.Sin(RareT * 16);                                  // wing beat
        Color c = C(14, 16, 24);
        if (!Sprites.Begin(0, new Vector2(x, y), 64, 40)) return;
        // Wings first: one connected membrane per side (shoulder, wrist, tip, then a scalloped trailing edge back to the body).
        foreach (float d in new[] { -1f, 1f })
        {
            var shoulder = new Vector2(x + d * 2, y - 2);
            var wrist = new Vector2(x + d * 12, y - 7 - 6 * flap);
            var tip = new Vector2(x + d * 27, y - 2 - 10 * flap);
            var s1 = new Vector2(x + d * 20, y + 4 - 6 * flap);       // the three dips along the back edge of the wing
            var s2 = new Vector2(x + d * 13, y + 3 - 2.5f * flap);
            var s3 = new Vector2(x + d * 7, y + 5);
            var hip = new Vector2(x + d * 2, y + 4);
            Tri(shoulder, wrist, tip, c); Tri(shoulder, tip, s1, c); Tri(shoulder, s1, s2, c); Tri(shoulder, s2, s3, c); Tri(shoulder, s3, hip, c);
            Raylib.DrawLineEx(wrist, tip, 1f, C(74, 86, 116));        // wing bone, a little lighter
        }
        Raylib.DrawEllipseV(new Vector2(x, y + 1), 3.6f, 6, c);                                                   // body
        Raylib.DrawCircleV(new Vector2(x, y - 5), 3.2f, c);                                                       // head
        Tri(new Vector2(x - 3, y - 6), new Vector2(x - 3.4f, y - 11), new Vector2(x - 0.6f, y - 7.5f), c);      // pointed ears
        Tri(new Vector2(x + 3, y - 6), new Vector2(x + 3.4f, y - 11), new Vector2(x + 0.6f, y - 7.5f), c);
        Sprites.End();
    }

    public override void Draw()
    {
        // Back wall and floor
        Raylib.DrawRectangleGradientV(0, 0, W, 250, C(30, 21, 16), C(22, 15, 12));
        Raylib.DrawRectangleGradientV(0, 250, W, 110, C(36, 25, 18), C(14, 10, 8));
        for (int x = 0; x < W; x += 26) Raylib.DrawLine(x, 0, x, 250, C(18, 12, 9, 120));
        for (int y = 262; y < H; y += 14) Raylib.DrawLine(0, y, W, y, C(16, 11, 8, 160));

        // Sloped ceiling beams leaning in toward the middle
        for (int i = 0; i < 6; i++)
        {
            float lx = 20 + i * 26;
            Tri(new Vector2(lx, 0), new Vector2(lx + 16, 0), new Vector2(lx + 60, 250), C(44, 30, 21));
            float rx = W - 20 - i * 26;
            Tri(new Vector2(rx, 0), new Vector2(rx - 16, 0), new Vector2(rx - 60, 250), C(44, 30, 21));
        }

        // An old wall clock hung on the back wall (silent)
        Raylib.DrawRectangle(78, 96, 4, 30, C(44, 30, 22));
        Clocks.DrawFace(80, 124, 16, C(96, 66, 40), C(196, 184, 156), C(44, 32, 24), C(150, 50, 40));

        // Skylight: stormy sky, rain streaks, frame and glass drops
        Raylib.DrawRectangleGradientV((int)SkyX, (int)SkyY, (int)SkyW, (int)SkyH, C(34, 44, 66), C(70, 84, 108));
        Raylib.BeginScissorMode((int)SkyX, (int)SkyY, (int)SkyW, (int)SkyH);
        if (RareT >= 0) DrawBat();
        rain.DrawStreaks();
        rain.DrawGlassDrops();
        Raylib.EndScissorMode();
        Raylib.DrawRectangleLinesEx(new Rectangle(SkyX - 6, SkyY - 6, SkyW + 12, SkyH + 12), 7, C(70, 48, 32));
        Raylib.DrawRectangle((int)(SkyX + SkyW / 2) - 2, (int)SkyY, 4, (int)SkyH, C(70, 48, 32));
        Raylib.DrawRectangle((int)SkyX, (int)(SkyY + SkyH / 2) - 2, (int)SkyW, 4, C(70, 48, 32));

        // Cool light falling from the skylight, with dust drifting through it
        Raylib.BeginBlendMode(BlendMode.Additive);
        Tri(new Vector2(SkyX, SkyY + SkyH), new Vector2(SkyX + SkyW, SkyY + SkyH), new Vector2(SkyX + SkyW + 90, 320), C(110, 140, 190, 14));
        Tri(new Vector2(SkyX, SkyY + SkyH), new Vector2(SkyX + 90 - 90, 320), new Vector2(SkyX + SkyW + 90, 320), C(110, 140, 190, 10));
        foreach (var m in motes)
        {
            float a = 0.5f + 0.5f * MathF.Sin(t * 0.7f + m.Phase);
            Raylib.DrawCircleV(new Vector2(m.X, m.Y), 1, C(200, 215, 240, (int)(70 * a)));
        }
        Raylib.EndBlendMode();

        // Trunk with straps, stacked boxes and a lamp on top
        Raylib.DrawRectangle(60, 270, 150, 56, look.Trunk);
        Raylib.DrawRectangle(60, 262, 150, 12, look.Lid);
        Raylib.DrawRectangle(90, 262, 8, 64, C(40, 28, 20));
        Raylib.DrawRectangle(172, 262, 8, 64, C(40, 28, 20));
        Raylib.DrawRectangle(126, 286, 18, 12, C(180, 140, 70));
        Raylib.DrawRectangle(214, 296, 70, 34, C(96, 76, 52));
        Raylib.DrawRectangle(226, 268, 52, 30, C(108, 86, 60));
        // Lamp: small shade on a base
        Raylib.DrawRectangle(142, 242, 6, 22, C(60, 46, 34));
        Tri(new Vector2(128, 244), new Vector2(162, 244), new Vector2(145, 220), C(230, 180, 100));
        Raylib.BeginBlendMode(BlendMode.Additive);
        Glow(145, 236, 120, C(255, 175, 90, (int)(70 * lamp)));
        Glow(145, 236, 40, C(255, 210, 140, (int)(60 * lamp)));
        Raylib.EndBlendMode();

        // Draped sheet over an old chair on the right
        Raylib.DrawRectangle(500, 252, 70, 74, C(92, 92, 100));
        Raylib.DrawEllipse(535, 252, 36, 12, C(112, 112, 120));
        Raylib.DrawRectangle(504, 322, 62, 5, C(64, 64, 70));

        // Bucket catching the drip
        Raylib.DrawRectangle(396, 300, 40, 28, look.Bucket);
        Raylib.DrawEllipse(416, 300, 20, 5, C(40, 60, 76));
        if (dripY >= 0) Raylib.DrawCircleV(new Vector2(416, dripY), 1.6f, C(160, 190, 220, 200));
        if (ripple >= 0)
        {
            float rr = ripple * 12;
            Raylib.DrawEllipseLines(416, 300, rr, rr * 0.25f, C(170, 200, 230, (int)(150 * (1 - ripple / 1.6f))));
        }

        Vignette(180);
    }
}
