using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using NeonRift.Gameplay;
using Names = NeonRift.Art.GameContent.Sprites;
using Files = NeonRift.Art.GameContent.Stages;

namespace NeonRift.Art;

/// <summary>A pose split into a body without the gun arm (it keeps the pose's points) and the arm itself, so the arm can rotate to any aim.</summary>
internal sealed record ArmRig(Sprite Body, Sprite Arm);

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

    private readonly SpriteAtlas _atlas;
    private readonly List<Texture> _textures = [];
    private readonly Dictionary<string, ArmRig> _rigs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BackdropLayer[]> _backdrops = new(StringComparer.Ordinal);

    public Assets(IGraphicsDevice device, AssetCache content)
    {
        // Content/sprites.sheet says how every image is cut: cell sizes, anchors, tile rectangles and the rates strips play at.
        var sheet = content.Sheet(Names.Path);
        var b = new SpriteAtlasBuilder().AddSheet(sheet);
        // The aiming poses are derived from the frames the sheet marks a shoulder on: the standing shot and every frame of the run.
        var poses = sheet.Frames.Where(static f => f.Points.Any(static p => p.Name == "shoulder")).ToList();
        foreach (var pose in poses) AddRig(b, pose);
        _atlas = b.Build(device);
        foreach (var pose in poses) _rigs[pose.Name] = new ArmRig(_atlas["rigbody_" + pose.Name], _atlas["rigarm_" + pose.Name]);

        _backdrops["city"] =
        [
            Layer(device, content, Files.City.Sky, 0f, 0),
            Layer(device, content, Files.City.Far, 0.12f, 40),
            Layer(device, content, Files.City.Near, 0.3f, 60),
        ];
        _backdrops["grotto"] =
        [
            Layer(device, content, Files.Grotto.Back, 0f, 15),
            Layer(device, content, Files.Grotto.Far, 0.15f, 30),
            Layer(device, content, Files.Grotto.Middle, 0.35f, 30),
        ];
        _backdrops["lab"] =
        [
            Layer(device, content, Files.Lab.Back, 0.1f, 15),
            Layer(device, content, Files.Lab.Middle, 0.3f, 15),
            Layer(device, content, Files.Lab.Front, 1.25f, 15, foreground: true),
        ];
        Title = Upload(device, content.Image(Files.City.Far));
    }

    /// <summary>The game's pixel font, shared so actors can label pickups.</summary>
    public BitmapFont? Font { get; set; }

    /// <summary>The skyline used behind the title screen.</summary>
    public Texture Title { get; }

    /// <summary>The packed sprite sheet (exposed so tests can guard its size).</summary>
    public Texture AtlasTexture => _atlas.Texture;
    /// <summary>The sprites and clips of <c>Content/sprites.sheet</c>, which play reads boxes, points and clip lengths from.</summary>
    public SpriteAtlas Atlas => _atlas;
    public Sprite this[string name] => _atlas[name];
    /// <summary>The frames <paramref name="name"/>0, 1, 2... (the atlas collects each once).</summary>
    public IReadOnlyList<Sprite> Frames(string name) => _atlas.Frames(name);
    /// <summary>The strip <paramref name="name"/> playing at the rate <c>Content/sprites.sheet</c> gives it.</summary>
    public AnimationClip Clip(string name) => _atlas.Clip(name);
    public bool Contains(string name) => _atlas.Contains(name);
    /// <summary>The body and arm of the gun pose drawn in the frame <paramref name="pose"/> of the sheet.</summary>
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
    /// Splits a gun pose into body and arm: the arm is the nine rows around the gun's row, from the shoulder to the gun
    /// tip the sheet marks, and it turns around the shoulder.
    /// </summary>
    private static void AddRig(SpriteAtlasBuilder b, SheetFrame pose)
    {
        var shoulder = pose.Points.First(static p => p.Name == "shoulder").Position;
        var tip = pose.Points.First(static p => p.Name == "tip").Position;
        int left = (int)shoulder.X, top = (int)shoulder.Y - 4, width = (int)tip.X - left + 1, height = 9;
        b.Add(pose with { Name = "rigbody_" + pose.Name, Image = pose.Image.Clone().ClearRect(left, top, width, height) });
        b.Add("rigarm_" + pose.Name, pose.Image.Crop(left, top, width, height), new Vector2(0, 4));
    }

    public void Dispose()
    {
        _atlas.Dispose();
        foreach (var t in _textures) t.Dispose();
    }
}
