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
        .Bytes("music/sting.mp3", Mp3)
        .Text("levels/1.txt", "###");

    /// <summary>Half a second of a 441 Hz tone as a 16 kbit/s MP3 file (made with ffmpeg).</summary>
    private static byte[] Mp3 => Convert.FromBase64String(
        "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");

    [TestMethod]
    public void A_sound_content_folder_passes_and_the_command_exits_zero()
    {
        using var content = Good();
        var report = ContentTool.Check(content.Root);
        Assert.AreEqual(0, report.Errors.Count + report.Warnings.Count, string.Join("\n", report.Issues));
        Assert.AreEqual(10, report.Files);
        Assert.AreEqual(0, Cli.RunAsync(["content", "check", "--content", content.Root]).Result);
    }

    [TestMethod]
    public void Points_and_boxes_that_cannot_be_meant_are_reported_with_their_file_and_name()
    {
        using var content = Good()
            .Text("enemies/far.sheet", """
                { "sheets": [ { "image": "drone.png", "name": "far", "cell": [8, 8],
                    "points": { "muzzle": [11, 3], "typo": [[4, 4], [400, 4], null, [4, -90]] },
                    "boxes": { "reach": [6, 0, 10, 8], "lost": [300, 300, 4, 4] } } ] }
                """)
            .Text("enemies/endless.sheet", """{ "sheets": [ { "image": "drone.png", "name": "endless", "cell": [8, 8], "points": { "p": [1e40, 2] } } ] }""")
            .Text("enemies/wide.sheet", """{ "sheets": [ { "image": "drone.png", "name": "wide", "cell": [8, 8], "boxes": { "b": [0, 0, 1e40, 2] } } ] }""");
        var report = ContentTool.Check(content.Root);
        string all = string.Join("\n", report.Issues);
        Assert.IsTrue(report.Warnings.Any(w => w.Path == "enemies/far.sheet" && w.Message.Contains("\"typo\"", StringComparison.Ordinal) && w.Message.Contains("far1", StringComparison.Ordinal)), all);
        Assert.IsTrue(report.Warnings.Any(w => w.Path == "enemies/far.sheet" && w.Message.Contains("\"typo\"", StringComparison.Ordinal) && w.Message.Contains("far3", StringComparison.Ordinal)), all);
        Assert.IsTrue(report.Warnings.Any(w => w.Path == "enemies/far.sheet" && w.Message.Contains("\"lost\"", StringComparison.Ordinal)), all);
        Assert.IsFalse(report.Issues.Any(i => i.Message.Contains("\"muzzle\"", StringComparison.Ordinal) || i.Message.Contains("\"reach\"", StringComparison.Ordinal)),
            "a muzzle ahead of the sprite and a box that reaches out of it are what points and boxes are for:\n" + all);
        Assert.IsTrue(report.Errors.Any(e => e.Path == "enemies/endless.sheet" && e.Message.Contains("\"p\"", StringComparison.Ordinal)), all);
        Assert.IsTrue(report.Errors.Any(e => e.Path == "enemies/wide.sheet" && e.Message.Contains("\"b\"", StringComparison.Ordinal)), all);
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
        Error("music/boss.mp3", "not an MP3 file");
        Assert.IsFalse(all.Contains("raylib"), "MP3 decodes on every platform: no warning about one");
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
    public void The_strips_of_a_sheet_are_the_ones_it_declares()
    {
        // Neon Rift's sprites.sheet: three props named "prop_tank-1".."prop_tank-3", each an image of its own, were
        // listed as a strip "prop_tank-", and the strips "hit1".."hit5" (frames "hit10", "hit11"...) were not listed.
        using var content = new Folder()
            .Bytes("tank-1.png", Png(Solid(2, 2, 1))).Bytes("tank-2.png", Png(Solid(2, 2, 2))).Bytes("tank-3.png", Png(Solid(2, 2, 3)))
            .Bytes("hit1.png", Png(Solid(4, 2, 4))).Bytes("hit2.png", Png(Solid(4, 2, 5))).Bytes("tiles.png", Png(Solid(8, 2, 6)))
            .Text("sprites.sheet", """
                { "sheets": [
                    { "image": "tank-1.png", "name": "prop_tank-1" },
                    { "image": "tank-2.png", "name": "prop_tank-2" },
                    { "image": "tank-3.png", "name": "prop_tank-3" },
                    { "image": "hit1.png", "name": "hit1", "cell": [2, 2], "fps": 28, "loop": false },
                    { "image": "hit2.png", "name": "hit2", "cell": [2, 2] },
                    { "image": "tiles.png", "cell": [2, 2], "strips": { "lava": { "from": 1, "count": 2 } }, "sprites": { "rock1": [0, 0], "rock2": [3, 0] } }
                ] }
                """);
        string code = ContentTool.Names(content.Root, "MyGame", "GameContent");
        int strips = code.IndexOf("public static class Strips", StringComparison.Ordinal);
        Assert.IsTrue(strips > 0, code);
        string listed = code[strips..code.IndexOf('}', strips)];
        StringAssert.Contains(listed, "public const string Hit1 = \"hit1\";", "a strip with a rate");
        StringAssert.Contains(listed, "public const string Hit2 = \"hit2\";", "a strip without a rate");
        StringAssert.Contains(listed, "public const string Lava = \"lava\";", "a strip of a sheet's \"strips\"");
        Assert.IsFalse(listed.Contains("prop_tank-", StringComparison.Ordinal), "three sprites of their own are not a strip:\n" + listed);
        Assert.IsFalse(listed.Contains("\"rock\"", StringComparison.Ordinal), "nor are two named sprites that end in a number:\n" + listed);
        StringAssert.Contains(code, "public const string PropTank1 = \"prop_tank-1\";", "the sprites are still named");
        CollectionAssert.AreEqual(new[] { "hit1", "hit2", "lava" }, MicroEngine.Graphics.SpriteSheet.Load(content.Path("sprites.sheet")).Strips.ToArray());
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
