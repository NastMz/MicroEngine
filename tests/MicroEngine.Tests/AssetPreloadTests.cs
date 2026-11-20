using System.Numerics;
using MicroEngine.Audio;
using MicroEngine.Content;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tests;

/// <summary>
/// <c>Assets.Preload</c> takes every kind of asset: sounds, music, sheets, shaders and the kinds a game registers,
/// with the options the direct calls take, so a loading screen holds no code per kind.
/// </summary>
[TestClass]
public sealed class AssetPreloadTests
{
    private sealed record Level(string Text);

    private static byte[] Wav()
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);
        short[] samples = [0, 1000, -1000, 0];
        w.Write("RIFF"u8); w.Write(36 + samples.Length * 2); w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(22050); w.Write(44100); w.Write((short)2); w.Write((short)16);
        w.Write("data"u8); w.Write(samples.Length * 2);
        foreach (short s in samples) w.Write(s);
        w.Flush();
        return stream.ToArray();
    }

    private static string Content()
    {
        string root = Directory.CreateTempSubdirectory("me-preload-").FullName;
        Directory.CreateDirectory(Path.Combine(root, "art"));
        Directory.CreateDirectory(Path.Combine(root, "audio"));
        Directory.CreateDirectory(Path.Combine(root, "levels"));
        var strip = new PixelImage(16, 8).Clear(Color.White);
        File.WriteAllBytes(Path.Combine(root, "art", "drone.png"), PngEncoder.Encode(strip.ToRgba(), 16, 8));
        File.WriteAllText(Path.Combine(root, "art", "enemies.sheet"), """{ "sheets": [ { "image": "drone.png", "name": "drone", "cell": [8, 8], "fps": 12 } ] }""");
        File.WriteAllBytes(Path.Combine(root, "audio", "jump.wav"), Wav());
        File.WriteAllBytes(Path.Combine(root, "audio", "theme.ogg"), [1, 2, 3, 4]);
        File.WriteAllText(Path.Combine(root, "levels", "one.lvl"), "###");
        File.WriteAllText(Path.Combine(root, "notes.xyz"), "?");
        return root;
    }

    private static void Finish(AssetCache assets, AssetBatch batch)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!batch.IsDone && DateTime.UtcNow < deadline)
        {
            assets.Update();
            if (!batch.IsDone) Thread.Sleep(1);
        }
        Assert.IsTrue(batch.IsDone, "the batch finishes within frames");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Files_of_every_kind_preload_by_path_and_the_usual_calls_find_them(bool background)
    {
        string root = Content();
        try
        {
            var device = new HeadlessGraphicsDevice();
            using var assets = new AssetCache(device, root) { BackgroundDecoding = background, FrameBudgetMilliseconds = 0 };
            assets.Loaders.Add(AssetLoader.Background<Level>("level", c => new Level(System.Text.Encoding.UTF8.GetString(c.ReadBytes())), [".lvl"]));
            var batch = assets.Preload("art/drone.png", "art/enemies.sheet", "audio/jump.wav", "audio/theme.ogg", "levels/one.lvl", "audio/none.wav");
            Assert.AreEqual(6, batch.Count);
            Assert.AreEqual(0, device.TexturesCreated, "nothing reaches the device before the engine's update");
            Assert.AreEqual("art/drone.png", batch.Current, "what a loading screen names while it waits");
            Finish(assets, batch);
            Assert.IsNull(batch.Current);
            Assert.AreEqual("audio/none.wav", batch.Errors.Single().AssetPath);
            int textures = device.TexturesCreated, count = assets.Count;
            Assert.AreEqual(5, count);
            Assert.AreEqual(16, assets.Texture("art/drone.png").Width);
            Assert.AreEqual(12f, assets.Atlas("art/enemies.sheet").Clip("drone").FramesPerSecond, "a sheet description preloads as the atlas it makes");
            Assert.AreEqual(4, assets.Sound("audio/jump.wav").Samples.Length);
            Assert.AreEqual("ogg", assets.Music("audio/theme.ogg").Format);
            Assert.AreEqual("###", assets.Store.Get(assets.Scope.Load<Level>("level", "levels/one.lvl")).Text, "a kind the game registered");
            Assert.AreEqual((textures, count), (device.TexturesCreated, assets.Count), "the preloaded objects are returned, nothing new is made");
            Assert.IsTrue(assets.Statistics.AudioBytes > 0);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [TestMethod]
    public void A_preload_takes_the_options_the_direct_calls_take()
    {
        string root = Content();
        try
        {
            var device = new HeadlessGraphicsDevice();
            using var assets = new AssetCache(device, root);
            var feet = new AtlasOptions(Pivot: new Vector2(0.5f, 1));
            var batch = assets.Preload(load =>
            {
                load.Atlas("art/enemies.sheet", feet, TextureFilter.Linear);
                load.Texture("art/drone.png", TextureFilter.Linear);
                load.Music("audio/jump.wav");
                load.Asset("sound", "audio/jump.wav");
                load.RenderTarget("minimap", 64, 32);
            });
            Assert.AreEqual(5, batch.Count);
            batch.Wait();
            int textures = device.TexturesCreated;
            var atlas = assets.Atlas("art/enemies.sheet", feet, TextureFilter.Linear);
            Assert.AreEqual(new Vector2(4, 8), atlas["drone0"].Origin);
            Assert.AreEqual(TextureFilter.Linear, assets.Texture("art/drone.png", TextureFilter.Linear).Filter);
            Assert.AreEqual("wav", assets.Music("audio/jump.wav").Format, "a WAV asked for as music");
            Assert.AreEqual(64, assets.RenderTarget("minimap", 64, 32).Width);
            assets.Sound("audio/jump.wav");
            Assert.AreEqual(textures, device.TexturesCreated, "the same keys as the direct calls: nothing loads twice");
            Assert.AreEqual(5, assets.Count);
            Assert.AreNotSame(atlas, assets.Atlas("art/enemies.sheet"), "other options are another asset, as always");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [TestMethod]
    public void A_file_no_loader_reads_is_refused_at_once_naming_it()
    {
        string root = Content();
        try
        {
            using var assets = new AssetCache(new HeadlessGraphicsDevice(), root);
            var error = Assert.ThrowsExactly<ArgumentException>(() => assets.Preload("art/drone.png", "notes.xyz"));
            StringAssert.Contains(error.Message, "notes.xyz");
            StringAssert.Contains(error.Message, ".xyz");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [TestMethod]
    public void A_background_loader_works_off_the_games_thread_and_a_plain_one_on_it()
    {
        string root = Content();
        try
        {
            using var assets = new AssetCache(new HeadlessGraphicsDevice(), root) { BackgroundDecoding = true };
            int game = Environment.CurrentManagedThreadId, background = -1, plain = -1, staged = -1, completed = -1;
            assets.Loaders.Add(AssetLoader.Background<Level>("level", c => { background = Environment.CurrentManagedThreadId; return new Level("a"); }, [".lvl"]));
            assets.Loaders.Add(new AssetLoader<Level>("plain level", c => { plain = Environment.CurrentManagedThreadId; return new Level("b"); }));
            assets.Loaders.Add(new StagedAssetLoader<string, Level>("staged level",
                c => { staged = Environment.CurrentManagedThreadId; return "c"; },
                (c, text) => { completed = Environment.CurrentManagedThreadId; return new Level(text); }));
            var batch = assets.Preload(load =>
            {
                load.Asset("level", "levels/one.lvl");
                load.Asset("plain level", "levels/one.lvl");
                load.Asset("staged level", "levels/one.lvl");
            });
            Finish(assets, batch);
            Assert.AreEqual(0, batch.Errors.Count);
            Assert.AreNotEqual(game, background, "a loader that touches only the content runs on a worker");
            Assert.AreEqual(game, plain, "a loader that says nothing runs where every loader runs: the game's thread");
            Assert.AreNotEqual(game, staged);
            Assert.AreEqual(game, completed, "the device part of a staged loader runs on the game's thread");
            Assert.AreEqual("c", assets.Store.Get(assets.Scope.Load<Level>("staged level", "levels/one.lvl")).Text, "a direct load runs both stages");
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
