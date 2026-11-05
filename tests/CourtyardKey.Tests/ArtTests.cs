using MicroEngine;
using CourtyardKey.Visuals;

namespace CourtyardKey.Tests;

/// <summary>The hybrid art loads completely: every enemy and boss frame exists, and the imported heart-trap is repainted in the game's ink.</summary>
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

    private static bool Contains(MicroEngine.Graphics.PixelImage img, Color c)
    {
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
                if (img[x, y] == c) return true;
        return false;
    }
}
