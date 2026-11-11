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

    public abstract void Update(float dt);
    public abstract void Draw();
}
