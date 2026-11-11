using System.Diagnostics;
using Raylib_cs;

// Dev helper:  Drowse --perf   runs every scene flat out (no frame limit) for a few seconds and prints
// how long a frame takes, how bad the worst frames are, and how much memory each frame allocates.
// Set SLEEPY_RARE_SECS=0.1 first to measure with each scene's rare event playing.
public static class Perf
{
    const int Warmup = 90, Frames = 600;

    /// <param name="present">Draws the scene texture to the window the way the real loop does (shader included).</param>
    /// <param name="opaque">Forces the scene texture opaque, as the real loop does.</param>
    public static void Run(Scene[] scenes, RenderTexture2D target, Action<Rectangle, float> present, Action opaque)
    {
        Console.WriteLine($"startup to first frame: {(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds:F0} ms (includes loading sounds and fonts)");
        Console.WriteLine("scene                       fps   avg ms  p99 ms  max ms  update  draw   KB/frame  gen0 GCs");
        var times = new double[Frames];
        for (int s = 0; s < scenes.Length; s++)
        {
            var sc = scenes[s];
            double upd = 0, drw = 0;
            long mem0 = 0; int gc0 = 0;
            for (int f = -Warmup; f < Frames; f++)
            {
                if (f == 0) { mem0 = GC.GetAllocatedBytesForCurrentThread(); gc0 = GC.CollectionCount(0); upd = drw = 0; }
                var frame = Stopwatch.StartNew();
                var part = Stopwatch.StartNew();
                sc.Step(1 / 60f);
                double u = part.Elapsed.TotalMilliseconds; part.Restart();
                sc.PrepareSprites();
                Raylib.BeginTextureMode(target);
                Raylib.ClearBackground(Color.Black);
                sc.Draw();
                opaque();
                Raylib.EndTextureMode();
                double d = part.Elapsed.TotalMilliseconds;
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                present(new Rectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight()), f / 60f);
                Raylib.EndDrawing();               // with no frame limit this waits for the graphics card, so it is the true frame time
                if (f >= 0) { times[f] = frame.Elapsed.TotalMilliseconds; upd += u; drw += d; }
            }
            double avg = times.Average();
            var sorted = times.OrderBy(x => x).ToArray();
            double p99 = sorted[(int)(Frames * 0.99)];
            double kb = (GC.GetAllocatedBytesForCurrentThread() - mem0) / 1024.0 / Frames;
            Console.WriteLine($"{s,2} {sc.Name,-22} {1000 / avg,7:F0} {avg,8:F2} {p99,7:F2} {sorted[^1],7:F2} {upd / Frames,7:F2} {drw / Frames,5:F2} {kb,9:F1} {GC.CollectionCount(0) - gc0,8}");
        }
    }
}
