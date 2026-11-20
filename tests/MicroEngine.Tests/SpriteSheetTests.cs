using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tests;

/// <summary>
/// Sheets described by data: a <c>.sheet</c> file next to its images gives frames, pivots and clips, loaded through
/// <c>Assets.Atlas</c> into an ordinary <see cref="SpriteAtlas"/>, so no slicing table or frame rate lives in code.
/// </summary>
[TestClass]
public sealed class SpriteSheetTests
{
    /// <summary>A sheet whose pixel (x, y) is opaque with red = column and green = row of its <paramref name="cell"/>-sized cell.</summary>
    private static PixelImage Grid(int columns, int rows, int cell, int margin = 0, int spacing = 0, int opaqueCells = int.MaxValue)
    {
        var image = new PixelImage(2 * margin + columns * cell + (columns - 1) * spacing, 2 * margin + rows * cell + (rows - 1) * spacing);
        for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                if (row * columns + column >= opaqueCells) continue;
                image.FillRect(margin + column * (cell + spacing), margin + row * (cell + spacing), cell, cell, new Color((byte)(column * 10), (byte)(row * 10), 200));
            }
        return image;
    }

    private static byte[] Png(PixelImage image) => PngEncoder.Encode(image.ToRgba(), image.Width, image.Height);

    private static SpriteSheet Parse(string json, PixelImage image, AtlasOptions? options = null) =>
        SpriteSheet.Parse(json, _ => image.Clone(), options);

    [TestMethod]
    public void A_grid_image_becomes_a_named_strip_with_its_origin_and_a_clip_at_its_rate()
    {
        var sheet = Parse("""{ "sheets": [ { "image": "drone.png", "name": "drone", "cell": [8, 8], "origin": [4, 7], "fps": 12 } ] }""", Grid(4, 1, 8));
        CollectionAssert.AreEqual(new[] { "drone0", "drone1", "drone2", "drone3" }, sheet.Frames.Select(f => f.Name).ToArray());
        Assert.AreEqual(new Vector2(4, 7), sheet.Frames[2].Origin);
        Assert.AreEqual(new Color(20, 0, 200), sheet.Frames[2].Image[0, 0], "the third cell's pixels");
        var clip = sheet.Clips.Single();
        Assert.AreEqual("drone", clip.Name);
        Assert.AreEqual(12f, clip.FramesPerSecond);
        Assert.IsNull(clip.Durations);
        Assert.IsTrue(clip.Loop);
        CollectionAssert.AreEqual(new[] { "drone0", "drone1", "drone2", "drone3" }, clip.Frames.ToArray());
    }

    [TestMethod]
    public void Trailing_transparent_cells_are_dropped_unless_a_count_is_given_and_a_strip_without_a_rate_is_no_clip()
    {
        var image = Grid(4, 2, 8, opaqueCells: 6);
        var sheet = Parse("""{ "sheets": [ { "image": "a.png", "name": "walk", "cell": [8, 8] } ] }""", image);
        Assert.AreEqual(6, sheet.Frames.Count, "the two empty cells at the end are not frames");
        Assert.AreEqual(0, sheet.Clips.Count, "no rate, no clip: the frames are still there by name");
        var counted = Parse("""{ "sheets": [ { "image": "a.png", "name": "walk", "cell": [8, 8], "count": 7 } ] }""", image);
        Assert.AreEqual(7, counted.Frames.Count);
    }

    [TestMethod]
    public void Margin_and_spacing_place_the_cells()
    {
        var sheet = Parse("""{ "sheets": [ { "image": "a.png", "name": "t", "cell": [8, 8], "margin": 2, "spacing": 1 } ] }""", Grid(3, 2, 8, margin: 2, spacing: 1));
        Assert.AreEqual(6, sheet.Frames.Count);
        var last = sheet.Frames[5].Image;
        Assert.AreEqual(new Color(20, 10, 200), last[0, 0]);
        Assert.AreEqual(new Color(20, 10, 200), last[7, 7], "the cell holds no pixel of the gap");
    }

    [TestMethod]
    public void Strips_and_sprites_name_parts_of_one_image_by_cell_or_by_pixels()
    {
        const string json = """
            {
              "pivot": [0.5, 1],
              "sheets": [ {
                "image": "tiles.png", "cell": [8, 8],
                "strips": {
                  "top": { "at": [1, 1], "count": 2 },
                  "run": { "from": 0, "count": 3, "durations": [100, 200, 300], "direction": "pingpong", "loop": false },
                  "fill": { "cells": [[3, 0], [3, 0], [0, 1]] },
                  "odd": { "rects": [[4, 4, 8, 8]], "pivot": [0, 0] }
                },
                "sprites": { "gem": [2, 1], "logo": [0, 0, 16, 8], "key": { "cell": [3, 1], "origin": [1, 2] } }
              } ]
            }
            """;
        var sheet = Parse(json, Grid(4, 2, 8));
        var byName = sheet.Frames.ToDictionary(f => f.Name);
        Assert.AreEqual(new Color(10, 10, 200), byName["top0"].Image[0, 0]);
        Assert.AreEqual(new Color(20, 10, 200), byName["top1"].Image[0, 0]);
        Assert.AreEqual(new Vector2(4, 8), byName["top0"].Origin, "the file's pivot, as a fraction of the frame");
        Assert.AreEqual(new Color(30, 0, 200), byName["fill1"].Image[0, 0], "a cell may repeat");
        Assert.AreEqual(new Color(0, 10, 200), byName["fill2"].Image[0, 0]);
        Assert.AreEqual(Vector2.Zero, byName["odd0"].Origin, "a strip's own pivot wins");
        Assert.AreEqual(new Color(0, 0, 200), byName["odd0"].Image[0, 0]);
        Assert.AreEqual(new Color(10, 10, 200), byName["odd0"].Image[7, 7]);
        Assert.AreEqual(new Color(20, 10, 200), byName["gem"].Image[0, 0]);
        Assert.AreEqual(16, byName["logo"].Image.Width);
        Assert.AreEqual(new Vector2(1, 2), byName["key"].Origin);
        var run = sheet.Clips.Single();
        CollectionAssert.AreEqual(new[] { 0.1f, 0.2f, 0.3f }, run.Durations!.ToArray());
        Assert.AreEqual(AnimationDirection.PingPong, run.Direction);
        Assert.IsFalse(run.Loop);
    }

    [TestMethod]
    public void An_image_without_cells_is_one_sprite()
    {
        var sheet = Parse("""{ "sheets": [ { "image": "palm.png", "name": "prop_palm", "pivot": [0.5, 1] } ] }""", new PixelImage(10, 20));
        Assert.AreEqual("prop_palm", sheet.Frames.Single().Name);
        Assert.AreEqual(new Vector2(5, 20), sheet.Frames[0].Origin);
    }

    [TestMethod]
    public void Mistakes_say_what_is_wrong_and_where()
    {
        var image = Grid(2, 1, 8);
        StringAssert.Contains(Fails("""{ "sheets": [ { "image": "a.png", "cell": [8, 8], "strips": { "s": { "at": [1, 0], "count": 3 } } } ] }"""), "outside the 16x8 image 'a.png'");
        StringAssert.Contains(Fails("""{ "sheets": [ { "image": "a.png", "cel": [8, 8] } ] }"""), "unknown property \"cel\"");
        StringAssert.Contains(Fails("""{ "sheets": [ { "image": "a.png", "name": "x" }, { "image": "a.png", "name": "x" } ] }"""), "two frames named 'x'");
        StringAssert.Contains(Fails("""{ "sheets": [ { "name": "x" } ] }"""), "\"image\"");
        StringAssert.Contains(Fails("""{ "sheets": [ { "image": "a.png", "name": "x", "cell": [8, 8], "durations": [100] } ] }"""), "2 frames");
        StringAssert.Contains(Fails("""{ "sheets": [ { "image": "a.png", "sprites": { "g": [5, 0] } } ] }"""), "\"cell\"");
        StringAssert.Contains(Fails("not json"), "Invalid JSON");
        var missing = Assert.ThrowsExactly<InvalidDataException>(() => SpriteSheet.Parse("""{ "sheets": [ { "image": "gone.png", "name": "x" } ] }""", _ => throw new FileNotFoundException("no such file")));
        StringAssert.Contains(missing.Message, "gone.png");

        string Fails(string json) => Assert.ThrowsExactly<InvalidDataException>(() => Parse(json, image)).Message;
    }

    [TestMethod]
    public void A_builder_takes_a_sheet_and_the_atlas_plays_its_clips_as_the_data_says()
    {
        const string json = """
            { "sheets": [
              { "image": "a.png", "name": "spin", "cell": [8, 8], "count": 3, "fps": 10 },
              { "image": "a.png", "cell": [8, 8], "strips": { "bounce": { "from": 0, "count": 3, "durations": [100, 200, 300], "direction": "pingpong" },
                                                              "die": { "from": 1, "count": 2, "fps": 4, "loop": false } } }
            ] }
            """;
        var device = new HeadlessGraphicsDevice();
        using var atlas = new SpriteAtlasBuilder().Add("extra", new PixelImage(4, 4)).AddSheet(Parse(json, Grid(4, 1, 8))).Build(device);
        CollectionAssert.AreEqual(new[] { "spin", "bounce", "die" }, atlas.Animations.ToArray(), "in the file's order");
        var spin = atlas.Clip("spin");
        Assert.AreEqual(10f, spin.FramesPerSecond);
        Assert.AreSame(atlas["spin1"], spin.FrameAt(0.1f));
        Assert.AreSame(spin, atlas.Clip("spin"), "built once");
        var bounce = atlas.Clip("bounce");
        CollectionAssert.AreEqual(new[] { atlas["bounce0"], atlas["bounce1"], atlas["bounce2"], atlas["bounce1"] }, bounce.Frames.ToArray());
        CollectionAssert.AreEqual(new[] { 0.1f, 0.2f, 0.3f, 0.2f }, bounce.FrameDurations.ToArray());
        Assert.IsFalse(atlas.Clip("die").Loop, "the data says it plays once");
        Assert.IsTrue(atlas.Clip("die", loop: true).Loop, "the caller may still decide");
        Assert.IsFalse(atlas.Clip("spin", loop: false).Loop);
        Assert.IsTrue(atlas.Contains("extra"));
    }

    [TestMethod]
    public void An_animator_is_made_from_an_atlas_in_one_call()
    {
        var device = new HeadlessGraphicsDevice();
        var sheet = Parse("""
            { "sheets": [ { "image": "a.png", "cell": [8, 8], "strips": { "idle": { "from": 0, "count": 2, "fps": 2 }, "run": { "from": 2, "count": 2, "fps": 10 } } } ] }
            """, Grid(4, 1, 8));
        using var atlas = new SpriteAtlasBuilder().AddSheet(sheet).Build(device);
        var animator = atlas.CreateAnimator();
        Assert.AreEqual("idle", animator.CurrentName, "the first clip of the data plays");
        animator.Play("run");
        animator.Update(0.1f);
        Assert.AreSame(atlas["run1"], animator.Frame);
        Assert.AreEqual("run", atlas.CreateAnimator("run").CurrentName);
        using var plain = new SpriteAtlasBuilder().Add("a", new PixelImage(2, 2)).Build(device);
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => plain.CreateAnimator());
        StringAssert.Contains(error.Message, "no animation");
    }

    [TestMethod]
    public void Assets_load_a_sheet_file_as_an_atlas_and_as_frames_to_pack_with_others()
    {
        string root = Directory.CreateTempSubdirectory("me-sheet-").FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "art"));
            File.WriteAllBytes(Path.Combine(root, "art", "drone.png"), Png(Grid(4, 1, 8)));
            File.WriteAllText(Path.Combine(root, "art", "enemies.sheet"), """{ "sheets": [ { "image": "drone.png", "name": "drone", "cell": [8, 8], "fps": 12 } ] }""");
            File.WriteAllText(Path.Combine(root, "art", "broken.sheet"), """{ "sheets": [ { "image": "none.png", "name": "x" } ] }""");
            var device = new HeadlessGraphicsDevice();
            using var assets = new AssetCache(device, root);
            var atlas = assets.Atlas("art/enemies.sheet", new AtlasOptions(Pivot: new Vector2(0.5f, 1)));
            Assert.AreSame(atlas, assets.Atlas("art/enemies.sheet", new AtlasOptions(Pivot: new Vector2(0.5f, 1))), "cached like any atlas");
            Assert.AreEqual(4, atlas.Frames("drone").Count);
            Assert.AreEqual(new Vector2(4, 8), atlas["drone0"].Origin, "the caller's pivot applies when the file gives none");
            Assert.AreEqual(12f, atlas.Clip("drone").FramesPerSecond);
            Assert.AreEqual(1, device.TexturesCreated);

            var sheet = assets.Sheet("art/enemies.sheet");
            Assert.AreNotSame(sheet, assets.Sheet("art/enemies.sheet"), "frames to change and pack are the caller's: not cached");
            using var mixed = new SpriteAtlasBuilder().AddSheet(sheet).Add("more", new PixelImage(3, 3)).Build(device);
            Assert.IsTrue(mixed.Contains("drone3") && mixed.Contains("more"));

            var error = Assert.ThrowsExactly<AssetLoadException>(() => assets.Atlas("art/broken.sheet"));
            Assert.AreEqual("art/broken.sheet", error.AssetPath);
            StringAssert.Contains(error.Message, "art/none.png");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [TestMethod]
    public void An_aseprite_json_tag_that_repeats_once_does_not_loop()
    {
        const string json = """
            {"frames": [
              {"filename": "a 0.aseprite", "frame": {"x":0,"y":0,"w":8,"h":8}, "duration": 100},
              {"filename": "a 1.aseprite", "frame": {"x":8,"y":0,"w":8,"h":8}, "duration": 100}
            ],
            "meta": {"image": "a.png", "frameTags": [{"name": "die", "from": 0, "to": 1, "direction": "forward", "repeat": "1"},
                                                    {"name": "idle", "from": 0, "to": 1, "direction": "forward"}]}}
            """;
        var device = new HeadlessGraphicsDevice();
        using var atlas = SpriteAtlas.Create(device.CreateTexture(new PixelImage(16, 8)), AtlasData.Parse(json));
        Assert.IsFalse(atlas.Clip("die").Loop);
        Assert.IsTrue(atlas.Clip("idle").Loop, "a tag without a repeat count loops, as before");
    }

    [TestMethod]
    public void Clips_name_frames_from_anywhere_in_the_file_or_a_whole_strip()
    {
        const string json = """
            {
              "sheets": [ { "image": "hero.png", "name": "hero", "cell": [8, 8] } ],
              "clips": {
                "hop": { "frames": ["hero1", "hero3", "hero1"], "fps": 6, "loop": false },
                "walk": { "strip": "hero", "durations": [100, 100, 200, 100] }
              }
            }
            """;
        var sheet = Parse(json, Grid(4, 1, 8));
        CollectionAssert.AreEqual(new[] { "hop", "walk" }, sheet.Clips.Select(c => c.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "hero1", "hero3", "hero1" }, sheet.Clips[0].Frames.ToArray(), "a frame may repeat");
        Assert.IsFalse(sheet.Clips[0].Loop);
        Assert.AreEqual(4, sheet.Clips[1].Frames.Count);
        StringAssert.Contains(Fails("""{ "sheets": [ { "image": "a.png", "name": "x", "cell": [8, 8] } ], "clips": { "c": { "frames": ["x9"], "fps": 4 } } }"""), "names the frame 'x9'");
        StringAssert.Contains(Fails("""{ "sheets": [ { "image": "a.png", "name": "x", "cell": [8, 8] } ], "clips": { "c": { "strip": "y", "fps": 4 } } }"""), "strips: x");
        StringAssert.Contains(Fails("""{ "sheets": [ { "image": "a.png", "name": "x", "cell": [8, 8] } ], "clips": { "c": { "strip": "x" } } }"""), "needs \"fps\" or \"durations\"");

        string Fails(string text) => Assert.ThrowsExactly<InvalidDataException>(() => Parse(text, Grid(4, 1, 8))).Message;
    }

    [TestMethod]
    public void A_packed_atlas_written_to_disk_loads_as_the_same_sprites_and_clips()
    {
        const string json = """
            {
              "sheets": [ { "image": "hero.png", "name": "hero", "cell": [8, 8], "origin": [3, 7], "fps": 8 },
                          { "image": "hero.png", "name": "odd", "cell": [8, 8], "count": 1, "pivot": [0.5, 1] } ],
              "clips": { "hop": { "frames": ["hero1", "hero3", "hero1"], "durations": [100, 250, 100], "loop": false },
                         "back": { "strip": "hero", "fps": 4, "direction": "pingpong_reverse" } }
            }
            """;
        var builder = new SpriteAtlasBuilder().AddSheet(Parse(json, Grid(4, 1, 8))).Add("wide", new PixelImage(55, 9), new Vector2(27, 4.5f));
        var packed = builder.Pack();
        string text = packed.ToJson("pack.png");
        Assert.AreEqual(text, builder.Pack().ToJson("pack.png"), "the same input gives the same text");
        Assert.IsFalse(text.Contains('\r'), "one line ending on every system");
        var data = AtlasData.Parse(text);
        Assert.AreEqual("pack.png", data.ImagePath);

        var device = new HeadlessGraphicsDevice();
        using var direct = builder.Build(device, TextureFilter.Nearest);
        using var loaded = SpriteAtlas.Create(device.CreateTexture(packed.Image, TextureFilter.Nearest), data);
        foreach (var name in direct.Names)
        {
            Assert.AreEqual(direct[name].Source, loaded[name].Source, name);
            Assert.AreEqual(direct[name].Origin, loaded[name].Origin, $"{name}: a whole-pixel anchor survives being written as a fraction");
        }
        CollectionAssert.AreEqual(direct.Animations.ToArray(), loaded.Animations.ToArray());
        foreach (var name in direct.Animations)
        {
            var a = direct.Clip(name);
            var b = loaded.Clip(name);
            Assert.AreEqual(a.Loop, b.Loop, name);
            CollectionAssert.AreEqual(a.Frames.Select(f => f.Source).ToArray(), b.Frames.Select(f => f.Source).ToArray(), $"{name}: the same pixels in the same order");
            Assert.AreEqual(a.Duration, b.Duration, 0.001f, name);
        }
        Assert.IsTrue(loaded.Contains("hop#1"), "a clip a frame tag cannot describe gets entries of its own, on the same pixels");
        Assert.AreEqual(loaded["hero3"].Source, loaded["hop#1"].Source);
    }
}
