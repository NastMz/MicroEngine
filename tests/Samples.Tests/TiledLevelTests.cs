using MicroEngine.Core;
using MicroEngine.Tiled;
using TiledLevel;

namespace Samples.Tests;

/// <summary>The Tiled sample: Tiled's own Sticker Knight map, loaded and played headlessly.</summary>
[TestClass]
public sealed class TiledLevelTests
{
    private static (TiledLevelGame Game, LevelScene Level) Start()
    {
        var game = new TiledLevelGame();
        game.InitializeForTest(HeadlessPlatform.For(game.Settings));
        return (game, (LevelScene)game.Scenes.Current!);
    }

    private static void Steps(TiledLevelGame game, int count, string? holding = null)
    {
        if (holding is not null) game.Input.SetForced(holding, true);
        for (int i = 0; i < count; i++) game.StepForTest();
        if (holding is not null) game.Input.SetForced(holding, false);
    }

    [TestMethod]
    public void The_map_saved_by_Tiled_loads_with_its_tileset_templates_and_objects()
    {
        var (game, level) = Start();
        var map = game.Map;
        Assert.AreEqual((79, 45, 32, 32), (map.Width, map.Height, map.TileWidth, map.TileHeight));
        Assert.HasCount(11, map.ObjectLayers.ToList());
        Assert.HasCount(62, map.Tilesets.Single().Tiles, "the collection's images, whatever gaps its ids have");
        Assert.AreEqual(20, level.Solids.Count, "18 static objects and the 2 push blocks of the block template (dynamic, solid in this sample)");
        Assert.AreEqual(6, level.CoinsTotal, "the diamond template's instances");
        Assert.AreEqual(new MicroEngine.Color(0x27, 0xb9, 0x9a), map.Background);
        Assert.IsNotNull(game.Sprites[map.Objects.First(o => o.Class == "hero").Gid], "the hero template's tile has a sprite");
    }

    [TestMethod]
    public void The_hero_lands_runs_collects_and_jumps()
    {
        // The tour script's route: run right from the start, jump at update 30, land on the castle's first platform.
        var (game, level) = Start();
        Steps(game, 10);
        Assert.IsTrue(level.OnGround, "the hero lands on the ground objects");
        Assert.AreEqual(991, level.Hero.Bottom, "the top of the ground tiles: 1087 less their 96-pixel height");
        float x = level.Hero.X;
        Steps(game, 20, "right");
        Assert.IsGreaterThan(x + 100, level.Hero.X, "running right");
        Assert.IsLessThan(6, level.CoinsLeft, "the diamonds on the way are collected");
        game.Input.SetForced("right", true);
        game.Input.SetForced("confirm", true);
        game.StepForTest();
        game.Input.SetForced("confirm", false);
        Steps(game, 90, "right");
        Assert.IsTrue(level.OnGround);
        Assert.AreEqual(735, level.Hero.Bottom, "on the platform object at y 831, 96 pixels tall");
    }
}
