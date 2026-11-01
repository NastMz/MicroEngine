using MicroEngine;
using MicroEngine.Graphics;

namespace GemGarden;

/// <summary>The art drawn in code: the gem, the slime and the two tiles. The hero comes from Content/hero.png.</summary>
public static class GardenArt
{
    #region palette
    // One character per colour: the rows below are pixel maps, '.' is transparent.
    private static readonly Dictionary<char, Color> Palette = Art.Palette(
        "o=#1a1c2c", "c=#41a6f6", "w=#ffffff", "p=#b13e53", "g=#38b764", "d=#257179");

    public static PixelImage Gem() => PixelImage.FromMap(Palette,
        "....o....",
        "...oco...",
        "..occco..",
        ".occwcco.",
        "occccccco",
        ".occccco.",
        "..occco..",
        "...oco...",
        "....o....");
    #endregion

    /// <summary>Two frames: the slime at rest and squashed.</summary>
    public static PixelImage[] Slime() =>
    [
        PixelImage.FromMap(Palette,
            "............",
            "...oooooo...",
            "..oppppppo..",
            ".oppppppppo.",
            ".oppwppwppo.",
            "oppppppppppo",
            "oppppppppppo",
            "oooooooooooo"),
        PixelImage.FromMap(Palette,
            "............",
            "............",
            "..oooooooo..",
            ".oppppppppo.",
            "oppwppppwppo",
            "oppppppppppo",
            "oppppppppppo",
            "oooooooooooo"),
    ];

    #region tiles
    /// <summary>Grass: two greens mixed by value noise and an ordered dither.</summary>
    public static PixelImage Grass() => new PixelImage(16, 16)
        .Map((x, y, _) => Art.Dither(x, y, Art.ValueNoise(x, y, 6) * 0.45f) ? Palette['d'] : Palette['g']);

    /// <summary>Hedge: dark leaves with light speckles and an outline.</summary>
    public static PixelImage Hedge() => new PixelImage(16, 16)
        .Map((x, y, _) => Art.Hash(x, y, 7) > 0.75f ? Palette['g'] : Palette['d'])
        .DrawRect(0, 0, 16, 16, Palette['o']);
    #endregion
}
