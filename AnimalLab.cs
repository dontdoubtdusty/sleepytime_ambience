using System.Diagnostics;
using System.Numerics;
using Raylib_cs;
using static Gfx;

// Dev helper:  Drowse --animals <out.png>
// The "animal lab". For every animal in the scenes it draws the scene twice (with and without the animal), compares
// the two pictures to find exactly which pixels are the animal, and reports:
//   size      width x height and pixel area at the real 640x360 scene size (tiny animals are hard to read)
//   round     how round the outline is, 0 to 1 (1 is a circle; low means spiky or blocky; cute things are usually rounder)
//   contrast  how far the animal's colours are from what is behind it, 0 to 255 (low means it vanishes in the dark)
//   aspect    length over width (about 1 for a disc; a worm or a chain of beads is high; the koi redraw fixed "centipede")
//   solid     area over the area of its convex hull, 0 to 1 (low means ragged: thin limbs, gaps, loose pieces)
//   variety   spread of colour inside the animal (low means one flat blob; patterns, shading and highlights raise it)
//   cost      extra drawing time the animal adds to one frame, in microseconds
// It also saves a sheet with every animal enlarged, so the shapes can be judged by eye.
// These numbers are proxies. They catch "too small", "too dark", "too spiky" and "too slow"; whether it looks cozy
// is still a call made by looking at the sheet.
public static unsafe class AnimalLab
{
    record Case(string Name, int Scene, float RareT, bool RareOnly = false);   // RareOnly: compare with and without just the rare event, since the scene has other animals too

    // The scene number, and which second of its rare event to show (-1 for animals that are always on screen).
    static readonly Case[] Cases =
    {
        new("Cat-dino (fireplace)", 0, -1), new("Fox (cabin)", 1, 7), new("Rabbit (forest)", 2, 8), new("Moth (library)", 3, 10),
        new("Raccoon (tent)", 6, 5), new("Bat (attic)", 12, 4), new("Fish (aquarium)", 8, -1), new("Turtle (aquarium)", 8, 25, true),
        new("Koi (pond)", 13, -1), new("Dragonfly (pond)", 13, 10, true),
    };

    // Area of the convex hull of a set of points (monotone chain: sort, then build the lower and upper boundary).
    static double HullArea(List<(double X, double Y)> pts)
    {
        if (pts.Count < 3) return 1;
        var p = pts.OrderBy(q => q.X).ThenBy(q => q.Y).ToList();
        double Cross((double X, double Y) o, (double X, double Y) a, (double X, double Y) b) => (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
        var h = new List<(double X, double Y)>();
        for (int pass = 0; pass < 2; pass++)
        {
            int start = h.Count;
            foreach (var q in pass == 0 ? p : Enumerable.Reverse(p))
            {
                while (h.Count >= start + 2 && Cross(h[^2], h[^1], q) <= 0) h.RemoveAt(h.Count - 1);
                h.Add(q);
            }
            h.RemoveAt(h.Count - 1);
        }
        double area = 0;
        for (int i = 0; i < h.Count; i++) { var u = h[i]; var v = h[(i + 1) % h.Count]; area += u.X * v.Y - v.X * u.Y; }
        return Math.Abs(area) / 2;
    }

    // Draws the scene into the target texture.
    static void DrawScene(RenderTexture2D target, Scene sc)
    {
        sc.PrepareSprites();
        Raylib.BeginTextureMode(target);
        Raylib.ClearBackground(Color.Black);
        sc.Draw();
        Raylib.EndTextureMode();
    }

    // One full scene picture as RGB values.
    static Color[] Grab(RenderTexture2D target, Scene sc)
    {
        DrawScene(target, sc);
        var img = Raylib.LoadImageFromTexture(target.Texture);
        Raylib.ImageFlipVertical(ref img);
        var px = new Color[W * H];
        Color* p = (Color*)img.Data;
        for (int i = 0; i < px.Length; i++) px[i] = new Color((int)p[i].R, (int)p[i].G, (int)p[i].B, 255);
        Raylib.UnloadImage(img);
        return px;
    }

    // Average time of one scene draw in microseconds.
    static double DrawMicros(RenderTexture2D target, Scene sc, int runs)
    {
        for (int i = 0; i < 20; i++) DrawScene(target, sc);
        var w = Stopwatch.StartNew();
        for (int i = 0; i < runs; i++) DrawScene(target, sc);
        return w.Elapsed.TotalMilliseconds * 1000 / runs;
    }

    public static void Run(Scene[] scenes, RenderTexture2D target, string outPath)
    {
        const int Cols = 3, CellW = 420, CellH = 310, PicH = 230;
        int rows = (Cases.Length + Cols - 1) / Cols;
        var sheet = Raylib.LoadRenderTexture(Cols * CellW, rows * CellH);
        Raylib.SetTextureFilter(sheet.Texture, TextureFilter.Bilinear);
        Console.WriteLine("animal                    size       area  round aspect  solid variety  contrast  cost us  notes");

        // First pass: measure each animal and keep the picture and box for the sheet.
        var shots = new List<(Case C, Color[] Pic, int X0, int Y0, int X1, int Y1, double Round, double Aspect, double Solid, double Variety, double Contrast, double Cost, List<string> Notes)>();
        foreach (var c in Cases)
        {
            var sc = scenes[c.Scene];
            sc.EndRare();
            for (int i = 0; i < 120; i++) sc.Step(1 / 60f);
            sc.LabSetRare(c.RareT);

            HideAnimals = false; var a = Grab(target, sc);
            void Without() { if (c.RareOnly) { sc.LabSetRare(-1); HideAnimals = false; } else HideAnimals = true; }
            Without(); var b = Grab(target, sc); sc.LabSetRare(c.RareT);

            // Which pixels differ: that is the animal.
            var mask = new bool[W * H];
            for (int i = 0; i < mask.Length; i++)
                mask[i] = Math.Abs(a[i].R - b[i].R) + Math.Abs(a[i].G - b[i].G) + Math.Abs(a[i].B - b[i].B) > 18;

            // Group nearby pixels (dilate by 3) and keep the biggest group.
            var near = new bool[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (!mask[y * W + x]) continue;
                    for (int dy = -3; dy <= 3; dy++)
                        for (int dx = -3; dx <= 3; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < W && ny >= 0 && ny < H) near[ny * W + nx] = true;
                        }
                }
            var comp = new int[W * H];
            int best = 0, bestCount = 0, next = 0;
            var stack = new Stack<int>();
            for (int s = 0; s < comp.Length; s++)
            {
                if (!near[s] || comp[s] != 0) continue;
                int id = ++next, count = 0;
                comp[s] = id; stack.Push(s);
                while (stack.Count > 0)
                {
                    int p = stack.Pop();
                    if (mask[p]) count++;
                    int px = p % W, py = p / W;
                    if (px > 0 && near[p - 1] && comp[p - 1] == 0) { comp[p - 1] = id; stack.Push(p - 1); }
                    if (px < W - 1 && near[p + 1] && comp[p + 1] == 0) { comp[p + 1] = id; stack.Push(p + 1); }
                    if (py > 0 && near[p - W] && comp[p - W] == 0) { comp[p - W] = id; stack.Push(p - W); }
                    if (py < H - 1 && near[p + W] && comp[p + W] == 0) { comp[p + W] = id; stack.Push(p + W); }
                }
                if (count > bestCount) { bestCount = count; best = id; }
            }
            if (bestCount == 0) { Console.WriteLine($"{c.Name,-24}  not found (nothing differs)"); sc.EndRare(); continue; }

            int x0 = W, y0 = H, x1 = 0, y1 = 0, area = 0, edge = 0; double diff = 0;
            double sx = 0, sy = 0, sxx = 0, syy = 0, sxy = 0, sr = 0, sg = 0, sb = 0, srr = 0, sgg = 0, sbb = 0;
            var edgePts = new List<(double X, double Y)>();
            bool InAnimal(int q) => mask[q] && comp[q] == best;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    if (!InAnimal(i)) continue;
                    area++; x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y);
                    diff += (Math.Abs(a[i].R - b[i].R) + Math.Abs(a[i].G - b[i].G) + Math.Abs(a[i].B - b[i].B)) / 3.0;
                    sx += x; sy += y; sxx += (double)x * x; syy += (double)y * y; sxy += (double)x * y;
                    sr += a[i].R; sg += a[i].G; sb += a[i].B; srr += a[i].R * a[i].R; sgg += a[i].G * a[i].G; sbb += a[i].B * a[i].B;
                    if (x == 0 || y == 0 || x == W - 1 || y == H - 1 || !InAnimal(i - 1) || !InAnimal(i + 1) || !InAnimal(i - W) || !InAnimal(i + W)) { edge++; edgePts.Add((x, y)); }
                }
            double round = Math.Min(1, 4 * Math.PI * area / Math.Max(1.0, (double)edge * edge));
            double contrast = diff / area;

            // Plumpness: length over width from the spread of the pixels (about 1 for a disc, large for a worm or a bead chain).
            double mx = sx / area, my = sy / area;
            double cxx = sxx / area - mx * mx, cyy = syy / area - my * my, cxy = sxy / area - mx * my;
            double tr = cxx + cyy, det = Math.Sqrt(Math.Max(0, tr * tr / 4 - (cxx * cyy - cxy * cxy)));
            double aspect = Math.Sqrt((tr / 2 + det) / Math.Max(0.01, tr / 2 - det));

            // Solidity: the animal's area over the area of its convex hull (the rubber band stretched round it).
            // Near 1 is a single smooth blob; low means thin limbs, gaps or loose bits. Fins lower it a little, which is fine.
            double solid = area / Math.Max(1.0, HullArea(edgePts));

            // Colour variety: the spread of colour inside the animal. Very low means one flat blob with no shading or pattern.
            double n = area, vr = srr / n - (sr / n) * (sr / n), vg = sgg / n - (sg / n) * (sg / n), vb = sbb / n - (sb / n) * (sb / n);
            double variety = Math.Sqrt(Math.Max(0, (vr + vg + vb) / 3));

            HideAnimals = false; double withUs = DrawMicros(target, sc, 300);
            Without(); double withoutUs = DrawMicros(target, sc, 300); sc.LabSetRare(c.RareT);
            HideAnimals = false;
            double cost = withUs - withoutUs;

            var notes = new List<string>();
            if (area < 350) notes.Add("tiny");
            if (contrast < 25) notes.Add("hard to see");
            if (round < 0.2) notes.Add("spiky or blocky");
            if (aspect > 3.2) notes.Add("worm-like (too long and thin)");
            if (solid < 0.62) notes.Add("ragged outline (thin limbs or loose bits)");
            if (variety < 9) notes.Add("flat colour (no shading or pattern)");
            if (cost > 150) notes.Add("costly");
            Console.WriteLine($"{c.Name,-24} {x1 - x0 + 1,3}x{y1 - y0 + 1,-3} {area,7} {round,6:F2} {aspect,6:F1} {solid,6:F2} {variety,7:F0} {contrast,9:F0} {cost,8:F0}  {string.Join(", ", notes)}");
            shots.Add((c, a, x0, y0, x1, y1, round, aspect, solid, variety, contrast, cost, notes));
            sc.EndRare();
        }

        // Second pass: the sheet, each animal enlarged with its numbers underneath.
        var temp = new List<Texture2D>();   // freed only after the sheet is finished, since drawing is batched
        Raylib.BeginTextureMode(sheet);
        Raylib.ClearBackground(new Color(16, 14, 14, 255));
        for (int ci = 0; ci < shots.Count; ci++)
        {
            var s = shots[ci];
            var img = Raylib.GenImageColor(W, H, Color.Black);
            Color* dst = (Color*)img.Data;
            for (int i = 0; i < s.Pic.Length; i++) dst[i] = s.Pic[i];
            var tex = Raylib.LoadTextureFromImage(img);
            Raylib.SetTextureFilter(tex, TextureFilter.Bilinear);
            Raylib.UnloadImage(img);
            temp.Add(tex);
            int m = 16;
            var src = new Rectangle(Math.Max(0, s.X0 - m), Math.Max(0, s.Y0 - m), 0, 0);
            src.Width = Math.Min(W - src.X, s.X1 - s.X0 + 1 + 2 * m); src.Height = Math.Min(H - src.Y, s.Y1 - s.Y0 + 1 + 2 * m);
            float zoom = MathF.Min((CellW - 20f) / src.Width, (PicH - 10f) / src.Height);
            float cx = (ci % Cols) * CellW, cy = (ci / Cols) * CellH;
            var dest = new Rectangle(cx + (CellW - src.Width * zoom) / 2, cy + 5, src.Width * zoom, src.Height * zoom);
            Raylib.DrawTexturePro(tex, src, dest, Vector2.Zero, 0, Color.White);
            UiText.Draw(s.C.Name, cx + 12, cy + PicH + 8, 20, new Color(236, 218, 190, 255));
            UiText.Draw($"{s.X1 - s.X0 + 1}x{s.Y1 - s.Y0 + 1} px  round {s.Round:F2}  aspect {s.Aspect:F1}  solid {s.Solid:F2}", cx + 12, cy + PicH + 32, 14, new Color(190, 176, 150, 255));
            UiText.Draw($"variety {s.Variety:F0}  contrast {s.Contrast:F0}  cost {s.Cost:F0} us", cx + 12, cy + PicH + 50, 14, new Color(190, 176, 150, 255));
            if (s.Notes.Count > 0) UiText.Draw(string.Join(", ", s.Notes), cx + 12, cy + PicH + 68, 13, new Color(230, 140, 110, 255));
        }
        Raylib.EndTextureMode();
        foreach (var tx in temp) Raylib.UnloadTexture(tx);
        var si = Raylib.LoadImageFromTexture(sheet.Texture);
        Raylib.ImageFlipVertical(ref si);
        Raylib.ExportImage(si, outPath);
        Console.WriteLine($"sheet saved to {outPath}");
    }
}
