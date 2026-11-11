using Raylib_cs;

// Base class for every scene. A scene simulates in Update(dt) and draws in Draw() into the
// 640x360 scene space (Gfx.W x Gfx.H). It knows nothing about the window or the controls.
public abstract class Scene
{
    public abstract string Name { get; }

    /// <summary>Sound layers specific to this scene, with default volumes (0..1).</summary>
    protected abstract (Layer Layer, string Label, float Default)[] SoundLayers { get; }

    (Layer Layer, string Label, float Default)[]? all;

    /// <summary>All sound layers shown for this scene: its own, then the noise layers (off by default).</summary>
    public (Layer Layer, string Label, float Default)[] Layers => all ??= SoundLayers
        .Concat(new[] { (Layer.Brown, "Brown noise", 0f), (Layer.Pink, "Pink noise", 0f) }).ToArray();

    /// <summary>Random sound events this scene plays now and then: which sound, and the shortest and longest wait (seconds) between plays.</summary>
    public virtual (Evt Kind, float MinSecs, float MaxSecs)[] Events => Array.Empty<(Evt, float, float)>();

    /// <summary>True when this scene has a striking clock: the app plays the hour chime when the clock face reaches a new hour.</summary>
    public virtual bool Chimes => false;

    // ---- one rare event per scene: a quiet surprise now and then (a shooting star, a ship, a visitor) ----

    // Test only: SLEEPY_RARE_SECS=n makes the first and every later event start n seconds into the scene;
    // SLEEPY_RARE_SPEED=k plays the event k times faster (so a slow event can be checked in a 5 second --shot).
    static readonly float? rareTest = float.TryParse(Environment.GetEnvironmentVariable("SLEEPY_RARE_SECS"), out var rs) ? rs : null;
    static readonly float rareSpeed = float.TryParse(Environment.GetEnvironmentVariable("SLEEPY_RARE_SPEED"), out var rsp) ? rsp : 1f;
    float rareWait = rareTest ?? Gfx.Rf(150, 500);

    /// <summary>How long this scene's rare event lasts in seconds (0 = the scene has none yet).</summary>
    protected virtual float RareLength => 0;

    /// <summary>Seconds since the rare event began, or -1 when none is running. The scene draws the event from this.</summary>
    protected float RareT = -1;

    /// <summary>Called once when a rare event begins: pick where it starts, which way it goes, and so on.</summary>
    protected virtual void OnRareStart() { }

    /// <summary>Stops any running event (called when the scene is switched away from).</summary>
    public void EndRare() => RareT = -1;

    /// <summary>Test only (the animal lab): starts the rare event and jumps to the given second of it; a negative value ends it.</summary>
    public void LabSetRare(float seconds) { if (seconds < 0) { RareT = -1; return; } OnRareStart(); RareT = seconds; }

    /// <summary>One simulation step: the scene's own Update, then the rare-event timer (an event comes every 7 to 23 minutes).</summary>
    public void Step(float dt)
    {
        Update(dt);
        if (RareLength <= 0) return;
        if (RareT >= 0) { RareT += dt * rareSpeed; if (RareT > RareLength) RareT = -1; }
        else if ((rareWait -= dt) <= 0) { RareT = 0; OnRareStart(); rareWait = rareTest ?? Gfx.Rf(420, 1380); }
    }

    /// <summary>True while this scene has an animal on screen that is drawn through Sprites (see Sprites.cs).</summary>
    protected virtual bool AnimalsVisible => false;

    /// <summary>Calls the draw method of each animal; it runs once in the prepare pass and once in the normal pass.</summary>
    protected virtual void DrawAnimals() { }

    /// <summary>Paints this scene's animals into the sprite atlas. Call once per frame before the scene texture is begun.</summary>
    public void PrepareSprites()
    {
        if (!AnimalsVisible || Gfx.HideAnimals) return;
        Sprites.BeginPass();
        DrawAnimals();
        Sprites.EndPass();
    }

    public abstract void Update(float dt);
    public abstract void Draw();
}
