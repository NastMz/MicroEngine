using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;

namespace MicroEngine.Tests;

/// <summary>
/// Data on a sheet's frames beside the anchor: named points (a shoulder, a gun tip) and named boxes (a hit box), read
/// from the <c>.sheet</c> file, kept through packing and trimming, mirrored with the sprite.
/// </summary>
[TestClass]
public sealed class SpriteMarkTests
{
    private static PixelImage Strip(int cells, int w = 8, int h = 6)
    {
        var image = new PixelImage(cells * w, h);
        for (int i = 0; i < cells; i++) image.FillRect(i * w + 2, 1, 4, 4, new Color((byte)(40 * i + 40), 0, 0));
        return image;
    }

    private const string Sheet = """
        {
          "sheets": [
            { "image": "hero.png", "name": "hero", "cell": [8, 6], "origin": [4, 6], "fps": 10,
              "points": { "hand": [7, 2], "eye": [[5, 1], [5.5, 1], null] },
              "boxes": { "hurt": [2, 1, 4, 5], "hit": [null, [6, 0, 2, 3], null] } },
            { "image": "props.png", "cell": [8, 6],
              "strips": { "flag": { "from": 0, "count": 2, "points": { "top": [[1, 0], [2, 0]] }, "boxes": { "cloth": [1, 0, 6, 3] } } },
              "sprites": { "crate": { "cell": [2, 0], "pivot": [0.5, 1], "points": { "lid": [4, 1] }, "boxes": { "solid": [2, 1, 4, 4] } },
                           "plain": [0, 0] } }
          ]
        }
        """;

    private static SpriteSheet Parse(string text = Sheet) => SpriteSheet.Parse(text, name => Strip(3));

    [TestMethod]
    public void A_sheet_gives_its_frames_points_and_boxes_for_every_frame_or_one_by_one()
    {
        var sheet = Parse();
        var hero1 = sheet.Frames.First(f => f.Name == "hero1");
        CollectionAssert.AreEqual(new[] { new SpritePoint("hand", new Vector2(7, 2)), new SpritePoint("eye", new Vector2(5.5f, 1)) }, hero1.Points.ToArray());
        CollectionAssert.AreEqual(new[] { new SpriteBox("hurt", new RectF(2, 1, 4, 5)), new SpriteBox("hit", new RectF(6, 0, 2, 3)) }, hero1.Boxes.ToArray());
        var hero2 = sheet.Frames.First(f => f.Name == "hero2");
        CollectionAssert.AreEqual(new[] { "hand" }, hero2.Points.Select(p => p.Name).ToArray(), "null leaves a frame without the point");
        CollectionAssert.AreEqual(new[] { "hurt" }, hero2.Boxes.Select(b => b.Name).ToArray());
        Assert.AreEqual(new Vector2(2, 0), sheet.Frames.First(f => f.Name == "flag1").Points.Single().Position);
        Assert.AreEqual(new RectF(1, 0, 6, 3), sheet.Frames.First(f => f.Name == "flag0").Boxes.Single().Bounds);
        Assert.AreEqual("lid", sheet.Frames.First(f => f.Name == "crate").Points.Single().Name);
        Assert.AreEqual(0, sheet.Frames.First(f => f.Name == "plain").Points.Count);
        Assert.AreEqual(0, sheet.Frames.First(f => f.Name == "plain").Boxes.Count);
    }

    [TestMethod]
    public void A_sprite_answers_from_its_anchor_and_mirrors_with_the_flip()
    {
        using var atlas = new SpriteAtlasBuilder().AddSheet(Parse()).Build(new HeadlessGraphicsDevice());
        var hero = atlas["hero1"];                                  // anchored at (4, 6)
        Assert.AreEqual(new Vector2(3, -4), hero.Point("hand"), "(7, 2) seen from the anchor");
        Assert.AreEqual(new Vector2(-3, -4), hero.Point("hand", flipX: true), "mirrored around the anchor, as the sprite is drawn");
        Assert.AreEqual(new RectF(-2, -5, 4, 5), hero.Box("hurt"));
        Assert.AreEqual(new RectF(2, -6, 2, 3), hero.Box("hit"));
        Assert.AreEqual(new RectF(-4, -6, 2, 3), hero.Box("hit", flipX: true), "a box keeps its size and changes side");
        Assert.IsTrue(hero.TryPoint("eye", out var eye));
        Assert.AreEqual(new Vector2(1.5f, -5), eye);
        Assert.IsFalse(atlas["hero2"].TryPoint("eye", out _));
        Assert.IsFalse(hero.TryBox("nothing", out _));
        var missing = Assert.ThrowsExactly<KeyNotFoundException>(() => atlas["hero2"].Point("eye"));
        StringAssert.Contains(missing.Message, "eye");
        StringAssert.Contains(missing.Message, "hand", "the message lists the points the sprite has");
        Assert.ThrowsExactly<KeyNotFoundException>(() => hero.Box("lid"));
        var crate = atlas["crate"];                                 // pivot (0.5, 1) of 8x6: (4, 6)
        Assert.AreEqual(new Vector2(0, -5), crate.Point("lid"));
        Assert.AreEqual(1, crate.Points.Count);
        Assert.AreEqual(1, crate.Boxes.Count);
        Assert.AreEqual(0, atlas["plain"].Points.Count);

        // Where the point lies on screen is where the pixel it marks is drawn, both ways round.
        var at = new Vector2(100, 50);
        Assert.AreEqual(new Vector2(103, 46), at + hero.Point("hand"));
        Assert.AreEqual(new Vector2(97, 46), at + hero.Point("hand", flipX: true));
        AllocationAssertNone(() => _ = hero.Point("hand", true).X + hero.Box("hit").X + (hero.TryPoint("eye", out var p) ? p.X : 0));
    }

    private static void AllocationAssertNone(Action action) => MicroEngine.Testing.AllocationAssert.None(action, because: "points and boxes read every frame");

    [TestMethod]
    public void Points_and_boxes_survive_trimming_packing_to_a_file_and_loading_it_again()
    {
        var builder = new SpriteAtlasBuilder().AddSheet(Parse());
        var packed = builder.Pack(trim: true);
        var data = AtlasData.Parse(packed.ToJson("atlas.png"));
        var device = new HeadlessGraphicsDevice();
        using var whole = builder.Build(device);
        using var again = SpriteAtlas.Create(device.CreateTexture(packed.Image), data);
        foreach (var name in new[] { "hero0", "hero1", "hero2", "flag0", "flag1", "crate", "plain" })
        {
            var a = whole[name];
            var b = again[name];
            Assert.AreEqual(a.Points.Count, b.Points.Count, name);
            Assert.AreEqual(a.Boxes.Count, b.Boxes.Count, name);
            foreach (var point in a.Points) Assert.AreEqual(a.Point(point.Name), b.Point(point.Name), $"{name}.{point.Name}: the same place from the anchor, trimmed or not");
            foreach (var box in a.Boxes) Assert.AreEqual(a.Box(box.Name), b.Box(box.Name), $"{name}.{box.Name}");
        }
        Assert.IsTrue(again["hero1"].Width < whole["hero1"].Width, "the packed frames were trimmed");
        // The clip of the sheet keeps the frames' data.
        Assert.AreEqual(new Vector2(3, -4), again.Clip("hero").FrameAt(0.1f).Point("hand"));
    }

    [TestMethod]
    public void A_frame_added_in_code_carries_its_points_and_boxes()
    {
        var frame = new SheetFrame("gun", new PixelImage(10, 4), new Vector2(1, 2))
        {
            Points = [new SpritePoint("muzzle", new Vector2(10, 2))],
            Boxes = [new SpriteBox("grip", new RectF(0, 1, 3, 3))],
        };
        using var atlas = new SpriteAtlasBuilder().Add(frame).Build(new HeadlessGraphicsDevice());
        Assert.AreEqual(new Vector2(9, 0), atlas["gun"].Point("muzzle"));
        Assert.AreEqual(new RectF(-1, -1, 3, 3), atlas["gun"].Box("grip"));
    }

    [TestMethod]
    public void A_sheet_without_points_or_boxes_reads_as_before()
    {
        var sheet = SpriteSheet.Parse("""{ "sheets": [ { "image": "a.png", "name": "a", "cell": [8, 6], "origin": [4, 6], "fps": 10 } ] }""", _ => Strip(3));
        Assert.AreEqual(3, sheet.Frames.Count);
        Assert.IsTrue(sheet.Frames.All(f => f.Points.Count == 0 && f.Boxes.Count == 0));
        Assert.AreEqual(new Vector2(4, 6), sheet.Frames[0].Origin);
    }

    [TestMethod]
    [DataRow("""{ "sheets": [ { "image": "a.png", "name": "a", "cell": [8, 6], "points": { "p": [1] } } ] }""", "\"p\"")]
    [DataRow("""{ "sheets": [ { "image": "a.png", "name": "a", "cell": [8, 6], "points": { "p": [[1, 2], [3, 4]] } } ] }""", "3 frames")]
    [DataRow("""{ "sheets": [ { "image": "a.png", "name": "a", "cell": [8, 6], "boxes": { "b": [1, 2, 0, 4] } } ] }""", "positive")]
    [DataRow("""{ "sheets": [ { "image": "a.png", "name": "a", "cell": [8, 6], "boxes": { "b": [1, 2, 3] } } ] }""", "\"b\"")]
    [DataRow("""{ "sheets": [ { "image": "a.png", "name": "a", "cell": [8, 6], "points": [1, 2] } ] }""", "\"points\"")]
    [DataRow("""{ "sheets": [ { "image": "a.png", "cell": [8, 6], "points": { "p": [1, 2] }, "sprites": { "s": [0, 0] } } ] }""", "\"name\"")]
    [DataRow("""{ "sheets": [ { "image": "a.png", "name": "a", "cell": [8, 6], "points": { "p": ["x", 2] } } ] }""", "number")]
    public void Mistakes_in_points_and_boxes_say_what_is_wrong(string text, string expected)
    {
        var error = Assert.ThrowsExactly<InvalidDataException>(() => SpriteSheet.Parse(text, _ => Strip(3)));
        StringAssert.Contains(error.Message, expected);
    }

    [TestMethod]
    public void An_aseprite_sheet_export_gives_its_slices_as_boxes_and_their_pivots_as_points()
    {
        const string json = """
            { "frames": [
                { "filename": "hero 0.aseprite", "frame": { "x": 0, "y": 0, "w": 8, "h": 6 }, "spriteSourceSize": { "x": 0, "y": 0, "w": 8, "h": 6 }, "sourceSize": { "w": 8, "h": 6 }, "duration": 100 },
                { "filename": "hero 1.aseprite", "frame": { "x": 8, "y": 0, "w": 6, "h": 5 }, "spriteSourceSize": { "x": 2, "y": 1, "w": 6, "h": 5 }, "sourceSize": { "w": 8, "h": 6 }, "duration": 100 },
                { "filename": "hero 2.aseprite", "frame": { "x": 16, "y": 0, "w": 8, "h": 6 }, "spriteSourceSize": { "x": 0, "y": 0, "w": 8, "h": 6 }, "sourceSize": { "w": 8, "h": 6 }, "duration": 100 } ],
              "meta": { "image": "hero.png",
                "slices": [
                  { "name": "hurt", "color": "#0000ffff", "keys": [ { "frame": 0, "bounds": { "x": 2, "y": 1, "w": 4, "h": 5 } } ] },
                  { "name": "gun", "keys": [ { "frame": 1, "bounds": { "x": 5, "y": 2, "w": 3, "h": 2 }, "pivot": { "x": 3, "y": 1 } },
                                             { "frame": 2, "bounds": { "x": 4, "y": 2, "w": 3, "h": 2 }, "pivot": { "x": 3, "y": 1 } } ] } ] } }
            """;
        var data = AtlasData.Parse(json);
        Assert.AreEqual(new RectF(2, 1, 4, 5), data.Regions[0].Boxes!.Single(b => b.Name == "hurt").Bounds);
        Assert.IsFalse(data.Regions[0].Boxes!.Any(b => b.Name == "gun"), "a slice starts at its first key");
        Assert.AreEqual(new RectF(0, 0, 4, 5), data.Regions[1].Boxes!.Single(b => b.Name == "hurt").Bounds, "a key holds for later frames; a trimmed frame counts from its own corner");
        Assert.AreEqual(new RectF(3, 1, 3, 2), data.Regions[1].Boxes!.Single(b => b.Name == "gun").Bounds);
        Assert.AreEqual(new Vector2(6, 2), data.Regions[1].Points!.Single(p => p.Name == "gun").Position, "the slice's corner plus its pivot, in the trimmed frame");
        Assert.AreEqual(new Vector2(7, 3), data.Regions[2].Points!.Single(p => p.Name == "gun").Position);
        Assert.IsFalse(data.Regions[2].Points!.Any(p => p.Name == "hurt"), "a slice without a pivot is a box only");
    }

    private static string? RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "MicroEngine.slnx"))) return dir.FullName;
        return null;
    }

    /// <summary>Neon Rift's scan of a gun pose (Art/Assets.cs, AddRig): the gun tip and the shoulder, in the frame's pixels.</summary>
    private static (Vector2 Shoulder, Vector2 Tip) Scan(PixelImage frame)
    {
        int tipX = -1, tipY = 0;
        for (int y = 18; y < 40; y++)
            for (int x = frame.Width - 1; x > tipX; x--)
                if (frame[x, y].A > 0) { tipX = x; tipY = y; break; }
        int shoulder = tipX;
        for (int x = tipX; x > 20; x--)
        {
            int run = 0;
            for (int y = tipY - 10; y <= tipY + 10; y++) if (frame[x, y].A > 0) run++;
            if (run > 9) { shoulder = x + 1; break; }
        }
        return (new Vector2(shoulder, tipY), new Vector2(tipX, tipY));
    }

    // What an artist would write beside Neon Rift's gun poses: the shoulder and the gun tip of the standing shot and of
    // every frame of the running shot, in the frame's pixels.
    private const string GunPoses = """
        {
          "sheets": [
            { "image": "hero/shoot.png", "name": "hero_shoot", "cell": [71, 67], "origin": [31, 66],
              "points": { "shoulder": [36, 24], "tip": [54, 24] } },
            { "image": "hero/run-shoot.png", "name": "hero_run-shoot", "cell": [71, 67], "origin": [31, 66], "fps": 12,
              "points": { "shoulder": [[50, 26], [50, 27], [50, 29], [49, 29], [48, 28], [48, 28], [48, 29], [50, 29]], "tip": [[60, 26], [61, 27], [62, 29], [62, 29], [62, 28], [62, 28], [63, 29], [62, 29]] } }
          ]
        }
        """;

    [TestMethod]
    public void The_shoulder_and_gun_tip_of_a_run_and_gun_hero_come_from_the_sheet_where_the_game_scanned_pixels()
    {
        if (RepositoryRoot() is not { } root) { Assert.Inconclusive("the repository's games are not beside this build"); return; }
        string content = Path.Combine(root, "games", "NeonRift", "Content");
        var sheet = SpriteSheet.Parse(GunPoses, name => PixelImage.Load(Path.Combine(content, name)));
        using var atlas = new SpriteAtlasBuilder().AddSheet(sheet).Build(new HeadlessGraphicsDevice());
        int poses = 0;
        foreach (var frame in sheet.Frames)
        {
            if (frame.Name != "hero_shoot0" && !frame.Name.StartsWith("hero_run-shoot", StringComparison.Ordinal)) continue;
            poses++;
            var (shoulder, tip) = Scan(frame.Image);
            var sprite = atlas[frame.Name];
            // The game keeps the shoulder from the anchor and the arm's length: Assets.AddRig, _pendingRigs.
            Assert.AreEqual(shoulder - frame.Origin, sprite.Point("shoulder"), $"{frame.Name}: shoulder");
            Assert.AreEqual(tip.X - shoulder.X, sprite.Point("tip").X - sprite.Point("shoulder").X, $"{frame.Name}: arm length");
            Assert.AreEqual(tip - frame.Origin, sprite.Point("tip"), $"{frame.Name}: gun tip");
        }
        Assert.IsTrue(poses >= 5, $"the standing shot and the frames of the running shot: {poses}");
    }
}
