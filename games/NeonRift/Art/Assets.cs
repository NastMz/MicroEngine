using System.Numerics;
using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace NeonRift.Art;

/// <summary>A pose split into a body without the gun arm and the arm itself, so the arm can rotate to any aim.</summary>
internal sealed record ArmRig(Sprite Body, Sprite Arm, Vector2 Shoulder, float Length);

/// <summary>A parallax background built from one image of the stage's pack.</summary>
internal sealed record BackdropLayer(Texture Texture, float Factor, float Y, Color Fill, bool Foreground = false);

/// <summary>
/// Loads every image in <c>Content/</c> once: sprite strips are sliced into one atlas, tiles are cut
/// from the stage tilesets, and the hero's aiming poses are derived from the authored frames.
/// </summary>
internal sealed class Assets : IDisposable
{
    public const int HeroFrame = 71;
    /// <summary>Feet pivot of the hero frames (all frames share one 71×67 canvas).</summary>
    public static readonly Vector2 HeroOrigin = new(31, 66);

    private readonly SpriteAtlas _atlas;
    private readonly List<Texture> _textures = [];
    private readonly Dictionary<string, ArmRig> _rigs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BackdropLayer[]> _backdrops = new(StringComparer.Ordinal);

    public Assets(IGraphicsDevice device, ContentFolder content)
    {
        var b = new SpriteAtlasBuilder();
        // Hero (Warped City): every strip shares the 71×67 canvas and the feet pivot.
        foreach (var name in new[] { "idle", "run", "run-shoot", "jump", "back-jump", "crouch", "shoot", "hurt" })
            b.AddGrid("hero_" + name, content.Image($"hero/{name}.png"), 71, 67, origin: HeroOrigin);
        var shoot = content.Image("hero/shoot.png");
        AddRig(b, "shoot0", shoot);
        var runShoot = content.Image("hero/run-shoot.png");
        for (int i = 0; i < 8; i++) AddRig(b, "run-shoot" + i, runShoot.Crop(i * 71, 0, 71, 67));

        Strip(b, content, "drone", "enemies/drone.png", 55, 52, new Vector2(27, 26));
        Strip(b, content, "turret", "enemies/turret.png", 25, 23, new Vector2(12, 22));
        Strip(b, content, "trooper_idle", "enemies/trooper-idle.png", 61, 64, new Vector2(30, 63));
        Strip(b, content, "trooper_run", "enemies/trooper-run.png", 61, 64, new Vector2(30, 63));
        Strip(b, content, "egg_idle", "enemies/egg-idle.png", 44, 62, new Vector2(22, 61));
        Strip(b, content, "egg_shoot", "enemies/egg-shoot.png", 44, 62, new Vector2(22, 61));
        Strip(b, content, "skiff", "enemies/skiff.png", 29, 29, new Vector2(14, 14));
        Strip(b, content, "enemy_bullet", "fx/enemy-bullet.png", 12, 12, new Vector2(6, 6));
        Strip(b, content, "explosion", "fx/explosion.png", 48, 48, new Vector2(24, 24));
        Strip(b, content, "explosion_big", "fx/explosion-big.png", 55, 52, new Vector2(27, 26));
        Strip(b, content, "shot", "fx/shot.png", 15, 11, new Vector2(7, 5));
        Strip(b, content, "shot_hit", "fx/shot-hit.png", 15, 11, new Vector2(7, 5));
        Strip(b, content, "bolt", "fx/bolt.png", 48, 32, new Vector2(24, 16));
        Strip(b, content, "pulse", "fx/pulse.png", 63, 32, new Vector2(31, 16));
        Strip(b, content, "charged", "fx/charged.png", 63, 48, new Vector2(31, 24));
        Strip(b, content, "crossed", "fx/crossed.png", 32, 32, new Vector2(16, 16));
        Strip(b, content, "waveform", "fx/waveform.png", 95, 32, new Vector2(47, 16));
        for (int i = 1; i <= 5; i++) Strip(b, content, "hit" + i, $"fx/hit{i}.png", 32, 32, new Vector2(16, 16));
        Strip(b, content, "gunship", "bosses/gunship.png", 176, 96, new Vector2(88, 48));
        Strip(b, content, "gunship_thrust", "bosses/gunship-thrust.png", 48, 48, new Vector2(24, 24));
        Strip(b, content, "interceptor", "bosses/interceptor.png", 176, 125, new Vector2(88, 62));
        Strip(b, content, "interceptor_thrust", "bosses/interceptor-thrust.png", 81, 58, new Vector2(40, 29));
        Strip(b, content, "dreadnought", "bosses/dreadnought.png", 329, 160, new Vector2(164, 80));
        Strip(b, content, "dread_thrust_back", "bosses/dreadnought-thrust-back.png", 32, 47, new Vector2(16, 23));
        foreach (var prop in new[] { "palm", "plant", "plant-big" })
        {
            var img = content.Image($"stages/grotto/{prop}.png");
            b.Add("prop_" + prop, img, new Vector2(img.Width / 2f, img.Height));
        }
        foreach (var prop in new[] { "tank-1", "tank-2", "tank-3", "support" })
        {
            var img = content.Image($"stages/lab/{prop}.png");
            b.Add("prop_" + prop, img, new Vector2(img.Width / 2f, img.Height));
        }
        Tiles.AddAll(b, content);
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
    public Sprite[] Frames(string name) => _atlas.Frames(name);
    public bool Contains(string name) => _atlas.Contains(name);
    public ArmRig Rig(string pose) => _rigs[pose];
    public IReadOnlyList<BackdropLayer> Backdrop(string theme) => _backdrops[theme];

    private static void Strip(SpriteAtlasBuilder b, ContentFolder content, string name, string file, int w, int h, Vector2 origin)
        => b.AddGrid(name, content.Image(file), w, h, origin: origin);

    private Texture Upload(IGraphicsDevice device, PixelImage image)
    {
        var t = device.CreateTexture(image);
        _textures.Add(t);
        return t;
    }

    private BackdropLayer Layer(IGraphicsDevice device, ContentFolder content, string file, float factor, float y, bool foreground = false)
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
