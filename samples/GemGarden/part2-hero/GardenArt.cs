using MicroEngine;
using MicroEngine.Graphics;

namespace GemGarden;

/// <summary>The art drawn in code. The hero comes from Content/hero.png.</summary>
public static class GardenArt
{
    #region palette
    // One character per colour, used by the pixel maps and the tiles.
    private static readonly Dictionary<char, Color> Palette = Art.Palette("o=#1a1c2c", "g=#38b764", "d=#257179");
    #endregion

    #region grass
    /// <summary>Grass: two greens mixed by value noise and an ordered dither.</summary>
    public static PixelImage Grass() => new PixelImage(16, 16)
        .Map((x, y, _) => Art.Dither(x, y, Art.ValueNoise(x, y, 6) * 0.45f) ? Palette['d'] : Palette['g']);
    #endregion
}
