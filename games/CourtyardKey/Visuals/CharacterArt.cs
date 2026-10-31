using MicroEngine;
using MicroEngine.Graphics;

namespace CourtyardKey.Visuals;

/// <summary>
/// Wren the hero and the villagers. Characters are hand-authored character maps assembled from an
/// upper body and interchangeable legs, so walk cycles share one silhouette.
/// </summary>
public static class CharacterArt
{
    private static string HeroPalette =>
        $"o={H(Pal.Ink)} e={H(Pal.Night)} h={H(Pal.Hair[0])} H={H(Pal.Hair[1])} j={H(Pal.Hair[2])} " +
        $"S={H(Pal.Skin[1])} s={H(Pal.Skin[2])} T={H(Pal.Tunic[0])} t={H(Pal.Tunic[1])} l={H(Pal.Tunic[2])} " +
        $"R={H(Pal.Scarf[0])} r={H(Pal.Scarf[1])} q={H(Pal.Scarf[2])} B={H(Pal.Wood[0])} b={H(Pal.Wood[1])} y={H(Pal.Gold[2])}";

    private static string H(Color c) => ArtKit.Hex(c);

    private static readonly string[] DownBody =
    [
        "................",
        ".....oooooo.....",
        "....ojjjHHHo....",
        "...ojjHHHHHHo...",
        "...oHHHHHHHho...",
        "...oHsHHHHsho...",
        "...oHsessesHo...",
        "....oSssssSo....",
        "....oqrrrrRo....",
        "...oltrRRrtTo...",
        "..osltttttTTso..",
        "...oTbbyybbTo...",
        "....oTttttTo....",
    ];

    private static readonly string[] UpBody =
    [
        "................",
        ".....oooooo.....",
        "....ojjjjjHo....",
        "...ojjjHHHHHo...",
        "...oHjHHHHHHo...",
        "...oHHHHHHHho...",
        "...ohHHHHHHho...",
        "....ohHHHHho....",
        "....oRrrrrqo....",
        "...oTtrRRrtlo...",
        "..osTTtRrtttso..",
        "...oTbbRbbbTo...",
        "....oTtRttTo....",
    ];

    private static readonly string[] SideBody =
    [
        "................",
        ".....ooooo......",
        "....oHjjjHo.....",
        "...oHjjjHHHo....",
        "...oHHHHHHHo....",
        "...ohHHHssso....",
        "...ohHHsseso....",
        "....ohSsssSo....",
        "...oRRrrrqo.....",
        "..oRoTtllto.....",
        "..o.oTtTtso.....",
        "....oTbbyo......",
        "....oTtttTo.....",
    ];

    private static readonly string[][] FrontLegs =
    [
        ["....obbooBbo....", "....oBBooBBo....", ".....oo..oo....."],
        ["....obbooBbo....", "....oBBo.oo.....", ".....oo........."],
        ["....obbooBbo....", ".....oo.oBBo....", ".........oo....."],
    ];

    private static readonly string[][] SideLegs =
    [
        [".....obbo.......", ".....oBBo.......", ".....oooo......."],
        ["....obo.obo.....", "...oBo...oBo....", "...oo.....oo...."],
    ];

    /// <summary>Adds hero frames: hero_{down|up|side}{0..2}, hero_get, hero_lie.</summary>
    public static void AddHero(SpriteAtlasBuilder b)
    {
        var origin = new System.Numerics.Vector2(8, 15);
        for (int f = 0; f < 3; f++)
        {
            b.Add($"hero_down{f}", Compose(DownBody, FrontLegs[f], f), origin);
            b.Add($"hero_up{f}", Compose(UpBody, FrontLegs[f], f), origin);
        }
        b.Add("hero_side0", Compose(SideBody, SideLegs[0], 0), origin);
        b.Add("hero_side1", Compose(SideBody, SideLegs[1], 1), origin);
        b.Add("hero_side2", Compose(SideBody, SideLegs[0], 1), origin);
        b.Add("hero_get", ItemGetPose(), origin);
        b.Add("hero_lie", (Compose(SideBody, SideLegs[0], 0)).Rotate90(), new System.Numerics.Vector2(8, 12));
    }

    private static PixelImage Compose(string[] body, string[] legs, int frame)
    {
        var rows = body.Concat(legs).ToArray();
        var img = ArtKit.Map(HeroPalette, rows);
        // Walking frames bob the upper body one pixel: a cheap but lively cycle.
        if (frame != 0)
        {
            var bobbed = new PixelImage(img.Width, img.Height);
            bobbed.Blit(img, 0, 0, img.Width, 13, 0, 1);
            bobbed.Blit(img, 0, 13, img.Width, 3, 0, 13);
            return bobbed;
        }
        return img;
    }

    private static PixelImage ItemGetPose()
    {
        var img = Compose(DownBody, FrontLegs[0], 0);
        // Both arms raised above the head to present the treasure.
        foreach (int x in new[] { 2, 12 })
        {
            img.FillRect(x, 3, 2, 6, Pal.Tunic[1]);
            img.FillRect(x, 3, 1, 6, Pal.Tunic[2]);
            img.FillRect(x, 1, 2, 2, Pal.Skin[2]);
        }
        img[1, 1] = Pal.Ink; img[1, 2] = Pal.Ink; img[4, 1] = Pal.Ink; img[2, 0] = Pal.Ink; img[3, 0] = Pal.Ink;
        img[11, 1] = Pal.Ink; img[14, 1] = Pal.Ink; img[14, 2] = Pal.Ink; img[12, 0] = Pal.Ink; img[13, 0] = Pal.Ink;
        // Wide-eyed delight.
        img[6, 6] = Pal.Ink; img[9, 6] = Pal.Ink; img[7, 7] = Pal.Skin[0]; img[8, 7] = Pal.Skin[0];
        return img;
    }

    // ---------------------------------------------------------------- villagers

    /// <summary>Adds npc_{look}{0,1} (two idle breathing frames) for every villager.</summary>
    public static void AddVillagers(SpriteAtlasBuilder b)
    {
        var origin = new System.Numerics.Vector2(8, 15);
        foreach (var (name, flat) in Villagers())
        {
            var img = ArtKit.Volume(flat, Pal.Ink);
            b.Add($"npc_{name}0", img, origin);
            b.Add($"npc_{name}1", Breathe(img), origin);
        }
    }

    private static PixelImage Breathe(PixelImage img)
    {
        var copy = new PixelImage(img.Width, img.Height);
        copy.Blit(img, 0, 0, img.Width, 9, 0, 1);
        copy.Blit(img, 0, 9, img.Width, img.Height - 9, 0, 9);
        return copy;
    }

    private static IEnumerable<(string, PixelImage)> Villagers()
    {
        string P(Color hairDark, Color hair, Color clothDark, Color cloth, Color clothLight, Color extra, Color extraDark) =>
            $"o={H(Pal.Ink)} e={H(Pal.Night)} S={H(Pal.Skin[1])} s={H(Pal.Skin[2])} a={H(hairDark)} A={H(hair)} " +
            $"C={H(clothDark)} c={H(cloth)} d={H(clothLight)} w={H(extra)} W={H(extraDark)} y={H(Pal.Gold[2])} b={H(Pal.Wood[1])}";

        var elder = ArtKit.Map(P(Pal.Rock[3], Pal.White, Pal.RoofBlue[0], Pal.RoofBlue[1], Pal.RoofBlue[2], Pal.White, Pal.Rock[3]),
            "................",
            ".....oooooo.....",
            "....osssssso....",
            "...oWsssssswo...",
            "...oWseSSeswo...",
            "...owwsSSsswo...",
            "....owwwwwwo....",
            "...oCowwwwoCo...",
            "..oCcCowwoCcCo..",
            "..oCcccowoccCo..",
            "..osCcccccccso..",
            "...oCcyyyyccCo..",
            "...oCccccccCCo..",
            "...oCCccccCCCo..",
            "...oCCCCCCCCCo..",
            "....oooooooooo..");
        // A gnarled staff in the elder's left hand.
        elder.FillRect(13, 2, 1, 13, Pal.Wood[2]);
        elder.FillRect(14, 2, 1, 13, Pal.Wood[0]);
        elder.FillRect(12, 1, 3, 2, Pal.Wood[1]);
        elder[13, 0] = Pal.Leaf[3];
        yield return ("elder", elder);

        yield return ("merchant", ArtKit.Map(P(Pal.Wood[0], Pal.Wood[2], Pal.Roof[0], Pal.Roof[1], Pal.Roof[2], Pal.White, Pal.Cream),
            "......oooo......",
            ".....oAAAAo.....",
            "....oAAAAAAo....",
            "...oAAssssAAo...",
            "...oAseSSesAo...",
            "...oAsssssssao..",
            "....oSsSSsSo....",
            "....ocoSSocco...",
            "...occwwwwcco...",
            "..oscCwwwwCcso..",
            "..oSoCwwWwCoSo..",
            "...ocCwwwwCco...",
            "...occwWwwcco...",
            "..occcwwwwccco..",
            "..oCCCCCCCCCCo..",
            "...oooooooooo..."));

        yield return ("girl", ArtKit.Map(P(Pal.Gold[1], Pal.Gold[2], Pal.Heart[0], Pal.Heart[1], Pal.Heart[2], Pal.White, Pal.Cream),
            "................",
            "..oo.oooooo.oo..",
            ".oAAoAAAAAAoAAo.",
            ".oAoAAAAAAAAoAo.",
            "..ooAssssssAoo..",
            "...oAseSSesAo...",
            "....osssssSo....",
            ".....oSSSSo.....",
            "....ocwwwwco....",
            "...occcddccco...",
            "..osoccccccoso..",
            "...ocCccccCco...",
            "..occccccccco...",
            "..oCCCCCCCCCCo..",
            "...oo.oSSo.oo...",
            "......oo.oo....."));

        yield return ("kid", ArtKit.Map(P(Pal.Wood[0], Pal.Wood[1], Pal.Ember[0], Pal.Ember[1], Pal.Ember[2], Pal.RoofBlue[1], Pal.RoofBlue[0]),
            "................",
            "................",
            ".....oooooo.....",
            "....oWwwwwwo....",
            "...oWwwwwwwwoo..",
            "...ooaAAAAaoWWo.",
            "...oAseSSesAo...",
            "....osssssso....",
            ".....oSSSSo.....",
            "....occddcco....",
            "...osccddccso...",
            "....oCccccCo....",
            "....oWWWWWWo....",
            "....oWWooWWo....",
            "....obbo.obbo...",
            ".....oo...oo...."));

        yield return ("fisher", ArtKit.Map(P(Pal.Rock[2], Pal.Rock[3], Pal.RoofBlue[0], Pal.RoofBlue[1], Pal.RoofBlue[2], Pal.Sand[3], Pal.Sand[1]),
            "................",
            "......oooo......",
            ".....owwwwo.....",
            "..ooowwwwwwooo..",
            ".oWWWWWWWWWWWWo.",
            "..oooAssssAooo..",
            "...oAseSSesAo...",
            "...oAAAsssAAo...",
            "....oAAAAAAo....",
            "...occoAAoCco...",
            "..oscddccddcso..",
            "..oSoCddddCoSo..",
            "...oCCddddCCo...",
            "...oCCCCCCCCo...",
            "....obbo.obbo...",
            ".....oo...oo...."));

        yield return ("hermit", ArtKit.Map(P(Pal.Rock[2], Pal.Rock[4], Pal.Bat[0], Pal.Bat[1], Pal.Bat[2], Pal.Bat[3], Pal.Bat[0]),
            "......oooo......",
            ".....occcco.....",
            "....occddcco....",
            "...occAAAAcco...",
            "...ocAseSSeAco..",
            "...ocAssssAco...",
            "...occsSSsscco..",
            "..occcoSSoccCo..",
            "..occccddccCCo..",
            "..ocsccddccsCo..",
            "..ocSccyyccSCo..",
            "..occcccccccCo..",
            "..oCccccccccCo..",
            ".oCCcccccccCCCo.",
            ".oCCCCCCCCCCCCo.",
            "..oooooooooooo.."));
    }
}
