using System.Numerics;
using Raylib_cs;

// Smooth animals. The scenes are drawn at 640x360, which makes small shapes look jagged and makes slow movement
// step from pixel to pixel. Each animal is therefore first drawn four times bigger into a hidden "atlas" picture
// (one big off-screen texture that holds all the animals' pictures side by side), then shrunk into the scene with
// smoothing. The shrinking averages 4x4 blocks of pixels, which is what gives soft edges (anti-aliasing) and lets
// an animal sit on a fraction of a pixel instead of snapping to whole ones.
//
// How a scene uses it: the animal's draw method calls Begin before it paints and End after. The same method runs twice
// per frame. First in the "prepare" pass (before the scene is drawn): Begin returns true and the animal paints itself
// into the atlas. Then in the normal pass: Begin pastes the finished picture into the scene and returns false,
// so the method returns early. The painting code itself is the same ordinary drawing code with scene coordinates.
// Do not use additive blending (glow) while painting an animal; draw glows in the normal pass instead.
public static class Sprites
{
    public const int SS = 4;                    // how many times bigger the animals are drawn
    const int AtlasPx = 2048, Pad = 4, MaxSlots = 32;

    static RenderTexture2D atlas;
    static bool ready, filtered, used;
    static readonly Rectangle[] src = new Rectangle[MaxSlots];
    static readonly Vector2[] pos = new Vector2[MaxSlots], size = new Vector2[MaxSlots];
    static readonly bool[] has = new bool[MaxSlots];
    static int curX, curY, rowH;

    /// <summary>True while the animals are being painted into the atlas (before the scene itself is drawn).</summary>
    public static bool Preparing { get; private set; }

    /// <summary>Starts the prepare pass. Call before the scene texture is begun, then paint animals, then EndPass.</summary>
    public static void BeginPass()
    {
        if (!ready)
        {
            atlas = Raylib.LoadRenderTexture(AtlasPx, AtlasPx);
            ready = true;
        }
        Preparing = true; used = false;
        Array.Clear(has);
        curX = curY = rowH = 0;
        Raylib.BeginTextureMode(atlas);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        // Draw so that the atlas keeps "premultiplied" colour (colour already multiplied by its own opacity) with a correct opacity.
        Rlgl.SetBlendFactorsSeparate(0x0302, 0x0303, 1, 0x0303, 0x8006, 0x8006);
        Raylib.BeginBlendMode(BlendMode.CustomSeparate);
    }

    /// <summary>Ends the prepare pass and builds the shrunken versions (mipmaps) the smooth pasting uses.</summary>
    public static void EndPass()
    {
        Raylib.EndBlendMode();
        Raylib.EndTextureMode();
        Preparing = false;
        if (!used) return;
        Raylib.GenTextureMipmaps(ref atlas.Texture);
        if (!filtered) { Raylib.SetTextureFilter(atlas.Texture, TextureFilter.Trilinear); filtered = true; }
    }

    /// <summary>
    /// In the prepare pass: reserves room for an animal that is about w by h scene pixels around centre and returns true
    /// (paint it, then call End). In the normal pass: pastes that animal into the scene and returns false.
    /// id is any number from 0 to 31 that stays the same for the same animal in both passes.
    /// </summary>
    public static bool Begin(int id, Vector2 center, int w, int h)
    {
        if (!Preparing)
        {
            if (has[id]) Blit(id);
            return false;
        }
        int cw = w + 2 * Pad, ch = h + 2 * Pad;
        if (curX + cw > AtlasPx / SS) { curX = 0; curY += rowH; rowH = 0; }
        if (curY + ch > AtlasPx / SS || id >= MaxSlots) return false;    // no room left: leave this animal out
        rowH = Math.Max(rowH, ch);
        int cx = curX, cy = curY;
        curX += cw;
        // The atlas is stored upside down, so the rectangle read back later is flipped (negative height).
        src[id] = new Rectangle((cx + Pad) * SS, AtlasPx - (cy + Pad + h) * SS, w * SS, -h * SS);
        pos[id] = center; size[id] = new Vector2(w, h); has[id] = true; used = true;
        Raylib.BeginScissorMode(cx * SS, cy * SS, cw * SS, ch * SS);    // keeps a stray stroke out of the next animal's space
        Rlgl.PushMatrix();
        Rlgl.Translatef(SS * (cx + cw / 2f), SS * (cy + ch / 2f), 0);
        Rlgl.Scalef(SS, SS, 1);
        Rlgl.Translatef(-center.X, -center.Y, 0);
        return true;
    }

    /// <summary>Finishes painting one animal (prepare pass only).</summary>
    public static void End()
    {
        Rlgl.PopMatrix();
        Raylib.EndScissorMode();
    }

    // Pastes one finished animal into the scene at its place, shrunk 4x with smoothing.
    static void Blit(int id)
    {
        var s = size[id];
        Raylib.BeginBlendMode(BlendMode.AlphaPremultiply);
        Raylib.DrawTexturePro(atlas.Texture, src[id], new Rectangle(pos[id].X - s.X / 2f, pos[id].Y - s.Y / 2f, s.X, s.Y), Vector2.Zero, 0, Color.White);
        Raylib.EndBlendMode();
    }
}
