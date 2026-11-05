using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Kernel;

namespace AssetGallery;

/// <summary>
/// An HD game built only from content files: a 1280×720 canvas scaled smoothly, linear textures by default, two atlases
/// from packing tools (Starling XML and Aseprite JSON), a folder of loose sprites packed at load, a JPEG and three fonts.
/// </summary>
public sealed class GalleryGame : Game
{
    private readonly string? _contentRoot;

    /// <summary>Creates the gallery; tests pass the content folder of the sources.</summary>
    public GalleryGame(string? contentRoot = null) => _contentRoot = contentRoot;

    /// <inheritdoc/>
    public override GameSettings Settings { get; } = new("Asset Gallery", 1280, 720, 1)
    {
        Presentation = Presentation.HighResolution2D,
        Background = Color.Hex("1d2433"),
        Letterbox = Color.Hex("141925"),
    };

    /// <inheritdoc/>
    protected override string? ContentRoot => _contentRoot;

    /// <summary>The fonts: Inter (TTF and OTF) for the interface, Source Serif (OTF with CFF outlines) for prose.</summary>
    public ScalableFont Ui { get; private set; } = null!;
    /// <summary>Inter Bold, an OTF file, for titles.</summary>
    public ScalableFont Bold { get; private set; } = null!;
    /// <summary>Source Serif 4, an OTF file with CFF outlines.</summary>
    public ScalableFont Serif { get; private set; } = null!;
    /// <summary>Kenney's players: a Starling XML atlas, anchored at the feet.</summary>
    public SpriteAtlas Players { get; private set; } = null!;
    /// <summary>Kenney's enemies in Aseprite's JSON format: each tag is an animation with its frame durations.</summary>
    public SpriteAtlas Enemies { get; private set; } = null!;
    /// <summary>The loose images of <c>Content/items</c>, packed into one atlas at load.</summary>
    public SpriteAtlas Items { get; private set; } = null!;
    /// <summary>The sky backdrop, a PNG.</summary>
    public Texture Sky { get; private set; } = null!;
    /// <summary>Hills drawn over the sky, a PNG with transparency.</summary>
    public Texture Hills { get; private set; } = null!;
    /// <summary>The second backdrop, a JPEG.</summary>
    public Texture Dusk { get; private set; } = null!;

    /// <summary>
    /// Whether the loop runs in real time (an interactive run) rather than one fixed step per frame (deterministic time: a
    /// scripted or headless run, whose frames must not depend on how long anything took).
    /// </summary>
    public bool RealTime => TimeSettings.Mode == MicroEngine.Kernel.TimeMode.RealTime;

    /// <summary>
    /// In a fixed-step run, a scope holding the level's tile atlas, loaded before play; the gallery's level scope takes the
    /// atlas over (and this scope goes) the first time it loads. Null in interactive runs.
    /// </summary>
    public AssetCache? LevelAhead { get; set; }

    /// <summary>The game's own simulation state for the per-step state hash: whether the level atlas loaded ahead is still held.</summary>
    public override void HashState(StateHasher hash) => hash.Add(LevelAhead is not null);

    /// <inheritdoc/>
    protected override void Load()
    {
        // Everything the screen draws is loaded here: a load during play would show as a hitch (and be reported).
        Sky = Assets.Texture("backgrounds/set1_background.png");
        Hills = Assets.Texture("backgrounds/set1_hills.png");
        Dusk = Assets.Texture("backgrounds/set2_background.jpg");
        var feet = new AtlasOptions(Pivot: new Vector2(0.5f, 1f));
        Players = Assets.Atlas("atlases/players.xml", feet);
        Enemies = Assets.Atlas("atlases/enemies.json", feet);
        Items = Assets.AtlasFromFolder("items", pivot: new Vector2(0.5f, 0.5f));
        Ui = Assets.Font("fonts/Inter-Regular.ttf");
        Bold = Assets.Font("fonts/Inter-Bold.otf");
        Serif = Assets.Font("fonts/SourceSerif4-Regular.otf");
        // The text the gallery shows, rasterized now so the first frames do no font work.
        Ui.Prepare(ScalableFont.Ascii, GalleryScene.Small, GalleryScene.Body, GalleryScene.Label);
        Bold.Prepare(ScalableFont.Ascii, GalleryScene.Title);
        Serif.Prepare(ScalableFont.Ascii, GalleryScene.Prose);
        // A fixed-step run loads the level's atlas now: waiting for it during play would be a hitch, and leaving it to the
        // workers would make the frames depend on how long they take.
        if (!RealTime)
        {
            LevelAhead = Assets.CreateScope("level ahead");
            LevelAhead.Preload("atlases/tiles.xml").Wait();
        }
        Scenes.Push(new GalleryScene(this));
    }
}
