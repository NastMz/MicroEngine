using System.IO.Compression;
using MicroEngine.Audio;
using MicroEngine.Content;
using MicroEngine.Core;
using MicroEngine.Graphics;
using MicroEngine.Platform;

namespace MicroEngine.Tests.Content;

/// <summary>
/// The asset store of MicroEngine.Content: typed handles with generations, scopes that share and release, the loader
/// registry, and content sources beyond folders (zip archives, listed files).
/// </summary>
[TestClass]
public sealed class AssetStoreTests
{
    /// <summary>A disposable asset that records its disposal.</summary>
    private sealed class Thing(string path) : IDisposable
    {
        public string Path { get; } = path;
        public int Disposed { get; private set; }
        public void Dispose() => Disposed++;
    }

    private static (AssetStore Store, List<string> Loads) Open(IContentSource? source = null)
    {
        var loads = new List<string>();
        var loaders = new AssetLoaderRegistry().Add(new AssetLoader<Thing>("thing", c =>
        {
            loads.Add(c.Path);
            _ = c.ReadBytes();
            return new Thing(c.Path);
        }, [".thing"], _ => 10));
        return (new AssetStore(source ?? Listed("a.thing", "b.thing", "sub/c.thing"), loaders), loads);
    }

    private static ListedContentSource Listed(params string[] files) =>
        new("test files", files, path => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(path)));

    [TestMethod]
    public void A_handle_reaches_its_asset_until_every_holder_releases_it_then_it_is_stale()
    {
        var (store, loads) = Open();
        var level1 = store.Root.CreateScope("level 1");
        var level2 = store.Root.CreateScope("level 2");
        var a = level1.Load<Thing>("thing", "a.thing");
        var again = level2.Load<Thing>("thing", "./a.thing");
        Assert.AreEqual(a, again, "one entry per file, whichever scope asks and however it is spelled");
        Assert.AreEqual(1, loads.Count, "loaded once");
        var thing = store.Get(a);
        Assert.AreEqual("a.thing", thing.Path);

        Assert.IsTrue(level1.Release(a));
        Assert.AreEqual(0, thing.Disposed, "level 2 still holds it");
        Assert.AreSame(thing, store.Get(a));
        level2.Dispose();
        Assert.AreEqual(1, thing.Disposed, "disposed once, with its last holder");
        Assert.IsFalse(store.IsLoaded(a));
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => store.Get(a)).Message, "stale");

        var reloaded = level1.Load<Thing>("thing", "a.thing");
        Assert.AreEqual(a.Index, reloaded.Index, "the slot is reused");
        Assert.AreNotEqual(a.Generation, reloaded.Generation, "with a new generation");
        Assert.IsFalse(store.TryGet(a, out _), "the old handle never reaches the new asset");
        Assert.AreNotSame(thing, store.Get(reloaded));
        Assert.AreEqual(2, loads.Count);
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => store.Get(Asset<Thing>.None)).Message, "nothing");
    }

    [TestMethod]
    public void A_scope_releases_only_its_own_holds_and_children_go_with_their_parent()
    {
        var (store, _) = Open();
        var root = store.Root;
        var shared = root.Load<Thing>("thing", "a.thing");
        var level = root.CreateScope();
        var room = level.CreateScope();
        Assert.AreEqual(shared, room.Load<Thing>("thing", "a.thing"));
        var own = room.Load<Thing>("thing", "sub/c.thing");
        Assert.AreEqual(2, level.Count, "a scope counts what it and its children hold");
        Assert.AreEqual(2, store.Count);
        Assert.IsTrue(room.IsLoaded("sub/c.thing"));
        Assert.IsFalse(root.IsLoaded("sub/c.thing"), "a parent does not see its children's assets");
        Assert.IsTrue(room.IsLoaded("a.thing"), "a child sees its parents'");
        var c = store.Get(own);
        level.Dispose();
        Assert.IsTrue(room.IsDisposed, "children are released with their parent");
        Assert.AreEqual(1, c.Disposed);
        Assert.AreEqual(0, store.Get(shared).Disposed, "the root still holds it");
        Assert.AreEqual(1, store.Count);
        Assert.ThrowsExactly<ObjectDisposedException>(() => room.Load<Thing>("thing", "b.thing"));
        store.Dispose();
        Assert.AreEqual(0, store.Count);
    }

    [TestMethod]
    public void Records_report_kind_size_time_and_loads_during_play()
    {
        var (store, _) = Open();
        store.Root.Load<Thing>("thing", "a.thing");
        store.Root.Playing = true;
        var level = store.Root.CreateScope();
        Assert.IsTrue(level.Playing, "a new scope inherits the flag");
        level.Load<Thing>("thing", "b.thing");
        var records = store.Root.Loaded;
        Assert.AreEqual(2, records.Count);
        Assert.AreEqual(new AssetRecord("a.thing", "thing", 10, records[0].LoadMilliseconds, false), records[0]);
        Assert.IsTrue(records[1].DuringPlay);
        Assert.AreEqual(2, store.Root.Objects.Count);
    }

    [TestMethod]
    public void Loaders_are_chosen_by_kind_and_failures_name_the_asset()
    {
        var (store, _) = Open();
        Assert.AreEqual("thing", store.Loaders.KindOf("x/y.THING"));
        Assert.IsNull(store.Loaders.KindOf("x/y.png"));
        CollectionAssert.AreEqual(new[] { "thing" }, store.Loaders.Kinds.ToArray());
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => store.Root.Load<Thing>("model", "a.thing")).Message, "Registered: thing");
        StringAssert.Contains(Assert.ThrowsExactly<InvalidOperationException>(() => store.Root.Load<Texture>("thing", "a.thing")).Message, "Thing");
        var missing = Assert.ThrowsExactly<AssetLoadException>(() => store.Root.Load<Thing>("thing", "none.thing"));
        Assert.AreEqual(("none.thing", "thing"), (missing.AssetPath, missing.Kind));
        StringAssert.Contains(missing.Message, "not found");
        var wrongCase = Assert.ThrowsExactly<AssetLoadException>(() => store.Root.Load<Thing>("thing", "Sub/c.thing"));
        StringAssert.Contains(wrongCase.Message, "'sub/c.thing'", "the message gives the real spelling");
        Assert.ThrowsExactly<AssetLoadException>(() => store.Root.Load<Thing>("thing", "../a.thing"));
        Assert.AreEqual(0, store.Count, "nothing half-loaded stays");

        // A loader registered later under the same kind replaces the first (how a module plugs its loader in).
        store.Loaders.Add(new AssetLoader<Thing>("thing", c => new Thing("replaced " + c.Path)));
        Assert.AreEqual("replaced a.thing", store.Get(store.Root.Load<Thing>("thing", "a.thing")).Path);
    }

    [TestMethod]
    public void Values_made_elsewhere_join_the_store_and_lookups_never_load()
    {
        var (store, loads) = Open();
        var key = new AssetKey("thing", "b.thing");
        Assert.IsFalse(store.Root.TryAcquire<Thing>(key, out _));
        var made = new Thing("b.thing");
        var handle = store.Root.Add(key, made, 1.5);
        Assert.AreSame(made, store.Get(handle));
        Assert.AreEqual(handle, store.Root.HandleOf<Thing>(key));
        var level = store.Root.CreateScope();
        Assert.IsTrue(level.HoldLoaded("thing", "b.thing"), "any options of that kind");
        Assert.IsTrue(level.TryAcquire<Thing>(key, out var found));
        Assert.AreSame(made, found);
        Assert.AreEqual(0, loads.Count);
        Assert.AreEqual(10, store.Root.Loaded[0].Bytes, "sized by the kind's loader");
        Assert.ThrowsExactly<InvalidOperationException>(() => store.Root.Add(key, new Thing("b.thing"), 0));
        Assert.AreSame(made, level.Acquire<Thing>(key, _ => throw new InvalidOperationException("not called on a hit")));
    }

    [TestMethod]
    public void Options_and_variants_tell_loads_of_one_file_apart()
    {
        var (store, loads) = Open();
        var a = store.Root.Load<Thing>("thing", "a.thing", options: "big");
        var b = store.Root.Load<Thing>("thing", "a.thing", options: "big");
        var c = store.Root.Load<Thing>("thing", "a.thing", options: "small");
        var d = store.Root.Load<Thing>("thing", "a.thing", options: "big", variant: 2);
        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, c);
        Assert.AreNotEqual(a, d);
        Assert.AreEqual(3, loads.Count);
        Assert.IsTrue(store.Root.Release("a.thing"), "releasing a path lets go of every form");
        Assert.AreEqual(0, store.Count);
    }

    [TestMethod]
    public void The_source_opens_on_the_first_load_and_its_failure_names_it()
    {
        int opened = 0;
        var store = new AssetStore(() => { opened++; return Listed("a.thing"); });
        Assert.IsFalse(store.IsSourceOpen);
        Assert.AreEqual(0, opened, "a game without content files never opens one");
        _ = store.Source;
        _ = store.Source;
        Assert.AreEqual(1, opened);
        var broken = new AssetStore(() => new FolderContentSource(Path.Combine(Path.GetTempPath(), "microengine-no-such-folder-" + Guid.NewGuid())));
        var error = Assert.ThrowsExactly<AssetLoadException>(() => broken.Source);
        Assert.AreEqual("content source", error.Kind);
        StringAssert.Contains(error.Message, "Content folder not found");
    }

    private static byte[] Zip(params (string Name, byte[] Data)[] files)
    {
        var memory = new MemoryStream();
        using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (name, data) in files)
                using (var entry = zip.CreateEntry(name).Open()) entry.Write(data);
        return memory.ToArray();
    }

    [TestMethod]
    public void A_zip_archive_is_a_content_source_with_the_folder_rules()
    {
        var bytes = Zip(("items/gem.thing", [1, 2, 3]), ("items/deep/key.thing", [4]), ("readme.txt", [5]));
        using var zip = new ZipContentSource(new MemoryStream(bytes), "game.zip");
        Assert.AreEqual("game.zip", zip.Location);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, zip.ReadAllBytes("items/gem.thing"));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, zip.ReadAllBytes("./items/../items/gem.thing"));
        Assert.IsTrue(zip.Exists("items/deep/key.thing"));
        Assert.IsFalse(zip.Exists("items/Gem.thing"), "letter case counts");
        Assert.IsFalse(zip.Exists("../x"));
        CollectionAssert.AreEqual(new[] { "readme.txt" }, zip.Files("").ToArray());
        CollectionAssert.AreEqual(new[] { "items/gem.thing" }, zip.Files("items").ToArray(), "only the files directly inside");
        CollectionAssert.AreEqual(new[] { "items/deep/key.thing" }, zip.Files("items/deep/").ToArray());
        StringAssert.Contains(Assert.ThrowsExactly<FileNotFoundException>(() => zip.Open("items/Gem.thing")).Message, "'items/gem.thing'");
        StringAssert.Contains(Assert.ThrowsExactly<FileNotFoundException>(() => zip.Open("Items/gem.thing")).Message, "'items/gem.thing'", "folders too");
        StringAssert.Contains(Assert.ThrowsExactly<DirectoryNotFoundException>(() => zip.Files("Items")).Message, "'items'");
        Assert.ThrowsExactly<ArgumentException>(() => zip.Open("../outside.thing"));
        Assert.ThrowsExactly<FileNotFoundException>(() => zip.Open("none.thing"));
        Assert.ThrowsExactly<InvalidDataException>(() => new ZipContentSource(new MemoryStream([1, 2, 3]), "broken.zip"));

        // Several threads read one archive.
        Parallel.For(0, 64, i => CollectionAssert.AreEqual(new byte[] { 4 }, zip.ReadAllBytes("items/deep/key.thing")));

        var (store, loads) = Open(zip);
        var gem = store.Root.Load<Thing>("thing", "items/gem.thing");
        Assert.AreEqual("items/gem.thing", store.Get(gem).Path);
        Assert.AreEqual(1, loads.Count);
    }

    [TestMethod]
    public void A_folder_or_an_archive_opens_from_its_path()
    {
        var folder = Directory.CreateTempSubdirectory("microengine-content-").FullName;
        try
        {
            File.WriteAllBytes(Path.Combine(folder, "a.txt"), [7]);
            var archive = Path.Combine(folder, "content.zip");
            File.WriteAllBytes(archive, Zip(("a.txt", [8])));
            Assert.IsInstanceOfType<FolderContentSource>(ContentSources.Open(folder));
            var zip = ContentSources.Open(archive);
            Assert.IsInstanceOfType<ZipContentSource>(zip);
            Assert.AreEqual(8, zip.ReadAllBytes("a.txt")[0]);
            ((IDisposable)zip).Dispose();
            StringAssert.Contains(Assert.ThrowsExactly<FileNotFoundException>(() => ContentSources.Open(Path.Combine(folder, "a.txt"))).Message, "folder or a .zip");
            Assert.ThrowsExactly<FileNotFoundException>(() => ContentSources.Open(Path.Combine(folder, "none")));
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    [TestMethod]
    public void Listed_files_are_opened_by_the_host_exactly_as_listed()
    {
        var opened = new List<string>();
        var source = new ListedContentSource("assets", ["Content/a.wav", "Content/music/theme.ogg"], path =>
        {
            opened.Add(path);
            return new MemoryStream([1]);
        });
        Assert.AreEqual(1, source.ReadAllBytes("Content/music/../a.wav")[0]);
        CollectionAssert.AreEqual(new[] { "Content/a.wav" }, opened);
        CollectionAssert.AreEqual(new[] { "Content/music/theme.ogg" }, source.Files("Content/music").ToArray());
        CollectionAssert.AreEqual(Array.Empty<string>(), source.Files("").ToArray(), "a folder holding only a folder lists no file");
        StringAssert.Contains(Assert.ThrowsExactly<FileNotFoundException>(() => source.Open("content/a.wav")).Message, "'Content/a.wav'");
    }

    [TestMethod]
    public void Sounds_and_music_are_asset_kinds_of_the_games_store()
    {
        var folder = Directory.CreateTempSubdirectory("microengine-audio-").FullName;
        try
        {
            var clip = new AudioClip([0.5f, -0.5f, 0.25f, 0f], 22050);
            File.WriteAllBytes(Path.Combine(folder, "beep.wav"), clip.ToWav());
            File.WriteAllBytes(Path.Combine(folder, "theme.ogg"), [1, 2, 3, 4]);
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Gallery", "items", "blueGem.png"), Path.Combine(folder, "gem.png"));
            var assets = new AssetCache(new HeadlessGraphicsDevice(), folder);
            Assert.AreEqual("sound", assets.Loaders.KindOf("beep.wav"));
            Assert.AreEqual("music", assets.Loaders.KindOf("theme.ogg"));
            var beep = assets.Scope.Load<AudioClip>("sound", "beep.wav");
            var decoded = assets.Store.Get(beep);
            Assert.AreEqual(22050, decoded.SampleRate);
            Assert.AreEqual(4, decoded.Samples.Length);
            Assert.AreEqual(0.5f, decoded.Samples[0], 1e-4f);
            Assert.AreEqual(beep, assets.Scope.Load<AudioClip>("sound", "beep.wav"), "shared like any asset");
            var theme = assets.Store.Get(assets.Scope.Load<EncodedAudio>("music", "theme.ogg"));
            Assert.AreEqual("ogg", theme.Format);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, theme.Data);
            Assert.AreEqual("wav", assets.Store.Get(assets.Scope.Load<EncodedAudio>("music", "beep.wav")).Format);
            Assert.AreEqual(4L * sizeof(float), assets.Loaded.Single(a => a.Kind == "sound").Bytes);
            var gem = assets.Texture("gem.png");
            var statistics = assets.Statistics;
            Assert.AreEqual(gem.SizeInBytes, statistics.TextureBytes, "texture bytes count textures only");
            Assert.AreEqual(4L * sizeof(float) + 4 + clip.ToWav().Length, statistics.AudioBytes, "decoded samples and music files are audio bytes");
            assets.Unload("gem.png");
            Assert.AreEqual(3, assets.Count, "the cache counts every kind");
            var broken = Assert.ThrowsExactly<AssetLoadException>(() => assets.Scope.Load<AudioClip>("sound", "theme.ogg"));
            Assert.AreEqual("sound", broken.Kind);
            assets.Dispose();
            Assert.IsFalse(assets.Store.IsLoaded(beep));
        }
        finally { Directory.Delete(folder, recursive: true); }
    }
}
