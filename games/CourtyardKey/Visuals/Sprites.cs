using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>The game's single sprite atlas, built once at load from the procedural art modules and the monster sheets.</summary>
public sealed class Sprites : IDisposable
{
    private readonly SpriteAtlas _atlas;

    private Sprites(SpriteAtlas atlas) => _atlas = atlas;

    /// <param name="device">The graphics device the atlas is uploaded to.</param>
    /// <param name="image">Reads a content image by path (the game passes <c>Assets.Image</c>).</param>
    public static Sprites Build(IGraphicsDevice device, Func<string, PixelImage> image)
    {
        var b = new SpriteAtlasBuilder();
        TileArt.AddAll(b);
        CharacterArt.AddHero(b);
        CharacterArt.AddVillagers(b);
        CreatureArt.AddAll(b);
        MonsterArt.AddAll(b, image);
        PropArt.AddAll(b);
        ItemArt.AddAll(b);
        TitleArt.AddAll(b);
        return new Sprites(b.Build(device));
    }

    public Sprite this[string name] => _atlas[name];
    public bool Contains(string name) => _atlas.Contains(name);

    /// <summary>The "nameN" frame sequence (the atlas collects each once).</summary>
    public IReadOnlyList<Sprite> Frames(string name) => _atlas.Frames(name);

    public void Dispose() => _atlas.Dispose();
}
