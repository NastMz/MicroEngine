using MicroEngine;
using MicroEngine.Core;
using MicroEngine.Graphics;
using TrailDuel.Visuals;

namespace TrailDuel.Tests;

/// <summary>Evolution lines read as one family: imported sprites are recoloured to match their relatives.</summary>
[TestClass]
public sealed class CreatureArtTests
{
    private static readonly ContentFolder Content = new();

    /// <summary>The visible colours, opaque (some sheets store 254 alpha).</summary>
    private static HashSet<Color> Colours(PixelImage img)
    {
        var set = new HashSet<Color>();
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
                if (img[x, y].A > 0) set.Add(img[x, y] with { A = 255 });
        return set;
    }

    [TestMethod]
    public void BouldoxWearsPebblitsBrownsInsteadOfItsBlueGreenPlates()
    {
        var original = Colours(Content.Image("creatures/bouldox.png"));
        var shown = Colours(CreatureArt.Front(Content.Image, "bouldox"));
        Assert.IsTrue(original.Contains(Color.Hex("345d68")) && original.Contains(Color.Hex("acf2ff")), "The source keeps its own colours.");
        Assert.IsFalse(shown.Contains(Color.Hex("345d68")), "The blue-green plates are gone.");
        Assert.IsFalse(shown.Contains(Color.Hex("8b979d")), "The cool grey hide is gone.");
        int warm = shown.Count(c => c.R > c.B + 15 && c.G > c.B), cool = shown.Count(c => c.B > c.R + 15);
        Assert.IsTrue(warm > 10 && cool == 0, $"Pebblit's browns replace the cool tones ({warm} warm, {cool} cool).");
        Assert.IsTrue(shown.Contains(Color.Hex("365d3c")) && shown.Contains(Color.Black), "Moss and outline stay.");
    }

    [TestMethod]
    public void FizzbitWearsVoltigersCharcoalAndYellow()
    {
        var shown = Colours(CreatureArt.Front(Content.Image, "fizzbit"));
        Assert.IsFalse(shown.Contains(Color.Hex("8038a8")), "No purple quills remain.");
        Assert.IsFalse(shown.Contains(Color.Hex("4ed877")), "No green tips remain.");
        Assert.IsTrue(shown.Contains(Color.Hex("fed800")) && shown.Contains(Color.Hex("504b4d")), "Voltiger's yellow and charcoal are used.");
    }

    [TestMethod]
    public void OtherSpeciesAreShownAsDrawn()
    {
        var original = Content.Image("creatures/pebblit.png");
        var shown = CreatureArt.Front(Content.Image, "pebblit");
        CollectionAssert.AreEquivalent(Colours(original).ToList(), Colours(shown).ToList());
    }
}

/// <summary>The capture orb has its own look: a capped glass globe, not a two-tone ball split by a band.</summary>
[TestClass]
public sealed class OrbArtTests
{
    [TestMethod]
    [DataRow(12, false)]
    [DataRow(12, true)]
    [DataRow(8, false)]
    public void OrbIsACappedGlassGlobeWithoutABand(int size, bool super)
    {
        var orb = UiArt.Orb(size, super);
        int mid = size / 2;
        // No horizontal ink band across the body, and no white lower half.
        int inkAcross = Enumerable.Range(1, size - 2).Count(x => orb[x, mid - 1] == Pal.Ink || orb[x, mid] == Pal.Ink);
        Assert.IsTrue(inkAcross <= 2, $"Only the outline is ink on the middle rows ({inkAcross}).");
        int whiteBelow = 0, body = 0;
        for (int y = mid + 1; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var c = orb[x, y];
                if (c.A == 0 || c == Pal.Ink) continue;
                body++;
                if (c.R > 230 && c.G > 230 && c.B > 220) whiteBelow++;
            }
        Assert.IsTrue(body > 0 && whiteBelow == 0, "The lower half is coloured glass, not white.");
        // The glass is one hue top to bottom: the upper body is not a different colour block.
        Assert.IsFalse(Enumerable.Range(0, size).Any(x => Enumerable.Range(0, size).Any(y => orb[x, y] == Pal.Coral)), "No red top.");
    }
}
