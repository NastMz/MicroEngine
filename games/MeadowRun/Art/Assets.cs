using System.Numerics;
using MeadowRun.Content;
using MicroEngine.Graphics;

namespace MeadowRun.Art;

/// <summary>
/// Builds every sprite once at load into one atlas and exposes them by role. Tile lookups are
/// cached in arrays so the per-frame tile renderer never does string work.
/// </summary>
internal sealed class Assets : IDisposable
{
    public const int GroundVariants = 3;
    private readonly SpriteAtlas _atlas;
    private readonly Sprite[,,] _ground;
    private readonly Dictionary<(Biome, int), Sprite> _tiles = [];
    private readonly Dictionary<(PipForm, string), Sprite[]> _pip = [];

    public Assets(IGraphicsDevice device)
    {
        var b = new SpriteAtlasBuilder();
        var biomes = Enum.GetValues<Biome>();
        foreach (var biome in biomes)
        {
            string k = Key(biome);
            for (int mask = 0; mask < 16; mask++)
            for (int v = 0; v < GroundVariants; v++)
                b.Add($"ground_{k}_{mask}_{v}", TileArt.Ground(biome, mask, v));
            b.Add("brick_" + k, TileArt.Brick(biome));
            b.Add("hard_" + k, TileArt.Hard(biome));
            b.Add("plank_" + k, TileArt.Plank(biome));
            b.Add("spikes_" + k, TileArt.Spikes(biome));
            for (int part = 0; part < 3; part++) b.Add($"platform_{k}_{part}", ItemArt.Platform(biome, part));
            b.Add("shard_" + k, ItemArt.Shard(biome), new Vector2(3, 3));
        }
        for (int f = 0; f < 5; f++) b.Add("question" + f, TileArt.Question(f));
        for (int f = 0; f < 4; f++) b.Add("lava_top" + f, TileArt.Lava(f, true));
        for (int f = 0; f < 2; f++) b.Add("lava_body" + f, TileArt.Lava(f, false));
        b.Add("backwall", TileArt.BackWall());
        for (int v = 0; v < BackWallVariants; v++) b.Add("backwall" + v, TileArt.BackWall(v));
        foreach (int side in new[] { 1, 2, 4, 8 }) b.Add("wallframe" + side, TileArt.WallFrame(side));

        foreach (var form in Enum.GetValues<PipForm>())
        foreach (var clip in HeroArt.ClipNames)
            b.AddFrames($"pip_{form}_{clip}", HeroArt.Frames(clip, form), HeroArt.Pivot);

        b.AddFrames("coin", ItemArt.Coin(), new Vector2(6, 7));
        b.AddFrames("sunpetal", Enumerable.Range(0, 4).Select(ItemArt.Sunpetal), new Vector2(8, 16));
        b.AddFrames("emberseed", Enumerable.Range(0, 2).Select(ItemArt.EmberSeed), new Vector2(8, 16));
        b.Add("lifesprout", ItemArt.LifeSprout(), new Vector2(8, 16));
        b.AddFrames("spring", Enumerable.Range(0, 2).Select(ItemArt.Spring), new Vector2(8, 16));
        b.AddFrames("checkpoint", Enumerable.Range(0, 3).Select(ItemArt.Checkpoint), new Vector2(6, 32));
        b.Add("goalpole", ItemArt.GoalPole(9 * 16), new Vector2(4, 9 * 16));
        b.AddFrames("pennant", Enumerable.Range(0, 4).Select(ItemArt.Pennant), new Vector2(0, 6));
        b.AddFrames("seedshot", ItemArt.SeedShot(), new Vector2(4, 4));
        b.AddFrames("fireball", Enumerable.Range(0, 2).Select(ItemArt.Fireball), new Vector2(5, 5));
        b.Add("thorn", ItemArt.Thorn(), new Vector2(4, 4));
        b.Add("rock", ItemArt.Rock(), new Vector2(7, 7));
        b.AddFrames("sunstone", Enumerable.Range(0, 4).Select(ItemArt.Sunstone), new Vector2(10, 10));

        b.AddFrames("bumblet", Enumerable.Range(0, 3).Select(EnemyArt.Bumblet), new Vector2(8, 16));
        b.AddFrames("thornback", Enumerable.Range(0, 2).Select(EnemyArt.Thornback), new Vector2(9, 16));
        b.AddFrames("hopkin", Enumerable.Range(0, 3).Select(EnemyArt.Hopkin), new Vector2(8, 16));
        b.AddFrames("mothwing", Enumerable.Range(0, 2).Select(EnemyArt.Mothwing), new Vector2(9, 9));
        b.AddFrames("spitpod", Enumerable.Range(0, 3).Select(EnemyArt.Spitpod), new Vector2(8, 20));
        var boss = Enumerable.Range(0, 8).Select(EnemyArt.Grumbold).ToList();
        b.AddFrames("grumbold", boss, new Vector2(24, 40));
        b.AddFrames("grumbold_flash", boss.Select(i => i.Silhouette(Pal.Cream)), new Vector2(24, 40));
        b.AddFrames("mound", Enumerable.Range(0, 2).Select(EnemyArt.Mound), new Vector2(16, 12));

        foreach (var (name, image) in DecoArt.All())
            b.Add(name, image, name.StartsWith("glow", StringComparison.Ordinal) ? new Vector2(image.Width / 2f, image.Height / 2f) : new Vector2(image.Width / 2f, image.Height));
        foreach (var (name, image) in BackdropArt.All()) b.Add(name, image);

        b.Add("logo", UiArt.Logo());
        b.Add("icon_coin", UiArt.CoinIcon());
        b.Add("icon_life", UiArt.LifeIcon());
        b.Add("icon_clock", UiArt.ClockIcon());
        b.Add("icon_star", UiArt.StarIcon(true));
        b.Add("icon_star_empty", UiArt.StarIcon(false));
        for (int s = 0; s < 4; s++) b.Add("node" + s, UiArt.MapNode(s), new Vector2(8, 7));
        b.Add("map", UiArt.MapBackdrop());

        _atlas = b.Build(device);

        _ground = new Sprite[biomes.Length, 16, GroundVariants];
        foreach (var biome in biomes)
        {
            string k = Key(biome);
            for (int mask = 0; mask < 16; mask++)
            for (int v = 0; v < GroundVariants; v++)
                _ground[(int)biome, mask, v] = _atlas[$"ground_{k}_{mask}_{v}"];
            _tiles[(biome, Tile.Brick)] = _atlas["brick_" + k];
            _tiles[(biome, Tile.MultiCoinBrick)] = _atlas["brick_" + k];
            _tiles[(biome, Tile.HardBlock)] = _atlas["hard_" + k];
            _tiles[(biome, Tile.OneWay)] = _atlas["plank_" + k];
            _tiles[(biome, Tile.Spikes)] = _atlas["spikes_" + k];
            _tiles[(biome, Tile.UsedBlock)] = _atlas["question4"];
            _tiles[(biome, Tile.BackWall)] = _atlas["backwall"];
        }
        foreach (var form in Enum.GetValues<PipForm>())
        foreach (var clip in HeroArt.ClipNames)
            _pip[(form, clip)] = _atlas.Frames($"pip_{form}_{clip}");
    }

    public static string Key(Biome biome) => biome.ToString().ToLowerInvariant();

    public Sprite this[string name] => _atlas[name];
    public Sprite[] Frames(string name) => _atlas.Frames(name);
    public Sprite Ground(Biome biome, int mask, int variant) => _ground[(int)biome, mask, variant];
    public const int BackWallVariants = 4;

    public Sprite? TileSprite(Biome biome, int id) => _tiles.TryGetValue((biome, id), out var s) ? s : null;
    public Sprite[] Pip(PipForm form, string clip) => _pip[(form, clip)];

    public void Dispose() => _atlas.Dispose();
}
