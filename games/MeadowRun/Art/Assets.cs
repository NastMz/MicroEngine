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
    private readonly Dictionary<(PipForm, string), IReadOnlyList<Sprite>> _pip = [];
    private readonly Dictionary<(PipForm, string), AnimationClip> _pipClips = [];

    /// <summary>
    /// A strip that plays whole at one rate is a clip from the moment its frames are added (AddFrames with a rate).
    /// These are the others: part of a strip, a strip at a second rate, frames added one by one, and the question
    /// block, which shines for 0.4 s and rests for a second. Drawing asks a clip for the frame of a time and holds
    /// no rate. What is no clip: Pip's run in a level follows the distance he covers, and the Mole King picks poses.
    /// </summary>
    private static readonly SheetClip[] Clips =
    [
        Strip("coin_pop", "coin", 0, 6, 24),
        Strip("checkpoint_lit", "checkpoint", 1, 2, 6),
        Strip("bumblet_walk", "bumblet", 0, 2, 6),
        Strip("torch", "torch", 0, 3, 9),
        new("question", ["question0", "question1", "question2", "question3", "question0"], [0.1f, 0.1f, 0.1f, 0.1f, 1f], 0),
    ];

    /// <summary>The rate of Pip's clips that play by time, in every form: the others follow what he does.</summary>
    private static float PipRate(string clip) => clip switch { "idle" => 1.6f, "victory" => 4, "run" => 10, _ => 0 };

    private static SheetClip Strip(string name, string strip, int from, int count, float framesPerSecond) =>
        new(name, [.. Enumerable.Range(from, count).Select(i => strip + i)], null, framesPerSecond);
    private readonly Sprite[] _formIcons;

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
        b.AddFrames("lava_top", Enumerable.Range(0, 4).Select(f => TileArt.Lava(f, true)), 5);
        b.AddFrames("lava_body", Enumerable.Range(0, 2).Select(f => TileArt.Lava(f, false)), 5);
        for (int v = 0; v < BackWallVariants; v++) b.Add("backwall" + v, TileArt.BackWall(v));
        foreach (int side in new[] { 1, 2, 4, 8 }) b.Add("wallframe" + side, TileArt.WallFrame(side));

        foreach (var form in Enum.GetValues<PipForm>())
        foreach (var clip in HeroArt.ClipNames)
        {
            if (PipRate(clip) > 0) b.AddFrames($"pip_{form}_{clip}", HeroArt.Frames(clip, form), PipRate(clip), HeroArt.Pivot);
            else b.AddFrames($"pip_{form}_{clip}", HeroArt.Frames(clip, form), HeroArt.Pivot);
        }

        b.AddFrames("coin", ItemArt.Coin(), 8, new Vector2(6, 7));
        b.AddFrames("sunpetal", Enumerable.Range(0, 4).Select(ItemArt.Sunpetal), 6, new Vector2(8, 16));
        b.AddFrames("emberseed", Enumerable.Range(0, 2).Select(ItemArt.EmberSeed), 4, new Vector2(8, 16));
        b.Add("lifesprout", ItemArt.LifeSprout(), new Vector2(8, 16));
        b.AddFrames("spring", Enumerable.Range(0, 2).Select(ItemArt.Spring), new Vector2(8, 16));
        b.AddFrames("checkpoint", Enumerable.Range(0, 3).Select(ItemArt.Checkpoint), new Vector2(6, 32));
        b.Add("goalpole", ItemArt.GoalPole(9 * 16), new Vector2(4, 9 * 16));
        b.AddFrames("pennant", Enumerable.Range(0, 4).Select(ItemArt.Pennant), 8, new Vector2(0, 6));
        b.AddFrames("seedshot", ItemArt.SeedShot(), 16, new Vector2(4, 4));
        b.AddFrames("fireball", Enumerable.Range(0, 2).Select(ItemArt.Fireball), 12, new Vector2(5, 5));
        b.Add("thorn", ItemArt.Thorn(), new Vector2(4, 4));
        b.Add("rock", ItemArt.Rock(), new Vector2(7, 7));
        b.AddFrames("sunstone", Enumerable.Range(0, 4).Select(ItemArt.Sunstone), 6, new Vector2(10, 10));

        b.AddFrames("bumblet", Enumerable.Range(0, 3).Select(EnemyArt.Bumblet), new Vector2(8, 16));
        b.AddFrames("thornback", Enumerable.Range(0, 2).Select(EnemyArt.Thornback), 5, new Vector2(9, 16));
        b.AddFrames("hopkin", Enumerable.Range(0, 3).Select(EnemyArt.Hopkin), new Vector2(8, 16));
        b.AddFrames("mothwing", Enumerable.Range(0, 2).Select(EnemyArt.Mothwing), 10, new Vector2(9, 9));
        b.AddFrames("spitpod", Enumerable.Range(0, 3).Select(EnemyArt.Spitpod), new Vector2(8, 20));
        var boss = Enumerable.Range(0, 8).Select(EnemyArt.Grumbold).ToList();
        b.AddFrames("grumbold", boss, new Vector2(24, 40));
        b.AddFrames("grumbold_flash", boss.Select(i => i.Silhouette(Pal.Cream)), new Vector2(24, 40));
        b.AddFrames("mound", Enumerable.Range(0, 2).Select(EnemyArt.Mound), 8, new Vector2(16, 12));

        foreach (var (name, image) in DecoArt.All())
            b.Add(name, image, name.StartsWith("glow", StringComparison.Ordinal) ? new Vector2(image.Width / 2f, image.Height / 2f) : new Vector2(image.Width / 2f, image.Height));
        foreach (var (name, image) in BackdropArt.All()) b.Add(name, image);

        b.Add("logo", UiArt.Logo());
        b.Add("icon_coin", UiArt.CoinIcon());
        b.Add("icon_life", UiArt.LifeIcon());
        b.Add("icon_clock", UiArt.ClockIcon());
        foreach (var form in Enum.GetValues<PipForm>()) b.Add("icon_form_" + form, UiArt.FormIcon(form));
        for (int s = 0; s < 4; s++) b.Add("node" + s, UiArt.MapNode(s), new Vector2(8, 7));
        b.Add("map", UiArt.MapBackdrop());

        foreach (var clip in Clips) b.AddClip(clip);

        _atlas = b.Build(device);
        foreach (var form in Enum.GetValues<PipForm>())
        foreach (var clip in HeroArt.ClipNames)
            if (PipRate(clip) > 0) _pipClips[(form, clip)] = _atlas.Clip($"pip_{form}_{clip}");

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
            _tiles[(biome, Tile.BackWall)] = _atlas["backwall0"];
        }
        foreach (var form in Enum.GetValues<PipForm>())
        foreach (var clip in HeroArt.ClipNames)
            _pip[(form, clip)] = _atlas.Frames($"pip_{form}_{clip}");
        _formIcons = Array.ConvertAll(Enum.GetValues<PipForm>(), form => _atlas["icon_form_" + form]);
    }

    private static readonly string[] Keys = Array.ConvertAll(Enum.GetValues<Biome>(), b => b.ToString().ToLowerInvariant());

    /// <summary>The biome's lower-case name used in sprite names (cached: it is looked up while drawing).</summary>
    public static string Key(Biome biome) => Keys[(int)biome];

    public Sprite this[string name] => _atlas[name];
    public IReadOnlyList<Sprite> Frames(string name) => _atlas.Frames(name);
    /// <summary>An animation by name: <c>Clip("coin").FrameAt(time)</c> is the frame to draw.</summary>
    public AnimationClip Clip(string name) => _atlas.Clip(name);
    public Sprite Ground(Biome biome, int mask, int variant) => _ground[(int)biome, mask, variant];
    public const int BackWallVariants = 4;

    public Sprite? TileSprite(Biome biome, int id) => _tiles.TryGetValue((biome, id), out var s) ? s : null;
    public IReadOnlyList<Sprite> Pip(PipForm form, string clip) => _pip[(form, clip)];
    /// <summary>One of Pip's animations that play by time ("idle", "victory", and "run" on the map).</summary>
    public AnimationClip PipClip(PipForm form, string clip) => _pipClips[(form, clip)];
    /// <summary>The HUD's badge for Pip's form (cached: it is drawn every frame).</summary>
    public Sprite FormIcon(PipForm form) => _formIcons[(int)form];

    public void Dispose() => _atlas.Dispose();
}
