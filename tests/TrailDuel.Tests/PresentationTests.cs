using TrailDuel.Rules;
using TrailDuel.Visuals;
using TrailDuel.World;

namespace TrailDuel.Tests;

/// <summary>The hybrid art loads completely: shipped effect sheets, painted stages and battle portraits.</summary>
[TestClass]
public sealed class PresentationTests
{
    [TestMethod]
    public void EveryElementHasALoadedImpactAnimation()
    {
        using var h = new Harness();
        foreach (var type in Enum.GetValues<ElementType>())
            foreach (bool big in new[] { false, true })
            {
                var frames = h.Game.Atlas.Frames("fx:" + FxArt.Impact(type, big));
                Assert.IsTrue(frames.Count >= 4, $"{type} (big={big}) should animate, got {frames.Count} frames.");
            }
    }

    [TestMethod]
    public void EveryBackdropHasAStageAndPlatforms()
    {
        using var h = new Harness();
        foreach (var bd in Enum.GetValues<Backdrop>())
            Assert.AreEqual(BattleStageArt.Width, h.Game.Atlas["backdrop:" + bd].Width, $"backdrop:{bd}");
        foreach (var suffix in new[] { "", "forest", "hall", "indoor" })
        {
            Assert.IsNotNull(h.Game.Atlas["platform:enemy" + suffix]);
            Assert.IsNotNull(h.Game.Atlas["platform:player" + suffix]);
        }
    }

    [TestMethod]
    public void EveryMusicNameTheGameUsesIsRegisteredFromAFile()
    {
        using var h = new Harness();
        var used = Maps.Every.Select(m => m.Music)
            .Concat(["title", "battle", "trainer", "master", "victory", "evolution", "credits"])
            .Distinct();
        foreach (var name in used) Assert.IsTrue(h.Game.Audio.HasMusic(name), name);
        foreach (var jingle in Sound.MusicBank.Jingles) Assert.IsTrue(h.Game.Audio.HasSound("jingle:" + jingle), jingle);
    }

    [TestMethod]
    public void TrainersHaveDoubleSizeBattlePortraits()
    {
        using var h = new Harness();
        foreach (var look in CharacterLook.All)
        {
            var portrait = h.Game.Atlas[CharacterArt.PortraitKey(look.Key, Direction.Up)];
            Assert.AreEqual((CharacterArt.W * 2, CharacterArt.H * 2), (portrait.Width, (int)portrait.Source.Height), look.Key);
        }
    }
}
