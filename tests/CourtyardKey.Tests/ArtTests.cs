using MicroEngine;
using CourtyardKey.Visuals;

namespace CourtyardKey.Tests;

/// <summary>The hybrid art loads completely: every enemy and boss frame exists, and the imported pieces (the heart-trap, the houses, the trees) are repainted in the game's ink and ramps.</summary>
[TestClass]
public sealed class ArtTests
{
    private static readonly string[] EnemyFrames =
    [
        "slime0", "slime1", "slime2", "bat0", "bat1", "bat2", "spitter0", "spitter1", "spitter2",
        "knight_down0", "knight_down1", "knight_up0", "knight_up1", "knight_side0", "knight_side1",
        "boss_idle0", "boss_idle1", "boss_tell", "boss_dazed", "boss_rage0", "boss_rage1",
    ];

    [TestMethod]
    public void EveryEnemyFrameAndItsHitFlashExist()
    {
        using var h = new Harness();
        foreach (var name in EnemyFrames)
        {
            Assert.IsTrue(h.Game.Art.Contains(name), name);
            Assert.IsTrue(h.Game.Art.Contains(name + "!"), name + "!");
        }
    }

    [TestMethod]
    public void MonsterFramesAreRepaintedInTheGameInk()
    {
        var frames = MonsterArt.Frames(new MicroEngine.Core.ContentFolder().Image).ToList();
        CollectionAssert.AreEquivalent(new[] { "spitter0", "spitter1", "spitter2" }, frames.Select(f => f.Name).ToArray());
        foreach (var (name, img, _) in frames)
        {
            Assert.IsFalse(Contains(img, Color.Hex("141b1b")), $"{name} still uses the pack's outline colour");
            Assert.IsTrue(Contains(img, Pal.Ink), $"{name} should be outlined in the game's ink");
        }
    }

    [TestMethod]
    public void PackHousesAndTreesAreRepaintedInTheWorldRamps()
    {
        var pieces = PackArt.Pieces(new MicroEngine.Core.ContentFolder().Image).ToList();
        CollectionAssert.AreEquivalent(new[] { "tree0", "tree1", "tree2", "house_home", "house_shop" }, pieces.Select(p => p.Name).ToArray());
        foreach (var (name, img, _) in pieces)
        {
            // The pack's own outlines and wood are gone: the pieces stand in the game's light.
            foreach (var packColour in new[] { "201729", "2b2f35", "563a3f", "79584f", "2eca5c", "299750" })
                Assert.IsFalse(Contains(img, Color.Hex(packColour)), $"{name} still uses the pack's #{packColour}");
            var ramp = name.StartsWith("tree", StringComparison.Ordinal) ? Pal.Foliage : Pal.Timber;
            Assert.IsTrue(ramp.Any(c => Contains(img, c)), $"{name} should be painted in the world ramps");
        }
    }

    [TestMethod]
    public void EveryTileOfTheGroundAndTheDungeonHasItsSprite()
    {
        using var h = new Harness();
        for (int i = 0; i < GroundArt.Period * GroundArt.Period; i++)
        {
            Assert.IsTrue(h.Game.Art.Contains($"ground{i}"), $"ground{i}");
            Assert.IsTrue(h.Game.Art.Contains($"flagstone{i}"), $"flagstone{i}");
        }
        foreach (var name in new[] { "tree0", "tree1", "tree2", "house_home", "house_shop", "shade_n", "shade_w", "wall_corner" })
            Assert.IsTrue(h.Game.Art.Contains(name), name);
    }

    private static bool Contains(MicroEngine.Graphics.PixelImage img, Color c)
    {
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
                if (img[x, y] == c) return true;
        return false;
    }
}
