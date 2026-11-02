using System.Globalization;
using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;

namespace AssetGallery;

/// <summary>
/// One screen: a backdrop from PNG and JPEG files, animated characters from two atlas formats, a folder atlas, text in three
/// fonts at several sizes, and the asset cache's own numbers. Confirm switches the characters between linear and nearest
/// sampling; action loads (or releases) a "level" scope with a tile atlas.
/// </summary>
public sealed class GalleryScene(GalleryGame game) : Scene
{
    public const float Small = 18, Body = 22, Label = 28, Prose = 26, Title = 64;
    private static readonly string[] Walkers = ["playerBlue", "playerGreen", "playerRed", "playerGrey"];
    private static readonly string[] Tags = ["floating", "flying", "spikey", "swimming", "walking"];
    private const string Paragraph = "Every pixel on this screen comes from a file: PNG and JPEG images, a Starling XML atlas, " +
        "an Aseprite JSON export with its animation tags, a folder of loose sprites packed at load, and TrueType and OpenType fonts drawn at any size.";

    private AssetCache? _level;
    private AssetBatch? _loading;
    private SpriteAtlas? _tiles;
    private bool _nearest;

    /// <summary>Whether the level scope's tile atlas is loaded.</summary>
    public bool LevelLoaded => _tiles is not null;
    /// <summary>The sampling the character atlases use now.</summary>
    public TextureFilter CharacterFilter => game.Players.Texture.Filter;

    public override void Update(float dt)
    {
        if (Input.Pressed("confirm"))
        {
            _nearest = !_nearest;
            var filter = _nearest ? TextureFilter.Nearest : TextureFilter.Linear;
            game.Players.Texture.Filter = filter;
            game.Enemies.Texture.Filter = filter;
        }
        if (Input.Pressed("action"))
        {
            if (_level is null)
            {
                // A level's assets live in their own scope and go away with it. Preloading decodes on a worker and
                // uploads within the frame budget, so switching levels during play does not stall a frame.
                _level = Assets.CreateScope("level");
                _loading = _level.Preload("atlases/tiles.xml");
            }
            else
            {
                _level.Dispose();
                (_level, _loading, _tiles) = (null, null, null);
            }
        }
        if (_loading is { IsDone: true } && _level is not null)
        {
            _tiles = _level.Atlas("atlases/tiles.xml");
            _loading = null;
        }
    }

    public override void Exit() => _level?.Dispose();

    public override void Draw(IRenderer r)
    {
        // Backdrops: a PNG stretched over the left half, the JPEG over the right; linear sampling smooths the scaling.
        r.DrawStretched(game.Sky, game.Sky.Bounds, new RectF(0, 0, 640, 480), Color.White);
        r.DrawStretched(game.Dusk, game.Dusk.Bounds, new RectF(640, 0, 640, 480), Color.White);
        r.DrawStretched(game.Hills, game.Hills.Bounds, new RectF(0, 0, 640, 480), Color.White);
        r.FillRect(new RectF(0, 480, 1280, 240), Color.Hex("1d2433"));

        game.Bold.Draw(r, "Asset Gallery", new Vector2(40, 28), Title, Color.White);
        game.Ui.Draw(r, "HD canvas 1280x720, smooth scaling, files only", new Vector2(44, 104), Label, Color.Hex("e8eefc"));

        // Characters from the Starling atlas, walking in place at two scales.
        for (int i = 0; i < Walkers.Length; i++)
        {
            var frames = game.Players.Frames(Walkers[i] + "_walk");
            var frame = frames[(int)(Age * 10) % frames.Count];
            r.DrawPivoted(frame, new Vector2(110 + i * 130, 400), scale: new Vector2(i == 0 ? 2.5f : 1.5f));
        }
        // Enemies from the Aseprite export: each tag's clip, with the export's frame durations.
        for (int i = 0; i < Tags.Length; i++)
            r.DrawPivoted(game.Enemies.Clip(Tags[i]).FrameAt(Age), new Vector2(720 + i * 110, 400), scale: new Vector2(1.5f));
        // The folder atlas: one texture for every loose item image.
        int n = 0;
        foreach (var name in game.Items.Names.Order(StringComparer.Ordinal))
            r.DrawPivoted(game.Items[name], new Vector2(700 + n++ * 56, 160 + MathF.Sin(Age * 3 + n) * 6));
        if (_tiles is not null)
            for (int x = 0; x < 640; x += 64) r.DrawStretched(_tiles.Texture, _tiles["tileGreen_02"].Source, new RectF(x, 416, 64, 64), Color.White);

        // Text: wrapped prose in a serif OTF, interface text in a TTF at several sizes, and the built-in pixel font.
        float y = 500;
        foreach (var line in game.Serif.At(Prose).Wrap(Paragraph, 760))
        {
            game.Serif.Draw(r, line, new Vector2(40, y), Prose, Color.Hex("f4f1ea"));
            y += game.Serif.LineHeight(Prose);
        }
        var stats = Assets.Statistics;
        string memory = string.Create(CultureInfo.InvariantCulture, $"{stats.Count} assets, {stats.TextureBytes / 1048576.0:0.0} MB of textures, loaded in {stats.LoadMilliseconds:0} ms");
        game.Ui.At(Body).DrawShadowed(r, memory, new Vector2(840, 506), Color.White, Color.Black, new Vector2(2));
        game.Ui.Draw(r, $"Characters: {CharacterFilter} sampling (confirm switches)", new Vector2(840, 548), Small, Color.Hex("c9d3ea"));
        game.Ui.Draw(r, $"Level scope: {(LevelLoaded ? "loaded" : _loading is not null ? "loading" : "released")} (action switches)", new Vector2(840, 576), Small, Color.Hex("c9d3ea"));
        game.Ui.Draw(r, $"Glyph pages: {game.Ui.Pages + game.Bold.Pages + game.Serif.Pages}", new Vector2(840, 604), Small, Color.Hex("c9d3ea"));
        Font.Draw(r, "THE BUILT-IN PIXEL FONT STILL WORKS", new Vector2(840, 650), Color.Hex("ffd966"), scale: 2);
    }
}
