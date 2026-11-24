using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>The game's single sprite atlas, built once at load from the procedural art modules and the pack sheets.</summary>
public sealed class Sprites : IDisposable
{
    private readonly SpriteAtlas _atlas;

    private Sprites(SpriteAtlas atlas) => _atlas = atlas;

    /// <summary>The description of the pieces cut from the pack images (<c>Content/pack.sheet</c>).</summary>
    public const string PackSheet = "pack.sheet";

    /// <param name="device">The graphics device the atlas is uploaded to.</param>
    /// <param name="pack">The pack pieces as pixels (the game passes <c>Assets.Sheet(PackSheet)</c>).</param>
    public static Sprites Build(IGraphicsDevice device, SpriteSheet pack)
    {
        var b = new SpriteAtlasBuilder();
        GroundArt.AddAll(b);
        DungeonArt.AddAll(b);
        TileArt.AddAll(b);
        PackArt.AddAll(b, pack);
        CharacterArt.AddHero(b);
        CharacterArt.AddVillagers(b);
        CreatureArt.AddAll(b);
        MonsterArt.AddAll(b, pack);
        PropArt.AddAll(b);
        ItemArt.AddAll(b);
        TitleArt.AddAll(b);
        return new Sprites(b.Build(device));
    }

    public Sprite this[string name] => _atlas[name];
    public bool Contains(string name) => _atlas.Contains(name);

    private readonly Dictionary<string, Sprite> _flashes = [];

    /// <summary>The white hit-flash twin of a sprite (<c>"name!"</c>), looked up once per name instead of every frame of a flash.</summary>
    public Sprite Flash(string name)
    {
        if (!_flashes.TryGetValue(name, out var sprite)) _flashes[name] = sprite = _atlas[name + "!"];
        return sprite;
    }

    /// <summary>The "nameN" frame sequence (the atlas collects each once).</summary>
    public IReadOnlyList<Sprite> Frames(string name) => _atlas.Frames(name);

    public void Dispose() => _atlas.Dispose();
}
