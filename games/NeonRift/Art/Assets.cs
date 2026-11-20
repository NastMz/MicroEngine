using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using Names = NeonRift.Art.GameContent.Sprites;

namespace NeonRift.Art;

/// <summary>A pose split into a body without the gun arm and the arm itself, so the arm can rotate to any aim.</summary>
internal sealed record ArmRig(Sprite Body, Sprite Arm, Vector2 Shoulder, float Length);

/// <summary>A parallax background built from one image of the stage's pack.</summary>
internal sealed record BackdropLayer(Texture Texture, float Factor, float Y, Color Fill, bool Foreground = false);

/// <summary>
/// Loads every image in <c>Content/</c> once, through the game's content source (<c>Game.Assets</c>): the sprite
/// strips, props and tiles that <c>Content/sprites.sheet</c> describes are packed into one atlas, and the hero's aiming
/// poses are derived from the authored frames. The game's asset store holds it (<see cref="Kind"/>) and disposes it with the game.
/// </summary>
internal sealed class Assets : IDisposable
{
    /// <summary>The asset kind the game's store keeps this bundle under.</summary>
    public const string Kind = "neon rift art";
    public const int HeroFrame = 71;
    /// <summary>Feet pivot of the hero frames (all frames share one 71×67 canvas).</summary>
    public static readonly Vector2 HeroOrigin = new(31, 66);

    private readonly SpriteAtlas _atlas;
    private readonly List<Texture> _textures = [];
    private readonly Dictionary<string, ArmRig> _rigs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BackdropLayer[]> _backdrops = new(StringComparer.Ordinal);

    public Assets(IGraphicsDevice device, AssetCache content)
    {
        // Content/sprites.sheet says how every image is cut: cell sizes, anchors, tile rectangles and the rates strips play at.
        var b = new SpriteAtlasBuilder().AddSheet(content.Sheet(Names.Path));
        // The aiming poses are derived from authored frames.
        AddRig(b, "shoot0", content.Image("hero/shoot.png"));
        var runShoot = content.Image("hero/run-shoot.png");
        for (int i = 0; i < 8; i++) AddRig(b, "run-shoot" + i, runShoot.Crop(i * HeroFrame, 0, HeroFrame, 67));
        _atlas = b.Build(device);
        ResolveRigs();

        _backdrops["city"] =
        [
            Layer(device, content, "stages/city/sky.png", 0f, 0),
            Layer(device, content, "stages/city/far.png", 0.12f, 40),
            Layer(device, content, "stages/city/near.png", 0.3f, 60),
        ];
        _backdrops["grotto"] =
        [
            Layer(device, content, "stages/grotto/back.png", 0f, 15),
            Layer(device, content, "stages/grotto/far.png", 0.15f, 30),
            Layer(device, content, "stages/grotto/middle.png", 0.35f, 30),
        ];
        _backdrops["lab"] =
        [
            Layer(device, content, "stages/lab/back.png", 0.1f, 15),
            Layer(device, content, "stages/lab/middle.png", 0.3f, 15),
            Layer(device, content, "stages/lab/front.png", 1.25f, 15, foreground: true),
        ];
        Title = Upload(device, content.Image("stages/city/far.png"));
    }

    /// <summary>The game's pixel font, shared so actors can label pickups.</summary>
    public BitmapFont? Font { get; set; }

    /// <summary>The skyline used behind the title screen.</summary>
    public Texture Title { get; }

    /// <summary>The packed sprite sheet (exposed so tests can guard its size).</summary>
    public Texture AtlasTexture => _atlas.Texture;
    public Sprite this[string name] => _atlas[name];
    /// <summary>The frames <paramref name="name"/>0, 1, 2... (the atlas collects each once).</summary>
    public IReadOnlyList<Sprite> Frames(string name) => _atlas.Frames(name);
    /// <summary>The strip <paramref name="name"/> playing at the rate <c>Content/sprites.sheet</c> gives it.</summary>
    public AnimationClip Clip(string name) => _atlas.Clip(name);
    public bool Contains(string name) => _atlas.Contains(name);
    public ArmRig Rig(string pose) => _rigs[pose];
    public IReadOnlyList<BackdropLayer> Backdrop(string theme) => _backdrops[theme];

    private Texture Upload(IGraphicsDevice device, PixelImage image)
    {
        var t = device.CreateTexture(image);
        _textures.Add(t);
        return t;
    }

    private BackdropLayer Layer(IGraphicsDevice device, AssetCache content, string file, float factor, float y, bool foreground = false)
    {
        var image = content.Image(file);
        // Images are shorter than the canvas: the strip above them is filled with their top colour.
        var fill = image[0, 0].A > 0 ? image[0, 0] with { A = 255 } : Color.Transparent;
        return new BackdropLayer(Upload(device, image), factor, y, fill, foreground);
    }

    /// <summary>
    /// Splits a gun pose into body and arm. The gun tip is the right-most opaque pixel in the upper
    /// body; walking left along its row, the shoulder is where the silhouette becomes tall (the torso).
    /// </summary>
    private void AddRig(SpriteAtlasBuilder b, string pose, PixelImage frame)
    {
        int tipX = -1, tipY = 0;
        for (int y = 18; y < 40; y++)
        for (int x = frame.Width - 1; x > tipX; x--)
            if (frame[x, y].A > 0) { tipX = x; tipY = y; break; }
        int shoulder = tipX;
        for (int x = tipX; x > 20; x--)
        {
            int run = 0;
            for (int y = tipY - 10; y <= tipY + 10; y++) if (frame[x, y].A > 0) run++;
            if (run > 9) { shoulder = x + 1; break; }
        }
        int top = tipY - 4, height = 9;
        var arm = frame.Crop(shoulder, top, tipX - shoulder + 1, height);
        var body = frame.Clone().ClearRect(shoulder, top, tipX - shoulder + 1, height);
        // The gun row sits one pixel below the arm's top edge in these frames; pivot on the arm's middle.
        var pivot = new Vector2(0, 4);
        b.Add("rigbody_" + pose, body, HeroOrigin);
        b.Add("rigarm_" + pose, arm, pivot);
        _pendingRigs.Add((pose, new Vector2(shoulder, top + 4) - HeroOrigin, tipX - shoulder));
    }

    private readonly List<(string Pose, Vector2 Shoulder, float Length)> _pendingRigs = [];

    /// <summary>Resolves rig sprites once the atlas exists.</summary>
    private void ResolveRigs()
    {
        foreach (var (pose, shoulder, length) in _pendingRigs)
            _rigs[pose] = new ArmRig(_atlas["rigbody_" + pose], _atlas["rigarm_" + pose], shoulder, length);
        _pendingRigs.Clear();
    }

    public void Dispose()
    {
        _atlas.Dispose();
        foreach (var t in _textures) t.Dispose();
    }
}
