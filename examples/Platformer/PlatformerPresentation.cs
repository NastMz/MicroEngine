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
    public PlatformerPresentation(string assetDirectory, Func<byte[], PixelImage> decode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetDirectory); ArgumentNullException.ThrowIfNull(decode);
        var sources = new Dictionary<string, PixelImage>(StringComparer.Ordinal);
        foreach (var key in new[] { "background_color_hills", "spritesheet-characters-default", "terrain_grass_block_top", "terrain_grass_block_center", "coin_gold", "flag_green_a", "spikes" })
            sources.Add(key, decode(File.ReadAllBytes(Path.Combine(assetDirectory, key + ".png"))));
        using var reader = XmlReader.Create(Path.Combine(assetDirectory, "spritesheet-characters-default.xml"),
            new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 65536 });
        _frames = XDocument.Load(reader).Root!.Elements("SubTexture").ToDictionary(
            node => (string)node.Attribute("name")!, node => new Rect(Number(node, "x"), Number(node, "y"), Number(node, "width"), Number(node, "height")), StringComparer.Ordinal);
        _walk = new([_frames["character_beige_walk_a"], _frames["character_beige_walk_b"]], TimeSpan.FromMilliseconds(120));
        _images = new(sources);
    }
    private static int Number(XElement element, string name) => int.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
    public void Advance(TimeSpan elapsed, LevelGame game)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (_run != game.RunNumber) { _walk.Reset(); _run = game.RunNumber; }
        if (game.Walking && game.Grounded && !game.Completed && !game.Dead) _walk.Advance(elapsed); else _walk.Reset();
    }
    public void Draw(ICanvas canvas, LevelGame game)
    {
        ArgumentNullException.ThrowIfNull(canvas); ArgumentNullException.ThrowIfNull(game);
        if (canvas is not IImageRegionCanvas images) throw new NotSupportedException("This game requires image-region rendering.");
        canvas.Clear(new(160,210,235));
        var background = _images.Get(images, "background_color_hills");
        for (int i = 0; i < 3; i++) images.DrawImage(background, new(i * 256, 0, 256, 432));
        foreach (var solid in LevelGame.Solids)
        for (float y = solid.Y; y < solid.Y + solid.Height; y += 48)
        for (float x = solid.X; x < solid.X + solid.Width; x += 48)
        {
            float width = Math.Min(48, solid.X + solid.Width - x), height = Math.Min(48, solid.Y + solid.Height - y);
            var tile = _images.Get(images, y == solid.Y ? "terrain_grass_block_top" : "terrain_grass_block_center");
            images.DrawImageRegion(tile, new(0,0,width * 64 / 48,height * 64 / 48), new(x-game.CameraX,y,width,height));
        }
        for (int i = 0; i < 3; i++) if (!game.IsCollected(i)) Whole(images, "coin_gold", LevelGame.Coins[i], game.CameraX);
        Whole(images, "spikes", LevelGame.Hazard, game.CameraX);
        Whole(images, "flag_green_a", LevelGame.Flag, game.CameraX);
        var source = game.Dead ? _frames["character_beige_hit"] : !game.Grounded ? _frames["character_beige_jump"] : game.Walking ? _walk.Current : _frames["character_beige_idle"];
        images.DrawImageRegion(_images.Get(images, "spritesheet-characters-default"), source, new(game.Body.X-game.CameraX-18,game.Body.Y+game.Body.Height-64,64,64));
        if (canvas is ILabelCanvas labels)
        {
            labels.Label($"Coins {game.CollectedCount}/3 - reach the green flag", 16,16,20,new(25,40,55));
            labels.Label(game.Completed ? "Level complete! Space to replay" : game.Dead ? "Spikes! Space to retry" : "A/D or arrows: move | Space: jump | Esc: exit",16,46,18,new(25,40,55));
        }
    }
    private void Whole(IImageCanvas canvas, string key, Rect box, float camera) => canvas.DrawImage(_images.Get(canvas,key),new(box.X-camera,box.Y,box.Width,box.Height));
    public void Dispose() => _images.Dispose();
}
