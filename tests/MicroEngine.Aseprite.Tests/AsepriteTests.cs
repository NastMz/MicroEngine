using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Aseprite.Tests;

/// <summary>
/// Aseprite files read as the artist saved them (the optional <c>MicroEngine.Aseprite</c> package): frames flattened,
/// tags as clips, slices as sprites and pivots, through the game's assets. The files are written by
/// <see cref="AsepriteFileWriter"/> from the documented format.
/// </summary>
[TestClass]
public sealed class AsepriteTests
{
    private static readonly Color Red = new(255, 0, 0), Blue = new(0, 0, 255), Green = new(0, 255, 0);

    private static PixelImage Fill(int w, int h, Color color) => new PixelImage(w, h).Clear(color);

    /// <summary>An 8x8 file of four frames: a red body, a blue 2x2 mark that moves one pixel per frame, and a hidden green layer.</summary>
    private static AsepriteFileWriter Hero()
    {
        var file = new AsepriteFileWriter(8, 8).Layer("body").Layer("mark").Layer("guide", visible: false);
        int[] ms = [100, 200, 300, 400];
        for (int i = 0; i < 4; i++)
            file.Frame(ms[i], (0, 0, 0, Fill(8, 8, Red)), (1, i, 1, Fill(2, 2, Blue)), (2, 0, 0, Fill(8, 8, Green)));
        return file;
    }

    [TestMethod]
    public void Frames_are_the_visible_layers_flattened_and_named_like_aseprites_own_export()
    {
        var sheet = AsepriteAssets.Read("hero", Hero().ToBytes());
        CollectionAssert.AreEqual(new[] { "hero 0", "hero 1", "hero 2", "hero 3" }, sheet.Frames.Select(f => f.Name).ToArray());
        var third = sheet.Frames[2].Image;
        Assert.AreEqual(8, third.Width);
        Assert.AreEqual(Blue, third[2, 1], "the upper layer's cel, placed where the frame puts it");
        Assert.AreEqual(Blue, third[3, 2]);
        Assert.AreEqual(Red, third[1, 1], "the layer below shows around it");
        Assert.AreEqual(Red, third[7, 7], "the hidden layer is not drawn");
        Assert.AreEqual(new Vector2(4, 8), AsepriteAssets.Read("hero", Hero().ToBytes(), new AtlasOptions(Pivot: new Vector2(0.5f, 1))).Frames[0].Origin,
            "without a slice that has a pivot, the caller's pivot anchors the frames");
        Assert.AreEqual(0, sheet.Clips.Count);
    }

    [TestMethod]
    public void Tags_are_clips_with_each_frames_duration_direction_and_repeat()
    {
        var bytes = Hero().Tag("idle", 0, 1).Tag("spin", 1, 3, direction: 2).Tag("die", 2, 3, direction: 1, repeat: 1).ToBytes();
        var device = new HeadlessGraphicsDevice();
        using var atlas = new SpriteAtlasBuilder().AddSheet(AsepriteAssets.Read("hero", bytes)).Build(device);
        CollectionAssert.AreEqual(new[] { "idle", "spin", "die" }, atlas.Animations.ToArray());
        var idle = atlas.Clip("idle");
        CollectionAssert.AreEqual(new[] { 0.1f, 0.2f }, idle.FrameDurations.ToArray());
        Assert.IsTrue(idle.Loop);
        Assert.AreSame(atlas["hero 1"], idle.FrameAt(0.15f));
        var spin = atlas.Clip("spin");
        CollectionAssert.AreEqual(new[] { atlas["hero 1"], atlas["hero 2"], atlas["hero 3"], atlas["hero 2"] }, spin.Frames.ToArray(), "ping-pong");
        CollectionAssert.AreEqual(new[] { 0.2f, 0.3f, 0.4f, 0.3f }, spin.FrameDurations.ToArray());
        var die = atlas.Clip("die");
        CollectionAssert.AreEqual(new[] { atlas["hero 3"], atlas["hero 2"] }, die.Frames.ToArray(), "reverse");
        Assert.IsFalse(die.Loop, "a tag that repeats once plays once");
        Assert.AreEqual("idle", atlas.CreateAnimator().CurrentName);
    }

    [TestMethod]
    public void Slices_are_boxes_of_the_frames_they_cover_and_their_pivots_points()
    {
        // "body" anchors the frames at (4, 8); "hit" exists from frame 1 on.
        var bytes = Hero().Slice("body", 0, 1, 0, 6, 8, pivot: (3, 8))
            .Slice("gun", 0, 5, 2, 3, 2, pivot: (3, 1))
            .Slice("hit", 1, 6, 0, 2, 4).ToBytes();
        var sheet = AsepriteAssets.Read("hero", bytes);
        var f0 = sheet.Frames[0];
        var f1 = sheet.Frames[1];
        var f2 = sheet.Frames[2];
        Assert.AreEqual(new RectF(5, 2, 3, 2), f0.Boxes.Single(b => b.Name == "gun").Bounds);
        Assert.AreEqual(new Vector2(8, 3), f0.Points.Single(p => p.Name == "gun").Position, "the slice's corner plus its pivot");
        Assert.AreEqual(new Vector2(8, 3), f1.Points.Single(p => p.Name == "gun").Position, "a key holds until the next one");
        Assert.AreEqual(new Vector2(8, 3), f2.Points.Single(p => p.Name == "gun").Position);
        Assert.AreEqual(new RectF(5, 2, 3, 2), f2.Boxes.Single(b => b.Name == "gun").Bounds);
        Assert.IsFalse(f0.Boxes.Any(b => b.Name == "hit"), "a slice starts at its first key");
        Assert.AreEqual(new RectF(6, 0, 2, 4), f1.Boxes.Single(b => b.Name == "hit").Bounds);
        Assert.IsFalse(f1.Points.Any(p => p.Name == "hit"), "a slice without a pivot is a box only");

        using var atlas = new SpriteAtlasBuilder().AddSheet(sheet).Build(new MicroEngine.Core.HeadlessGraphicsDevice());
        var sprite = atlas["hero 0"];
        Assert.AreEqual(new Vector2(4, -5), sprite.Point("gun"), "from the frame's anchor (4, 8)");
        Assert.AreEqual(new Vector2(-4, -5), sprite.Point("gun", flipX: true));
        Assert.AreEqual(new RectF(1, -6, 3, 2), sprite.Box("gun"));
    }

    [TestMethod]
    public void A_slice_with_a_pivot_anchors_the_frames_and_every_slice_is_a_sprite()
    {
        var bytes = Hero().Slice("body", 0, 1, 0, 6, 8, pivot: (3, 8)).Slice("mark", 2, 2, 1, 2, 2).ToBytes();
        var sheet = AsepriteAssets.Read("hero", bytes, new AtlasOptions(Pivot: new Vector2(0.5f, 0.5f)));
        Assert.AreEqual(new Vector2(4, 8), sheet.Frames[0].Origin, "the slice's corner (1, 0) plus its pivot (3, 8), on the canvas");
        Assert.AreEqual(new Vector2(4, 8), sheet.Frames[3].Origin, "a key holds until the next one");
        var byName = sheet.Frames.ToDictionary(f => f.Name);
        Assert.AreEqual(6, byName["body"].Image.Width);
        Assert.AreEqual(new Vector2(3, 8), byName["body"].Origin, "a slice's sprite is anchored at its own pivot");
        Assert.AreEqual(Blue, byName["mark"].Image[0, 0], "cut from the frame its key names");
        Assert.AreEqual(2, byName["mark"].Image.Height);
        Assert.AreEqual(new Vector2(1, 1), byName["mark"].Origin, "a slice without a pivot takes the caller's");
    }

    [TestMethod]
    public void The_assets_load_an_aseprite_file_as_an_atlas_like_any_other()
    {
        string root = Directory.CreateTempSubdirectory("me-aseprite-").FullName;
        try
        {
            File.WriteAllBytes(Path.Combine(root, "hero.aseprite"), Hero().Tag("run", 0, 3).ToBytes());
            File.WriteAllText(Path.Combine(root, "broken.ase"), "this is not an Aseprite file, whatever its name says");
            var device = new HeadlessGraphicsDevice();
            using var assets = new AssetCache(device, root);
            var before = Assert.ThrowsExactly<AssetLoadException>(() => assets.Atlas("hero.aseprite"));
            StringAssert.Contains(before.Message, "MicroEngine.Aseprite", "without the package's loader, the failure says where Aseprite files come from");

            var atlas = assets.Aseprite("hero.aseprite");
            Assert.AreSame(atlas, assets.Atlas("hero.aseprite"), "registered on first use: the file is an atlas like any other");
            Assert.AreEqual(1, device.TexturesCreated);
            Assert.AreEqual(4, atlas.Clip("run").Frames.Count);
            Assert.AreEqual(4, assets.Sheet("hero.aseprite").Frames.Count, "its frames as pixels, to pack with others");
            var animator = atlas.CreateAnimator();
            animator.Update(0.35f);
            Assert.AreSame(atlas["hero 2"], animator.Frame, "100 ms + 200 ms played: the third frame");

            var error = Assert.ThrowsExactly<AssetLoadException>(() => assets.Aseprite("broken.ase"));
            Assert.AreEqual("broken.ase", error.AssetPath);
            StringAssert.Contains(error.Message, "Not a file Aseprite saves");
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
