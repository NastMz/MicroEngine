using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Cli.Tests;

/// <summary>
/// `microengine content`: checks a game's content folder before the game runs, writes the names of its files, frames
/// and clips as C# constants, and packs a folder of images and sheets into an atlas the engine already loads. Every
/// subcommand has a <c>--check</c> for CI.
/// </summary>
[TestClass]
public sealed class ContentTests
{
    private static byte[] Png(PixelImage image) => PngEncoder.Encode(image.ToRgba(), image.Width, image.Height);

    private static PixelImage Solid(int w, int h, byte red) => new PixelImage(w, h).Clear(new Color(red, 0, 0));

    private static byte[] Wav(short format = 1)
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);
        w.Write("RIFF"u8); w.Write(36 + 4); w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write(format); w.Write((short)1); w.Write(22050); w.Write(44100); w.Write((short)2); w.Write((short)16);
        w.Write("data"u8); w.Write(4); w.Write((short)0); w.Write((short)100);
        w.Flush();
        return stream.ToArray();
    }

    private sealed class Folder : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("me-content-").FullName;
        public string Path(string relative) => System.IO.Path.Combine(Root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
        public Folder Bytes(string relative, byte[] bytes)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path(relative))!);
            File.WriteAllBytes(Path(relative), bytes);
            return this;
        }
        public Folder Text(string relative, string text) => Bytes(relative, System.Text.Encoding.UTF8.GetBytes(text));
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    /// <summary>A small, valid content folder: loose images, a sheet with clips, an atlas a tool wrote, sounds, music and a data file.</summary>
    private static Folder Good() => new Folder()
        .Bytes("items/gem.png", Png(Solid(8, 8, 10)))
        .Bytes("items/red-key.png", Png(Solid(6, 10, 20)))
        .Bytes("enemies/drone.png", Png(Solid(32, 8, 30)))
        .Text("enemies/enemies.sheet", """
            { "sheets": [ { "image": "drone.png", "name": "drone", "cell": [8, 8], "fps": 12 } ],
              "clips": { "dive": { "frames": ["drone3", "drone1"], "fps": 6, "loop": false } } }
            """)
        .Bytes("ui/hud.png", Png(Solid(16, 8, 40)))
        .Text("ui/hud.json", """{"frames": {"heart.png": {"frame": {"x":0,"y":0,"w":8,"h":8}}, "coin.png": {"frame": {"x":8,"y":0,"w":8,"h":8}}}, "meta": {"image": "hud.png"}}""")
        .Bytes("sfx/jump.wav", Wav())
        .Bytes("music/theme.ogg", [.. "OggS"u8, 0, 2, 0, 0])
        .Text("levels/1.txt", "###");

    [TestMethod]
    public void A_sound_content_folder_passes_and_the_command_exits_zero()
    {
        using var content = Good();
        var report = ContentTool.Check(content.Root);
        Assert.AreEqual(0, report.Errors.Count + report.Warnings.Count, string.Join("\n", report.Issues));
        Assert.AreEqual(9, report.Files);
        Assert.AreEqual(0, Cli.RunAsync(["content", "check", "--content", content.Root]).Result);
    }

    [TestMethod]
    public void Every_problem_names_its_file_and_says_what_to_do()
    {
        using var content = Good()
            .Text("items/broken.png", "not an image")
            .Text("enemies/lost.sheet", """{ "sheets": [ { "image": "gone.png", "name": "x" } ] }""")
            .Text("enemies/outside.sheet", """{ "sheets": [ { "image": "drone.png", "cell": [8, 8], "strips": { "s": { "at": [3, 0], "count": 3 } } } ] }""")
            .Text("enemies/clip.sheet", """{ "sheets": [ { "image": "drone.png", "name": "d", "cell": [8, 8] } ], "clips": { "c": { "frames": ["d9"], "fps": 4 } } }""")
            .Text("ui/menu.json", """{"frames": {"a.png": {"frame": {"x":0,"y":0,"w":80,"h":8}}}, "meta": {"image": "hud.png"}}""")
            .Text("ui/orphan.json", """{"frames": {"a.png": {"frame": {"x":0,"y":0,"w":8,"h":8}}}, "meta": {"image": "nowhere.png"}}""")
            .Bytes("sfx/adpcm.wav", Wav(format: 2))
            .Bytes("music/boss.mp3", [1, 2, 3])
            .Bytes("items/gem.psd", [1])
            .Bytes("items/pack.tps", [1])
            .Bytes("items/photo.webp", [1])
            .Bytes("items/big.png", Png(Solid(64, 4, 1)));
        var report = ContentTool.Check(content.Root, maxTexture: 32);
        string all = string.Join("\n", report.Issues.Select(i => i.ToString()));

        Error("items/broken.png", "not an image the engine reads");
        Error("enemies/lost.sheet", "gone.png");
        Error("enemies/outside.sheet", "outside the 32x8 image");
        Error("enemies/clip.sheet", "names the frame 'd9'");
        Error("ui/menu.json", "lies outside the 16x8 atlas image");
        Error("ui/orphan.json", "nowhere.png");
        Error("sfx/adpcm.wav", "PCM");
        Error("items/photo.webp", "PNG");
        Error("items/big.png", "64x4");
        Warning("music/boss.mp3", "OGG");
        Warning("items/gem.psd", "source file");
        Warning("items/pack.tps", "source file");
        Assert.AreEqual(1, Cli.RunAsync(["content", "check", "--content", content.Root, "--max-texture", "32"]).Result, "errors fail the command");

        void Error(string path, string says) => Assert.IsTrue(report.Errors.Any(e => e.Path == path && e.Message.Contains(says, StringComparison.Ordinal)), $"expected an error for {path} saying '{says}' in:\n{all}");
        void Warning(string path, string says) => Assert.IsTrue(report.Warnings.Any(e => e.Path == path && e.Message.Contains(says, StringComparison.Ordinal)), $"expected a warning for {path} saying '{says}' in:\n{all}");
    }

    private static byte[] ExampleFont(string name) =>
        File.ReadAllBytes(System.IO.Path.Combine(Repository(), "samples", "Examples", "Content", "fonts", name));

    [TestMethod]
    public void String_tables_that_agree_pass_with_their_fonts_and_variants()
    {
        using var content = Good()
            .Text("text/en.strings.json", """{ "@name": "English", "@fonts": ["fonts/Inter.ttf"], "play": "Play", "gems": "{n, plural, one {# gem} other {# gems}}" }""")
            .Text("text/es.strings.json", """{ "@name": "Español", "@fonts": ["fonts/Inter.ttf"], "play": "Jugar", "gems": "{n, plural, one {# gema} other {# gemas}}" }""")
            .Text("text/ar.strings.json", """{ "@name": "العربية", "@fonts": ["fonts/Inter.ttf", "fonts/Arabic.ttf"], "play": "العب", "gems": "{n, plural, zero {#} one {#} two {#} few {#} many {#} other {#}}" }""")
            .Bytes("fonts/Inter.ttf", ExampleFont("Inter-Regular.ttf"))
            .Bytes("fonts/Arabic.ttf", ExampleFont("NotoSansArabic-Regular.ttf"))
            .Bytes("ui/hud.es.png", Png(Solid(16, 8, 40)));
        var report = ContentTool.Check(content.Root);
        Assert.AreEqual(0, report.Issues.Count, string.Join("\n", report.Issues));
        Assert.AreEqual(0, Cli.RunAsync(["content", "check", "--content", content.Root]).Result);
    }

    [TestMethod]
    public void String_table_problems_name_the_table_and_the_key()
    {
        using var content = Good()
            .Text("text/en.strings.json", """{ "@fonts": ["fonts/Inter.ttf", "fonts/gone.ttf"], "play": "Play", "quit": "Quit", "found": "{who} found it", "gems": "{n, plural, one {# gem} other {# gems}}" }""")
            .Text("text/es.strings.json", """{ "play": "Jugar", "found": "{quien} lo encontró", "gems": "{n, plural, one {# gema}}" }""")
            .Text("text/ja.strings.json", """{ "@fonts": ["fonts/Inter.ttf"], "play": "プレイ", "quit": "終了", "found": "{who}", "gems": "{n, plural, other {#}}" }""")
            .Text("text/fr.strings.json", "{ \"play\": 3 }")
            .Text("text/menu.strings.json", "{ \"play\": \"Play\" }")
            .Text("more/extra.strings.csv", "key,en,es\nextra,More,\n")
            .Bytes("fonts/Inter.ttf", ExampleFont("Inter-Regular.ttf"))
            .Bytes("ui/banner.es.png", Png(Solid(4, 4, 1)))
            .Bytes("ui/hud.JA.png", Png(Solid(16, 8, 40)));
        var report = ContentTool.Check(content.Root);
        string all = string.Join("\n", report.Issues.Select(i => i.ToString()));

        Error("text/es.strings.json", "'quit'");
        Error("text/es.strings.json", "'extra'");
        Error("text/es.strings.json", "{quien}");
        Error("text/es.strings.json", "'gems'");
        Error("text/fr.strings.json", "line 1");
        Error("text/menu.strings.json", "language tag");
        Error("text/en.strings.json", "fonts/gone.ttf");
        Error("ui/hud.JA.png", "'ja'");
        Warning("text/ja.strings.json", "プレイ終了");
        Warning("ui/banner.es.png", "ui/banner.png");
        Assert.IsFalse(report.Issues.Any(i => i.Path == "more/extra.strings.csv"), "the CSV's own language has the key:\n" + all);
        Assert.AreEqual(1, Cli.RunAsync(["content", "check", "--content", content.Root]).Result, "a missing key fails the command");

        void Error(string path, string says) => Assert.IsTrue(report.Errors.Any(e => e.Path == path && e.Message.Contains(says, StringComparison.Ordinal)), $"expected an error for {path} saying '{says}' in:\n{all}");
        void Warning(string path, string says) => Assert.IsTrue(report.Warnings.Any(e => e.Path == path && e.Message.Contains(says, StringComparison.Ordinal)), $"expected a warning for {path} saying '{says}' in:\n{all}");
    }

    [TestMethod]
    public void Names_that_differ_only_in_letter_case_are_reported()
    {
        // Windows and macOS cannot hold both spellings, so the listing is given directly.
        var issues = ContentTool.NameClashes(["art/Hero.png", "art/hero.png", "Art/tiles.png", "sfx/jump.wav", "sfx/jump.ogg", "ok/a.png"]);
        Assert.IsTrue(issues.Any(i => i.Error && i.Path == "art/hero.png" && i.Message.Contains("art/Hero.png", StringComparison.Ordinal)), string.Join("\n", issues));
        Assert.IsTrue(issues.Any(i => i.Error && i.Message.Contains("'Art'", StringComparison.Ordinal) && i.Message.Contains("'art'", StringComparison.Ordinal)), "folders too");
        Assert.AreEqual(2, issues.Count, "two files of one name and different extensions are no problem: " + string.Join("\n", issues));
        Assert.IsFalse(issues.Any(i => i.Path.StartsWith("ok/", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Names_are_written_as_constants_for_files_frames_and_clips()
    {
        using var content = Good().Bytes("items/gem.jpg", Png(Solid(2, 2, 1))).Bytes("2d/class.png", Png(Solid(2, 2, 1)));
        string code = ContentTool.Names(content.Root, "MyGame", "GameContent");
        StringAssert.Contains(code, "namespace MyGame;");
        StringAssert.Contains(code, "internal static class GameContent");
        StringAssert.Contains(code, "public static class Items");
        StringAssert.Contains(code, "public const string RedKey = \"items/red-key.png\";");
        StringAssert.Contains(code, "public const string GemPng = \"items/gem.png\";", "two files of one name are told apart by their extension");
        StringAssert.Contains(code, "public const string GemJpg = \"items/gem.jpg\";");
        StringAssert.Contains(code, "public static class _2d", "a name C# cannot start with gets an underscore");
        StringAssert.Contains(code, "public const string Class = \"2d/class.png\";");
        // A sheet and an atlas become a class with their path, sprites, strips and clips.
        StringAssert.Contains(code, "public const string Path = \"enemies/enemies.sheet\";");
        StringAssert.Contains(code, "public const string Drone0 = \"drone0\";");
        StringAssert.Contains(code, "public const string Dive = \"dive\";");
        StringAssert.Contains(code, "public const string Heart = \"heart\";");
        int strips = code.IndexOf("public static class Strips", StringComparison.Ordinal);
        Assert.IsTrue(strips > 0 && code.IndexOf("public const string Drone = \"drone\";", strips, StringComparison.Ordinal) > strips, "numbered frames are also named as the strip atlas.Frames takes");
        content.Text("sprites.sheet", """{ "sheets": [ { "image": "items/gem.png", "name": "gem" } ] }""");
        StringAssert.Contains(ContentTool.Names(content.Root, "MyGame", "GameContent"), "public static class SpriteNames", "a class cannot hold a member of its own name");
        File.Delete(content.Path("sprites.sheet"));
        Assert.AreEqual(code, ContentTool.Names(content.Root, "MyGame", "GameContent"), "the same folder gives the same file");
        Assert.IsFalse(code.Contains('\r'));

        string file = content.Path("../" + System.IO.Path.GetFileName(content.Root) + "-names.cs");
        try
        {
            Assert.AreEqual(1, Cli.RunAsync(["content", "names", "--content", content.Root, "--out", file, "--namespace", "MyGame", "--check"]).Result, "missing: out of date");
            Assert.AreEqual(0, Cli.RunAsync(["content", "names", "--content", content.Root, "--out", file, "--namespace", "MyGame"]).Result);
            Assert.AreEqual(code, File.ReadAllText(file));
            Assert.AreEqual(0, Cli.RunAsync(["content", "names", "--content", content.Root, "--out", file, "--namespace", "MyGame", "--check"]).Result);
            content.Bytes("items/new.png", Png(Solid(2, 2, 1)));
            Assert.AreEqual(1, Cli.RunAsync(["content", "names", "--content", content.Root, "--out", file, "--namespace", "MyGame", "--check"]).Result, "a new file makes the names stale");
        }
        finally { File.Delete(file); }
    }

    [TestMethod]
    public void A_folder_packs_into_an_atlas_the_engine_loads_like_the_folder_itself()
    {
        using var content = Good();
        string source = content.Path("items"), enemies = content.Path("enemies");
        // The sheet's frames and clips go into the same atlas as the loose images.
        File.Copy(System.IO.Path.Combine(enemies, "drone.png"), System.IO.Path.Combine(source, "drone.png"));
        File.Copy(System.IO.Path.Combine(enemies, "enemies.sheet"), System.IO.Path.Combine(source, "enemies.sheet"));
        string output = content.Path("atlases/items");

        Assert.AreEqual(1, Cli.RunAsync(["content", "pack", source, "--out", output, "--check"]).Result, "nothing packed yet: out of date");
        Assert.AreEqual(0, Cli.RunAsync(["content", "pack", source, "--out", output]).Result);
        byte[] png = File.ReadAllBytes(output + ".png");
        string json = File.ReadAllText(output + ".json");
        Assert.AreEqual(0, Cli.RunAsync(["content", "pack", source, "--out", output]).Result);
        CollectionAssert.AreEqual(png, File.ReadAllBytes(output + ".png"), "the same input gives the same bytes");
        Assert.AreEqual(json, File.ReadAllText(output + ".json"));
        Assert.AreEqual(0, Cli.RunAsync(["content", "pack", source, "--out", output, "--check"]).Result);

        var device = new HeadlessGraphicsDevice();
        using var assets = new AssetCache(device, content.Root);
        var packed = assets.Atlas("atlases/items.json");
        var folder = assets.AtlasFromFolder("items");
        foreach (var name in new[] { "gem", "red-key" })
        {
            Assert.AreEqual(folder[name].Width, packed[name].Width, name);
            Assert.AreEqual(folder[name].Origin, packed[name].Origin, name);
        }
        Assert.IsFalse(packed.Contains("drone"), "an image a sheet cuts up is not also a sprite of its own");
        Assert.AreEqual(4, packed.Frames("drone").Count);
        Assert.AreEqual(12f, packed.Clip("drone").FramesPerSecond, 0.01f);
        Assert.IsFalse(packed.Clip("dive").Loop);
        Assert.AreEqual(packed["drone3"].Source, packed.Clip("dive").Frames[0].Source);

        File.WriteAllBytes(System.IO.Path.Combine(source, "gem.png"), Png(Solid(8, 8, 99)));
        Assert.AreEqual(1, Cli.RunAsync(["content", "pack", source, "--out", output, "--check"]).Result, "a changed image makes the atlas stale");
    }

    [TestMethod]
    public void Packing_takes_a_pivot_trimming_and_extrusion()
    {
        using var content = new Folder();
        var hero = new PixelImage(16, 16);
        hero.FillRect(4, 4, 8, 12, Color.White);
        content.Bytes("src/hero.png", Png(hero));
        string output = content.Path("out/hero");
        var result = ContentTool.Pack(content.Path("src"), output, new PackOptions(Pivot: new Vector2(0.5f, 1), Trim: true, Extrude: true), check: false);
        Assert.AreEqual(1, result.Sprites);
        var data = AtlasData.Parse(File.ReadAllText(output + ".json"));
        Assert.AreEqual((8f, 12f), (data.Regions[0].Source.Width, data.Regions[0].Source.Height), "trimmed to its opaque pixels");
        Assert.AreEqual(new Vector2(4, 12), data.Regions[0].Origin, "the feet of the whole 16x16 frame, from the trimmed corner");
        var image = PixelImage.Load(output + ".png");
        var at = data.Regions[0].Source;
        Assert.AreEqual(Color.White, image[(int)at.X - 1, (int)at.Y - 1], "extruded: the edge pixels repeat around the sprite");
        var error = Assert.ThrowsExactly<CliException>(() => ContentTool.Pack(content.Path("none"), output, new PackOptions(), check: false));
        StringAssert.Contains(error.Message, "none");
    }

    private static string Repository()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(System.IO.Path.Combine(dir.FullName, "MicroEngine.slnx"))) return dir.FullName;
        throw new InvalidOperationException("MicroEngine.slnx not found above the test output");
    }

    [TestMethod]
    public void The_repositorys_content_passes_the_check_and_its_generated_names_are_current()
    {
        string root = Repository();
        foreach (var game in new[] { "games/NeonRift", "games/CourtyardKey", "games/TrailDuel", "samples/AssetGallery", "samples/Examples", "samples/GemGarden/part4-complete" })
        {
            var report = ContentTool.Check(System.IO.Path.Combine(root, game, "Content"));
            Assert.AreEqual(0, report.Errors.Count, $"{game}:\n{string.Join("\n", report.Errors)}");
        }
        string neon = System.IO.Path.Combine(root, "games", "NeonRift");
        Assert.AreEqual(0, Cli.RunAsync(["content", "names", "--content", System.IO.Path.Combine(neon, "Content"), "--out", System.IO.Path.Combine(neon, "Art", "GameContent.g.cs"), "--namespace", "NeonRift.Art", "--check"]).Result,
            "out of date: run microengine content names --project games/NeonRift --out games/NeonRift/Art/GameContent.g.cs --namespace NeonRift.Art");
    }
}
