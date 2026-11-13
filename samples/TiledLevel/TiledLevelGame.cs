using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Hosting;
using MicroEngine.Tiled;

namespace TiledLevel;

/// <summary>
/// An HD platformer whose level is a map made in the Tiled editor: the map is data, the rules (what collides, what is
/// the hero, what is collected) are this game's.
/// </summary>
public sealed class TiledLevelGame : Game
{
    /// <summary>The game's composition, called by every build (desktop Program.cs, browser, Android, iOS).</summary>
    public static void Configure(HostBuilder host) => host.UseGame(() => new TiledLevelGame());

    private readonly string? _contentRoot;

    /// <summary>Creates the game; tests pass the content folder of the sources.</summary>
    public TiledLevelGame(string? contentRoot = null) => _contentRoot = contentRoot;

    /// <inheritdoc/>
    public override GameSettings Settings { get; } = new("Tiled Level", 1280, 720, 1)
    {
        Presentation = Presentation.HighResolution2D,
        Background = Color.Hex("27b99a"),
    };

    /// <inheritdoc/>
    protected override string? ContentRoot => _contentRoot;

    /// <summary>The level as Tiled saved it.</summary>
    public TiledMap Map { get; private set; } = null!;
    /// <summary>The map's tile images, as sprites.</summary>
    public TiledSprites Sprites { get; private set; } = null!;

    /// <inheritdoc/>
    protected override void Load()
    {
        #region load
        // The map, its tileset and its templates come through the game's assets, like any content file; the tileset's
        // images become textures here, so nothing loads during play.
        Map = Assets.TiledMap("map/sandbox.tmx");
        Sprites = new TiledSprites(Map, Assets);
        #endregion
        Scenes.Push(new LevelScene(this));
    }
}
