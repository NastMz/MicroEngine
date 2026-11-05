using MicroEngine;

namespace MeadowRun.Art;

/// <summary>
/// The Meadow Run master palette (32 colours). Every sprite, tile and UI element picks from
/// these ramps so the four biomes still read as one game.
/// </summary>
internal static class Pal
{
    public static readonly Color Ink = Color.Hex("1b1a2e");
    public static readonly Color Night = Color.Hex("2b2a45");
    public static readonly Color Slate = Color.Hex("45436b");
    public static readonly Color Dusk = Color.Hex("6f6d96");
    public static readonly Color Haze = Color.Hex("a5a3c9");
    public static readonly Color Mist = Color.Hex("e8e6f5");
    public static readonly Color Cream = Color.Hex("fffaf0");

    public static readonly Color Pine = Color.Hex("1f4d3a");
    public static readonly Color Fern = Color.Hex("2e7a4f");
    public static readonly Color Leaf = Color.Hex("4fae5c");
    public static readonly Color Lime = Color.Hex("8fd86a");
    public static readonly Color Sprout = Color.Hex("d4f58c");

    public static readonly Color Bark = Color.Hex("3b2433");
    public static readonly Color Soil = Color.Hex("5e3b3a");
    public static readonly Color Clay = Color.Hex("8a5a44");
    public static readonly Color Sand = Color.Hex("b98a5e");
    public static readonly Color Wheat = Color.Hex("e3bf8a");

    public static readonly Color Ocean = Color.Hex("3a5fb8");
    public static readonly Color Sky = Color.Hex("5b9be8");
    public static readonly Color Air = Color.Hex("9fd3ff");
    public static readonly Color Cloud = Color.Hex("d9f0ff");

    public static readonly Color Amber = Color.Hex("f5a524");
    public static readonly Color Gold = Color.Hex("ffd447");
    public static readonly Color Butter = Color.Hex("fff3a3");

    public static readonly Color Wine = Color.Hex("a3263a");
    public static readonly Color Berry = Color.Hex("e04b4b");
    public static readonly Color Coral = Color.Hex("ff8a7a");
    public static readonly Color Blush = Color.Hex("ffc2c9");

    public static readonly Color Plum = Color.Hex("4a2a7a");
    public static readonly Color Violet = Color.Hex("7b4fd0");
    public static readonly Color Lilac = Color.Hex("b28bff");
    public static readonly Color Teal = Color.Hex("2fa5b5");
    public static readonly Color Aqua = Color.Hex("6ff0e6");

    public static readonly Color Rust = Color.Hex("c8461c");
    public static readonly Color Flame = Color.Hex("ff7a2e");

    // Light grades: the colour a dark biome's light map multiplies the scene by (not sprite colours).
    public static readonly Color CaveShade = Color.Hex("9c94c8");
    public static readonly Color CastleShade = Color.Hex("e0ccd0");

    public static readonly Color[] GrassRamp = [Pine, Fern, Leaf, Lime];
    public static readonly Color[] SoilRamp = [Bark, Soil, Clay, Sand];
    public static readonly Color[] StoneRamp = [Night, Slate, Dusk, Haze];
    public static readonly Color[] GoldRamp = [Rust, Amber, Gold, Butter];
    public static readonly Color[] CrystalRamp = [Plum, Violet, Lilac, Mist];
    public static readonly Color[] CloudRamp = [Haze, Air, Cloud, Cream];
    public static readonly Color[] LavaRamp = [Wine, Rust, Flame, Gold];
}
