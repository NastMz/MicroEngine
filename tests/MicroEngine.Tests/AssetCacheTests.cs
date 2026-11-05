using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.Testing;

namespace MicroEngine.Tests;

/// <summary><c>Game.Assets</c>: one object per asset, scopes that release what they loaded, clear failures, diagnostics and preloading.</summary>
[TestClass]
public sealed class AssetCacheTests
{
    private static readonly string Gallery = Path.Combine(AppContext.BaseDirectory, "Gallery");

    private static (AssetCache Assets, HeadlessGraphicsDevice Device) Open()
    {
        var device = new HeadlessGraphicsDevice();
        return (new AssetCache(device, Gallery), device);
    }

    [TestMethod]
    public void The_same_request_returns_the_same_object_and_uploads_once()
    {
        var (assets, device) = Open();
        var a = assets.Texture("backgrounds/set1_background.png");
        var b = assets.Texture("./backgrounds/set1_background.png");
        Assert.AreSame(a, b);
        Assert.AreSame(a, assets.Texture("items/../backgrounds/./set1_background.png"), "one entry per file, however it is spelled");
        Assert.AreEqual(1, device.TexturesCreated);
        Assert.AreEqual(640, a.Width);
        var smooth = assets.Texture("backgrounds/set1_background.png", TextureFilter.Linear);
        Assert.AreNotSame(a, smooth, "another filter is another texture");
        Assert.AreEqual(TextureFilter.Linear, smooth.Filter);
        Assert.AreEqual(480, assets.Texture("backgrounds/set2_background.jpg").Height, "JPEG");
        Assert.AreSame(assets.Atlas("atlases/players.xml"), assets.Atlas("atlases/players.xml"));
        Assert.AreSame(assets.Font("fonts/Inter-Regular.ttf"), assets.Font("fonts/Inter-Regular.ttf"));
        Assert.AreEqual(5, assets.Statistics.Count);
        assets.Dispose();
        Assert.AreEqual(0, device.LiveTextures, "disposing the cache releases every texture it made");
    }

    [TestMethod]
    public void Looking_up_a_loaded_asset_allocates_nothing()
    {
        var (assets, _) = Open();
        assets.Texture("items/blueGem.png");
        assets.Atlas("atlases/players.xml");
        assets.Font("fonts/Inter-Regular.ttf");
        var scope = assets.CreateScope();
        AllocationAssert.None(() => Lookups(scope, assets), warmup: 100, calls: 1000, because: "games may ask for assets in Draw, every frame");

        static void Lookups(AssetCache scope, AssetCache root)
        {
            scope.Texture("items/blueGem.png");
            scope.Atlas("atlases/players.xml");
            scope.Font("fonts/Inter-Regular.ttf");
            _ = scope.Count + root.Count;
        }
    }

    [TestMethod]
    public void Images_are_decoded_fresh_for_editing_and_never_cached()
    {
        var (assets, device) = Open();
        var image = assets.Image("items/blueGem.png");
        image[0, 0] = Color.White;
        Assert.AreNotEqual(Color.White, assets.Image("items/blueGem.png")[0, 0]);
        Assert.AreEqual(0, device.TexturesCreated);
        Assert.AreEqual(0, assets.Statistics.Count);
    }

    [TestMethod]
    public void A_folder_of_images_becomes_one_atlas_named_by_file()
    {
        var (assets, device) = Open();
        var items = assets.AtlasFromFolder("items", pivot: new Vector2(0.5f, 0.5f));
        Assert.IsTrue(items.Contains("blueGem") && items.Contains("keyRed"));
        Assert.AreEqual(Directory.GetFiles(Path.Combine(Gallery, "items"), "*.png").Length, items.Names.Count);
        Assert.AreEqual(new Vector2(items["blueGem"].Width / 2f, items["blueGem"].Height / 2f), items["blueGem"].Origin);
        Assert.AreEqual(1, device.TexturesCreated, "one texture for the whole folder");
        Assert.AreSame(items, assets.AtlasFromFolder("items", pivot: new Vector2(0.5f, 0.5f)));
    }

    [TestMethod]
    public void A_scope_reuses_its_parents_assets_and_releases_only_its_own()
    {
        var (assets, device) = Open();
        var shared = assets.Atlas("atlases/players.xml");
        var level = assets.CreateScope("level 1");
        Assert.AreSame(shared, level.Atlas("atlases/players.xml"), "found in the parent, not loaded again");
        var tiles = level.Atlas("atlases/tiles.xml");
        Assert.IsTrue(level.IsLoaded("atlases/tiles.xml"));
        Assert.IsFalse(assets.IsLoaded("atlases/tiles.xml"), "the parent does not see its children's assets");
        Assert.AreEqual(2, assets.Statistics.Count, "statistics include children");
        Assert.AreEqual(2, assets.Count, "the shared atlas counts once");
        Assert.AreEqual(2, level.Count, "the level holds the atlas it asked for and its own tiles");
        level.Dispose();
        Assert.IsTrue(tiles.Texture.IsDisposed);
        Assert.IsFalse(shared.Texture.IsDisposed);
        Assert.AreEqual(1, device.LiveTextures);
        Assert.AreEqual(1, assets.Statistics.Count);
        Assert.ThrowsExactly<ObjectDisposedException>(() => level.Texture("items/blueGem.png"));
    }

    [TestMethod]
    public void Sibling_scopes_share_one_object_and_it_lives_until_the_last_releases_it()
    {
        var (assets, device) = Open();
        var level1 = assets.CreateScope("level 1");
        var level2 = assets.CreateScope("level 2");
        var a = level1.Texture("items/blueGem.png");
        var b = level2.Texture("items/blueGem.png");
        Assert.AreSame(a, b, "a second scope gets the object the first loaded");
        Assert.AreEqual(1, device.TexturesCreated, "decoded and uploaded once");
        Assert.AreEqual(1, assets.Count, "one shared entry");
        Assert.AreEqual(1, assets.Statistics.Count);
        Assert.AreEqual(a.SizeInBytes, assets.Statistics.TextureBytes, "counted once");
        Assert.AreEqual(1, assets.Loaded.Count);
        Assert.AreEqual(1, level1.Count);
        Assert.AreEqual(1, level2.Count);

        Assert.IsTrue(level1.Unload("items/blueGem.png"), "level 1 let go of it");
        Assert.IsFalse(a.IsDisposed, "level 2 still holds it");
        Assert.AreEqual(0, level1.Count);
        Assert.IsFalse(level1.IsLoaded("items/blueGem.png"));
        Assert.AreEqual(1, assets.Count);
        Assert.AreSame(a, level1.Texture("items/blueGem.png"), "asking again holds the same object again");
        level1.Dispose();
        Assert.IsFalse(a.IsDisposed);
        level2.Dispose();
        Assert.IsTrue(a.IsDisposed, "released with its last holder");
        Assert.AreEqual(0, device.LiveTextures);
        Assert.AreEqual(0, assets.Count);
        Assert.AreEqual(1, device.TexturesCreated);
    }

    [TestMethod]
    public void Unloading_both_holders_disposes_the_asset_once()
    {
        var (assets, device) = Open();
        var level1 = assets.CreateScope();
        var level2 = assets.CreateScope();
        var atlas = level1.Atlas("atlases/players.xml");
        Assert.AreSame(atlas, level2.Atlas("atlases/players.xml"));
        var font = level1.Font("fonts/Inter-Regular.ttf");
        Assert.AreSame(font, level2.Font("fonts/Inter-Regular.ttf"));
        Assert.IsTrue(level2.Unload("atlases/players.xml"));
        Assert.IsFalse(atlas.Texture.IsDisposed);
        Assert.IsTrue(level1.Unload("atlases/players.xml"));
        Assert.IsTrue(atlas.Texture.IsDisposed);
        Assert.AreEqual(0, device.LiveTextures, "one texture made, one released: never disposed twice");
        Assert.IsFalse(level1.Unload("atlases/players.xml"), "nothing left to release");
        Assert.AreEqual(1, assets.Count, "the font is still shared");
        level1.Dispose();
        level2.Dispose();
        Assert.AreEqual(0, assets.Count);
    }

    [TestMethod]
    public void An_asset_a_child_also_holds_survives_its_parents_unload()
    {
        var (assets, _) = Open();
        var held = assets.Texture("items/redGem.png");
        var level = assets.CreateScope();
        Assert.AreSame(held, level.Texture("items/redGem.png"));
        Assert.IsTrue(assets.Unload("items/redGem.png"));
        Assert.IsFalse(held.IsDisposed, "the level still uses it");
        Assert.IsFalse(assets.IsLoaded("items/redGem.png"), "the root let go of it");
        Assert.IsTrue(level.IsLoaded("items/redGem.png"));
        level.Dispose();
        Assert.IsTrue(held.IsDisposed);
    }

    [TestMethod]
    public void A_preload_in_a_scope_finds_what_a_sibling_loaded()
    {
        var (assets, device) = Open();
        var level1 = assets.CreateScope();
        var level2 = assets.CreateScope();
        var gem = level1.Texture("items/greenGem.png");
        var batch = level2.Preload("items/greenGem.png");
        Assert.IsTrue(batch.IsDone, "already loaded by a sibling: done at once");
        Assert.AreSame(gem, level2.Texture("items/greenGem.png"));
        Assert.AreEqual(1, device.TexturesCreated);
        level1.Dispose();
        Assert.IsFalse(gem.IsDisposed, "the preloading scope holds it");
        level2.Dispose();
        Assert.IsTrue(gem.IsDisposed);
    }

    [TestMethod]
    public void Unload_releases_one_asset_and_a_later_request_loads_it_again()
    {
        var (assets, device) = Open();
        var first = assets.Texture("items/redGem.png");
        Assert.IsTrue(assets.Unload("items/redGem.png"));
        Assert.IsTrue(first.IsDisposed);
        Assert.IsFalse(assets.IsLoaded("items/redGem.png"));
        Assert.AreNotSame(first, assets.Texture("items/redGem.png"));
        Assert.IsFalse(assets.Unload("items/none.png"));
        Assert.AreEqual(1, device.LiveTextures);
    }

    [TestMethod]
    public void Failures_name_the_asset_and_say_what_is_wrong()
    {
        var (assets, _) = Open();
        var missing = Assert.ThrowsExactly<AssetLoadException>(() => assets.Texture("items/none.png"));
        Assert.AreEqual("items/none.png", missing.AssetPath);
        Assert.AreEqual("texture", missing.Kind);
        StringAssert.Contains(missing.Message, "not found");
        var wrongCase = Assert.ThrowsExactly<AssetLoadException>(() => assets.Texture("items/BlueGem.png"));
        StringAssert.Contains(wrongCase.Message, "blueGem.png", "the message gives the real spelling");
        StringAssert.Contains(Assert.ThrowsExactly<AssetLoadException>(() => assets.Texture("Items/blueGem.png")).Message, "items", "folders too");
        StringAssert.Contains(Assert.ThrowsExactly<AssetLoadException>(() => assets.Texture("atlases/players.xml")).Message, "PNG, JPEG");
        StringAssert.Contains(Assert.ThrowsExactly<AssetLoadException>(() => assets.Font("items/blueGem.png")).Message, "font");
        Assert.ThrowsExactly<AssetLoadException>(() => assets.Texture("../escape.png"));
        Assert.ThrowsExactly<AssetLoadException>(() => assets.AtlasFromFolder("fonts"));
        var noFolder = new AssetCache(new HeadlessGraphicsDevice(), Path.Combine(Gallery, "no-such-folder"));
        StringAssert.Contains(Assert.ThrowsExactly<AssetLoadException>(() => noFolder.Texture("a.png")).Message, "Content folder not found");
        Assert.AreEqual(0, assets.Statistics.Count, "nothing half-loaded stays");
    }

    [TestMethod]
    public void Loads_during_play_are_counted_and_slow_ones_reported_once()
    {
        var (assets, _) = Open();
        assets.Texture("items/blueGem.png");
        assets.Playing = true;
        assets.SlowLoadMilliseconds = 0.000001;
        var error = Console.Error;
        var captured = new StringWriter();
        Console.SetError(captured);
        try
        {
            assets.Texture("backgrounds/set1_hills.png");
            assets.Texture("backgrounds/set1_hills.png");
        }
        finally { Console.SetError(error); }
        Assert.AreEqual(1, assets.Statistics.LoadsDuringPlay);
        Assert.IsFalse(assets.Loaded.Single(a => a.Path == "items/blueGem.png").DuringPlay);
        Assert.IsTrue(assets.Loaded.Single(a => a.Path == "backgrounds/set1_hills.png").DuringPlay);
        var report = captured.ToString();
        StringAssert.Contains(report, "backgrounds/set1_hills.png");
        Assert.AreEqual(1, report.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length, "reported once");
        Assert.AreEqual(640L * 480 * 4 + assets.Texture("items/blueGem.png").SizeInBytes, assets.Statistics.TextureBytes);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Preloading_finishes_within_frames_and_later_requests_find_the_assets(bool background)
    {
        var (assets, device) = Open();
        assets.BackgroundDecoding = background;
        assets.FrameBudgetMilliseconds = 0; // one asset per frame
        var batch = assets.Preload("backgrounds/set1_background.png", "atlases/enemies.json", "fonts/SourceSerif4-Regular.otf", "items/none.png");
        Assert.AreEqual(4, batch.Count);
        Assert.AreEqual(0, device.TexturesCreated, "nothing reaches the device before the engine's update");
        // Workers may start late on a busy two-core runner: wait by time, one asset per Update.
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!batch.IsDone && DateTime.UtcNow < deadline)
        {
            assets.Update();
            if (!batch.IsDone) Thread.Sleep(1);
        }
        Assert.IsTrue(batch.IsDone);
        Assert.AreEqual(1f, batch.Progress);
        Assert.AreEqual(1, batch.Errors.Count);
        Assert.AreEqual("items/none.png", batch.Errors[0].AssetPath);
        int created = device.TexturesCreated;
        var background_ = assets.Texture("backgrounds/set1_background.png");
        var enemies = assets.Atlas("atlases/enemies.json");
        var serif = assets.Font("fonts/SourceSerif4-Regular.otf");
        Assert.AreEqual(created, device.TexturesCreated, "the preloaded objects are returned, nothing new is made");
        Assert.AreEqual(12, enemies.Animations.Count);
        Assert.IsTrue(serif.Measure("x", 20).X > 0 && background_.Width == 640);
        Assert.AreEqual(1, assets.Preload("backgrounds/set1_background.png").Completed, "already loaded: done at once");
    }

    [TestMethod]
    public void Waiting_for_a_batch_finishes_it_and_reports_failures()
    {
        var (assets, _) = Open();
        var ok = assets.Preload("items/greenGem.png", "atlases/players.xml");
        ok.Wait();
        Assert.IsTrue(ok.IsDone);
        Assert.IsTrue(assets.IsLoaded("atlases/players.xml"));
        var bad = assets.Preload("items/missing.png");
        var error = Assert.ThrowsExactly<AggregateException>(bad.Wait);
        Assert.IsInstanceOfType<AssetLoadException>(error.InnerExceptions[0]);
        Assert.ThrowsExactly<ArgumentException>(() => assets.Preload("music/theme.ogg"));
    }

    private sealed class GalleryGame : Game
    {
        public override GameSettings Settings { get; } = new("gallery", 1280, 720) { Presentation = Presentation.HighResolution2D };
        protected override string? ContentRoot => Gallery;
        public SpriteAtlas? Players;
        protected override void Load()
        {
            Players = Assets.Atlas("atlases/players.xml");
            Scenes.Push(new Play());
        }
        private sealed class Play : Scene
        {
            public override void Update(float dt) => Assets.Texture("items/yellowGem.png");
            public override void Draw(IRenderer r) { }
        }
    }

    [TestMethod]
    public void A_game_owns_its_cache_and_releases_it_at_shutdown()
    {
        var game = new GalleryGame();
        var platform = HeadlessPlatform.For(game.Settings);
        var result = GameRunner.Run(game, platform, new RunOptions { Script = new InputScript().QuitAfter(3) });
        Assert.IsNull(result.Error, result.Error?.ToString());
        var device = (HeadlessGraphicsDevice)platform.Graphics;
        Assert.AreEqual(TextureFilter.Linear, game.Players!.Texture.Filter, "the smooth game's default filter");
        Assert.IsTrue(game.Players.Texture.IsDisposed, "released after Unload");
        Assert.AreEqual(0, device.LiveTextures, "every texture, the built-in font's included, is released at shutdown");
    }
}
