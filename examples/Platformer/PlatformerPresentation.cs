using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using MicroEngine.Loop;
using MicroEngine.TwoD;
using Platformer.Model;
namespace Platformer;

/// <summary>Game content mappings and draw composition; engine utilities own upload caching/playback.</summary>
public sealed class PlatformerPresentation : IDisposable
{
    private readonly ImageSet _images;
    private readonly Dictionary<string, Rect> _frames;
    private readonly FrameAnimation _walk;
    private int _run;
    private LevelGame? _level;
    private long _enemyPhase;
    public PlatformerPresentation(string assetDirectory, Func<byte[], PixelImage> decode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetDirectory); ArgumentNullException.ThrowIfNull(decode);
        var sources = new Dictionary<string, PixelImage>(StringComparer.Ordinal);
        foreach (var key in new[] { "background_color_hills", "spritesheet-characters-default", "terrain_grass_block_top", "terrain_grass_block_center", "coin_gold", "flag_green_a", "spikes", "slime_spike_walk_a", "slime_spike_walk_b" })
            sources.Add(key, decode(File.ReadAllBytes(Path.Combine(assetDirectory, key + ".png"))));
        using var reader = XmlReader.Create(Path.Combine(assetDirectory, "spritesheet-characters-default.xml"),
            new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 65536 });
        _frames = XDocument.Load(reader).Root!.Elements("SubTexture").ToDictionary(
            node => (string)node.Attribute("name")!, node => new Rect(Number(node, "x"), Number(node, "y"), Number(node, "width"), Number(node, "height")), StringComparer.Ordinal);
        _walk = new([_frames["character_beige_walk_a"], _frames["character_beige_walk_b"]], TimeSpan.FromMilliseconds(120));
        _images = new(sources);
    }
    private static int Number(XElement element, string name) => int.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
    public void AdvanceSession(TimeSpan elapsed, MeadowSession game)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));
        if (game.AdvancedThisUpdate) AdvanceCampaign(elapsed, game.Campaign);
    }
    public void DrawSession(ICanvas canvas, MeadowSession game)
    {
        ArgumentNullException.ThrowIfNull(canvas); ArgumentNullException.ThrowIfNull(game);
        if (canvas is not ILabelCanvas labels || canvas is not IImageRegionCanvas)
            throw new NotSupportedException("Session screens require image-region and label rendering.");
        DrawCampaign(canvas, game.Campaign);
        if (game.Phase == SessionPhase.Running)
        {
            labels.Label("P: pause | R: restart | Esc: exit",16,72,16,new(25,40,55));
            return;
        }
        canvas.Fill(new(176,120,416,240),new(20,35,50,245));
        var ink=new Rgba(245,245,235);
        if (game.Phase == SessionPhase.Title)
        {
            labels.Label("Meadow Run",208,148,28,ink);
            labels.Label("Space: start",208,212,22,ink);
            labels.Label("P: pause | R: restart | Esc: exit",192,286,16,ink);
        }
        else
        {
            labels.Label("Paused",208,140,28,ink);
            labels.Label(game.Selection == 0 ? "> Resume" : "  Resume",208,198,22,ink);
            labels.Label(game.Selection == 1 ? "> Restart campaign" : "  Restart campaign",208,236,22,ink);
            labels.Label("Up/Down + Space | P: resume",192,298,16,ink);
        }
    }
    public void AdvanceCampaign(TimeSpan elapsed, CampaignGame game)
    {
        ArgumentNullException.ThrowIfNull(game); Advance(elapsed, game.Current);
    }
    public void DrawCampaign(ICanvas canvas, CampaignGame game)
    {
        ArgumentNullException.ThrowIfNull(game);
        string status = game.Completed ? "Meadow Run complete! Space to replay" : game.Current.Completed ? "Level complete! Space to continue" : game.Current.Dead ? "Defeat! Space to retry this level" : game.Current.EnemyBody is not null ? "Avoid spiked slime | Space: jump | Esc: exit" : "A/D or arrows: move | Space: jump | Esc: exit";
        DrawCore(canvas, game.Current, $"Level {game.LevelNumber}/2 - Coins {game.Current.CollectedCount}/{game.Current.CoinCount} - reach the green flag", status);
    }
    public void Advance(TimeSpan elapsed, LevelGame game)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));
        if (!ReferenceEquals(_level, game) || _run != game.RunNumber) { _walk.Reset(); _run = game.RunNumber; _level = game; _enemyPhase = 0; }
        if (game.Walking && game.Grounded && !game.Completed && !game.Dead) _walk.Advance(elapsed); else _walk.Reset();
        if (game.EnemyBody is not null && !game.Completed && !game.Dead)
            _enemyPhase = (_enemyPhase + elapsed.Ticks % TimeSpan.FromMilliseconds(240).Ticks) % TimeSpan.FromMilliseconds(240).Ticks;
    }
    public void Draw(ICanvas canvas, LevelGame game) => DrawCore(canvas, game, null, null);
    private void DrawCore(ICanvas canvas, LevelGame game, string? header, string? status)
    {
        ArgumentNullException.ThrowIfNull(canvas); ArgumentNullException.ThrowIfNull(game);
        if (canvas is not IImageRegionCanvas images) throw new NotSupportedException("This game requires image-region rendering.");
        canvas.Clear(new(160,210,235));
        var background = _images.Get(images, "background_color_hills");
        for (int i = 0; i < 3; i++) images.DrawImage(background, new(i * 256, 0, 256, 432));
        foreach (var solid in game.Terrain)
        for (float y = solid.Y; y < solid.Y + solid.Height; y += 48)
        for (float x = solid.X; x < solid.X + solid.Width; x += 48)
        {
            float width = Math.Min(48, solid.X + solid.Width - x), height = Math.Min(48, solid.Y + solid.Height - y);
            var tile = _images.Get(images, y == solid.Y ? "terrain_grass_block_top" : "terrain_grass_block_center");
            images.DrawImageRegion(tile, new(0,0,width * 64 / 48,height * 64 / 48), new(x-game.CameraX,y,width,height));
        }
        for (int i = 0; i < game.CoinCount; i++) if (!game.IsCollected(i)) Whole(images, "coin_gold", game.CoinPositions[i], game.CameraX);
        Whole(images, "spikes", game.Danger, game.CameraX);
        Whole(images, "flag_green_a", game.Goal, game.CameraX);
        if (game.EnemyBody is Rect enemy)
            Whole(images, _enemyPhase < TimeSpan.FromMilliseconds(120).Ticks ? "slime_spike_walk_a" : "slime_spike_walk_b", new(enemy.X-12,enemy.Y+enemy.Height-64,64,64), game.CameraX);
        var source = game.Dead ? _frames["character_beige_hit"] : !game.Grounded ? _frames["character_beige_jump"] : game.Walking ? _walk.Current : _frames["character_beige_idle"];
        images.DrawImageRegion(_images.Get(images, "spritesheet-characters-default"), source, new(game.Body.X-game.CameraX-18,game.Body.Y+game.Body.Height-64,64,64));
        if (canvas is ILabelCanvas labels)
        {
            labels.Label(header ?? $"Coins {game.CollectedCount}/{game.CoinCount} - reach the green flag", 16,16,20,new(25,40,55));
            labels.Label(status ?? (game.Completed ? "Level complete! Space to replay" : game.Dead ? "Spikes! Space to retry" : "A/D or arrows: move | Space: jump | Esc: exit"),16,46,18,new(25,40,55));
        }
    }
    private void Whole(IImageCanvas canvas, string key, Rect box, float camera) => canvas.DrawImage(_images.Get(canvas,key),new(box.X-camera,box.Y,box.Width,box.Height));
    public void Dispose() => _images.Dispose();
}
