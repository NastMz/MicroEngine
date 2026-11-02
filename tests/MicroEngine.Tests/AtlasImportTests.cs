using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tests;

/// <summary>Atlases from packing tools (ADR 0025): TexturePacker and Aseprite JSON, Starling XML, and run-time packing for linear sampling.</summary>
[TestClass]
public sealed class AtlasImportTests
{
    private static string Gallery(string path) => Path.Combine(AppContext.BaseDirectory, "Gallery", path);

    // TexturePacker's "JSON (Hash)": trimmed frames, a pivot on one of them, the image in meta.
    private const string TexturePackerHash = """
        {"frames": {
          "hero_idle.png": {"frame": {"x":2,"y":2,"w":20,"h":30}, "rotated": false, "trimmed": true,
                            "spriteSourceSize": {"x":6,"y":2,"w":20,"h":30}, "sourceSize": {"w":32,"h":32}},
          "hero_jump.png": {"frame": {"x":24,"y":2,"w":32,"h":32}, "rotated": false, "trimmed": false,
                            "spriteSourceSize": {"x":0,"y":0,"w":32,"h":32}, "sourceSize": {"w":32,"h":32}, "pivot": {"x":0.5,"y":1}}
        },
        "meta": {"app": "https://www.codeandweb.com/texturepacker", "image": "hero.png", "size": {"w":64,"h":64}}}
        """;

    // Aseprite's "Array" export: durations per frame and frame tags with directions.
    private const string AsepriteArray = """
        {"frames": [
          {"filename": "slime 0.aseprite", "frame": {"x":0,"y":0,"w":16,"h":16}, "rotated": false, "trimmed": false,
           "spriteSourceSize": {"x":0,"y":0,"w":16,"h":16}, "sourceSize": {"w":16,"h":16}, "duration": 100},
          {"filename": "slime 1.aseprite", "frame": {"x":16,"y":0,"w":16,"h":16}, "rotated": false, "trimmed": false,
           "spriteSourceSize": {"x":0,"y":0,"w":16,"h":16}, "sourceSize": {"w":16,"h":16}, "duration": 200},
          {"filename": "slime 2.aseprite", "frame": {"x":32,"y":0,"w":16,"h":16}, "rotated": false, "trimmed": false,
           "spriteSourceSize": {"x":0,"y":0,"w":16,"h":16}, "sourceSize": {"w":16,"h":16}, "duration": 300}
        ],
        "meta": {"app": "https://www.aseprite.org/", "image": "slime.png", "size": {"w":48,"h":16},
                 "frameTags": [{"name": "bounce", "from": 0, "to": 2, "direction": "pingpong"},
                               {"name": "back", "from": 0, "to": 1, "direction": "reverse"}]}}
        """;

    [TestMethod]
    public void TexturePacker_json_keeps_trimmed_frames_in_place_and_reads_pivots()
    {
        var data = AtlasData.Parse(TexturePackerHash);
        Assert.AreEqual("hero.png", data.ImagePath);
        Assert.AreEqual(2, data.Regions.Count);
        var idle = data.Regions[0];
        Assert.AreEqual("hero_idle", idle.Name, "the image extension is dropped");
        Assert.AreEqual(new RectF(2, 2, 20, 30), idle.Source);
        // Top-left anchor of the untrimmed 32×32 frame: 6 pixels left of and 2 above the trimmed pixels.
        Assert.AreEqual(new Vector2(-6, -2), idle.Origin);
        Assert.AreEqual(new Vector2(16, 32), data.Regions[1].Origin, "pivot (0.5, 1) of a 32×32 frame");
        var feet = AtlasData.Parse(TexturePackerHash, new AtlasOptions(Pivot: new Vector2(0.5f, 1)));
        Assert.AreEqual(new Vector2(16 - 6, 32 - 2), feet.Regions[0].Origin, "a default pivot applies to frames without one");
        Assert.AreEqual("hero_idle.png", AtlasData.Parse(TexturePackerHash, new AtlasOptions(KeepExtensions: true)).Regions[0].Name);
    }

    [TestMethod]
    public void Aseprite_tags_become_clips_with_each_frames_duration_and_direction()
    {
        var device = new HeadlessGraphicsDevice();
        using var atlas = SpriteAtlas.Create(device.CreateTexture(new PixelImage(48, 16)), AtlasData.Parse(AsepriteArray));
        CollectionAssert.AreEquivalent(new[] { "bounce", "back" }, atlas.Animations.ToArray());
        var bounce = atlas.Clip("bounce");
        // Ping-pong: 0 1 2 1, then again; durations follow the frames.
        CollectionAssert.AreEqual(new[] { atlas["slime 0"], atlas["slime 1"], atlas["slime 2"], atlas["slime 1"] }, bounce.Frames.ToArray());
        CollectionAssert.AreEqual(new[] { 0.1f, 0.2f, 0.3f, 0.2f }, bounce.FrameDurations.ToArray());
        Assert.AreEqual(0.8f, bounce.Duration, 1e-5);
        Assert.AreSame(atlas["slime 0"], bounce.FrameAt(0.05f));
        Assert.AreSame(atlas["slime 1"], bounce.FrameAt(0.1f), "a frame starts exactly when the previous ends");
        Assert.AreSame(atlas["slime 2"], bounce.FrameAt(0.35f));
        Assert.AreSame(atlas["slime 1"], bounce.FrameAt(0.7f));
        Assert.AreSame(atlas["slime 0"], bounce.FrameAt(0.85f), "loops");
        var once = atlas.Clip("bounce", loop: false);
        Assert.AreSame(atlas["slime 1"], once.FrameAt(5f), "holds the last frame");
        Assert.AreSame(bounce, atlas.Clip("bounce"), "clips are built once");
        CollectionAssert.AreEqual(new[] { atlas["slime 1"], atlas["slime 0"] }, atlas.Clip("back").Frames.ToArray());
        var error = Assert.ThrowsExactly<KeyNotFoundException>(() => atlas.Clip("run"));
        StringAssert.Contains(error.Message, "bounce");
    }

    [TestMethod]
    public void Starling_xml_from_a_real_pack_loads_with_its_trimming_and_numbered_frames()
    {
        var device = new HeadlessGraphicsDevice();
        using var atlas = SpriteAtlas.Load(device, Gallery("atlases/players.xml"), new AtlasOptions(Pivot: new Vector2(0.5f, 1)));
        Assert.AreEqual(72, atlas.Names.Count);
        Assert.AreEqual(512, atlas.Texture.Width);
        var stand = atlas["playerBlue_stand"];
        Assert.IsTrue(stand.Width > 20 && stand.Height > 20);
        // Kenney numbers walk frames from 1; Frames finds them all.
        Assert.AreEqual(5, atlas.Frames("playerBlue_walk").Count);
        Assert.AreSame(atlas["playerBlue_walk1"], atlas.Frames("playerBlue_walk")[0]);
        Assert.AreEqual(5, atlas.Clip("playerBlue_walk", 10).Frames.Count);
        Assert.AreEqual(1, device.LiveTextures);
    }

    [TestMethod]
    public void The_galleries_aseprite_export_loads_its_tags()
    {
        var device = new HeadlessGraphicsDevice();
        using var atlas = SpriteAtlas.Load(device, Gallery("atlases/enemies.json"));
        Assert.AreEqual(24, atlas.Names.Count);
        Assert.AreEqual(12, atlas.Animations.Count);
        Assert.AreEqual(4, atlas.Clip("flying").Frames.Count, "ping-pong of three frames");
        Assert.AreEqual(0.48f, atlas.Clip("flying").Duration, 1e-5);
    }

    [TestMethod]
    public void Broken_atlases_fail_with_what_is_wrong()
    {
        StringAssert.Contains(Assert.ThrowsExactly<InvalidDataException>(() => AtlasData.Parse("hello")).Message, "Not an atlas");
        StringAssert.Contains(Assert.ThrowsExactly<InvalidDataException>(() => AtlasData.Parse("{\"meta\":{}}")).Message, "frames");
        StringAssert.Contains(Assert.ThrowsExactly<InvalidDataException>(() => AtlasData.Parse(
            """{"frames":[{"filename":"a","frame":{"x":0,"y":0,"w":4,"h":4},"rotated":true}]}""")).Message, "rotat");
        StringAssert.Contains(Assert.ThrowsExactly<InvalidDataException>(() => AtlasData.Parse(
            """<TextureAtlas imagePath="a.png"><SubTexture name="x" x="0" y="0" width="4" height="4" rotated="true"/></TextureAtlas>""")).Message, "rotat");
        StringAssert.Contains(Assert.ThrowsExactly<InvalidDataException>(() => AtlasData.Parse("<root/>")).Message, "TextureAtlas");
        StringAssert.Contains(Assert.ThrowsExactly<InvalidDataException>(() => AtlasData.Parse(
            """{"frames":[{"filename":"a","frame":{"x":0,"y":0,"w":4,"h":4}}],"meta":{"frameTags":[{"name":"t","from":0,"to":3}]}}""")).Message, "frames 0-3");
        var device = new HeadlessGraphicsDevice();
        var tooSmall = device.CreateTexture(new PixelImage(8, 8));
        StringAssert.Contains(Assert.ThrowsExactly<InvalidDataException>(() => SpriteAtlas.Create(tooSmall, AtlasData.Parse(TexturePackerHash))).Message, "outside");
    }

    /// <summary>A device that keeps the images it was given, to look at packed sheets.</summary>
    private sealed class KeepingDevice : IGraphicsDevice
    {
        private readonly HeadlessGraphicsDevice _inner = new();
        public PixelImage? Last;
        public TextureFilter DefaultFilter { get; set; }
        public Texture CreateTexture(PixelImage image, TextureFilter filter) { Last = image; return _inner.CreateTexture(image, filter); }
    }

    [TestMethod]
    public void Run_time_atlases_sampled_linearly_repeat_each_edge_into_a_border()
    {
        var tile = new PixelImage(2, 2).Clear(new Color(200, 10, 10));
        tile[1, 1] = new Color(10, 200, 10);
        var device = new KeepingDevice();
        using var nearest = new SpriteAtlasBuilder().Add("tile", tile).Build(device, TextureFilter.Nearest);
        Assert.AreEqual(new RectF(0, 0, 2, 2), nearest["tile"].Source, "nearest atlases keep their layout");
        Assert.AreEqual(Color.Transparent, device.Last![2, 0]);
        using var linear = new SpriteAtlasBuilder().Add("tile", tile).Build(device, TextureFilter.Linear);
        Assert.AreEqual(TextureFilter.Linear, linear.Texture.Filter);
        var r = linear["tile"].Source;
        Assert.AreEqual(new RectF(1, 1, 2, 2), r);
        var sheet = device.Last!;
        Assert.AreEqual(tile[0, 0], sheet[0, 0], "corner");
        Assert.AreEqual(tile[1, 1], sheet[3, 3], "opposite corner");
        Assert.AreEqual(tile[1, 0], sheet[2, 0], "top edge");
        Assert.AreEqual(tile[1, 1], sheet[3, 2], "right edge");
        Assert.AreEqual(Color.Transparent, sheet[4, 0], "a gap still separates neighbours");
        device.DefaultFilter = TextureFilter.Linear;
        using var byDefault = new SpriteAtlasBuilder().Add("tile", tile).Build(device);
        Assert.AreEqual(TextureFilter.Linear, byDefault.Texture.Filter, "the device's default filter applies");
    }

    [TestMethod]
    public void Timed_clips_check_their_durations()
    {
        var device = new HeadlessGraphicsDevice();
        var sprite = new Sprite(device.CreateTexture(new PixelImage(2, 2)), new RectF(0, 0, 2, 2), Vector2.Zero);
        Assert.ThrowsExactly<ArgumentException>(() => new AnimationClip([sprite, sprite], [0.1f]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new AnimationClip([sprite], [0f]));
        var fixedRate = new AnimationClip([sprite, sprite], 4);
        CollectionAssert.AreEqual(new[] { 0.25f, 0.25f }, fixedRate.FrameDurations.ToArray());
        Assert.AreEqual(0.5f, fixedRate.Duration);
    }
}
