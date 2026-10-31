using MicroEngine;

namespace CourtyardKey.Visuals;

/// <summary>
/// The game's master palette: warm lush greens for the vale, cool violet stone for the dungeon,
/// a teal-and-crimson hero, and gold for everything precious. Every sprite and tile draws from here.
/// </summary>
public static class Pal
{
    // Neutrals
    public static readonly Color Ink = Color.Hex("1b1726");
    public static readonly Color Night = Color.Hex("2b2440");
    public static readonly Color White = Color.Hex("f4f1e6");
    public static readonly Color Cream = Color.Hex("e8dcc0");

    // Grass ramp (dark to light)
    public static readonly Color[] Grass = [Color.Hex("1f3f2c"), Color.Hex("2c6438"), Color.Hex("438f42"), Color.Hex("6bb84f"), Color.Hex("a9dc6e")];
    // Foliage for trees and bushes (slightly bluer than grass so they read as separate)
    public static readonly Color[] Leaf = [Color.Hex("16322a"), Color.Hex("1f5536"), Color.Hex("2f7d42"), Color.Hex("54a84c"), Color.Hex("93d46a")];
    public static readonly Color[] Water = [Color.Hex("16304f"), Color.Hex("1f4f86"), Color.Hex("2f74be"), Color.Hex("5aaee6"), Color.Hex("bfeaf7")];
    public static readonly Color[] Sand = [Color.Hex("5e4030"), Color.Hex("8c623c"), Color.Hex("b98a54"), Color.Hex("dcbc80"), Color.Hex("f0dcaa")];
    public static readonly Color[] Rock = [Color.Hex("2c2a36"), Color.Hex("4a4756"), Color.Hex("6e6a78"), Color.Hex("9c97a3"), Color.Hex("cbc6cc")];
    public static readonly Color[] Wood = [Color.Hex("3a2018"), Color.Hex("5f3522"), Color.Hex("8a532f"), Color.Hex("b87d45")];
    public static readonly Color[] Roof = [Color.Hex("5a1a28"), Color.Hex("8f2a35"), Color.Hex("c44a43"), Color.Hex("e47a5c")];
    public static readonly Color[] RoofBlue = [Color.Hex("1d2a52"), Color.Hex("2e4a7d"), Color.Hex("4a72a8"), Color.Hex("7aa6cf")];
    public static readonly Color[] Plaster = [Color.Hex("8f7a68"), Color.Hex("c2ab8e"), Color.Hex("e6d6b8")];

    // Dungeon stone: cool violet so it never reads as the overworld
    public static readonly Color[] Stone = [Color.Hex("1c1a2a"), Color.Hex("2e2b42"), Color.Hex("45405e"), Color.Hex("625b80"), Color.Hex("8b84a8"), Color.Hex("b7b0cf")];
    public static readonly Color[] Moss = [Color.Hex("24402f"), Color.Hex("3b6340"), Color.Hex("5f8a4b")];
    public static readonly Color[] DeepWater = [Color.Hex("0f2230"), Color.Hex("163a4a"), Color.Hex("1f5a66"), Color.Hex("3f8c8c"), Color.Hex("8fd0c0")];
    public static readonly Color[] Carpet = [Color.Hex("4a1424"), Color.Hex("7a1f33"), Color.Hex("a53245")];

    // Characters
    public static readonly Color[] Skin = [Color.Hex("8a4b3a"), Color.Hex("c97b56"), Color.Hex("f2b38a")];
    public static readonly Color[] Tunic = [Color.Hex("1f5f63"), Color.Hex("2f9a8c"), Color.Hex("63d1b2")];
    public static readonly Color[] Hair = [Color.Hex("4e1f18"), Color.Hex("8a3a22"), Color.Hex("c2612f")];
    public static readonly Color[] Scarf = [Color.Hex("7d1d2c"), Color.Hex("c8323c"), Color.Hex("f0645a")];
    public static readonly Color[] Gold = [Color.Hex("7a4a10"), Color.Hex("c98a1e"), Color.Hex("f2c940"), Color.Hex("fff0a0")];
    public static readonly Color[] Heart = [Color.Hex("6e1424"), Color.Hex("d0283c"), Color.Hex("ff7076"), Color.Hex("ffd0c8")];
    public static readonly Color[] Gem = [Color.Hex("0e5a3a"), Color.Hex("22a35a"), Color.Hex("6fe08a"), Color.Hex("d8ffd8")];
    public static readonly Color[] GemBlue = [Color.Hex("1a2f7a"), Color.Hex("2f62d6"), Color.Hex("6fa8ff"), Color.Hex("e0f0ff")];
    public static readonly Color[] GemRed = [Color.Hex("5a0f2a"), Color.Hex("c42a55"), Color.Hex("ff7aa0"), Color.Hex("ffe0ea")];
    public static readonly Color[] Slime = [Color.Hex("1d4a5a"), Color.Hex("2e8a8a"), Color.Hex("5ccdb0"), Color.Hex("c4fff0")];
    public static readonly Color[] Bat = [Color.Hex("2a1838"), Color.Hex("4e2a63"), Color.Hex("8250a0"), Color.Hex("c896e0")];
    public static readonly Color[] Iron = [Color.Hex("262838"), Color.Hex("454a62"), Color.Hex("7a829e"), Color.Hex("c4cadc")];
    public static readonly Color[] Ember = [Color.Hex("7a2010"), Color.Hex("d0501c"), Color.Hex("ff9a2e"), Color.Hex("ffe08a")];
    public static readonly Color[] Spirit = [Color.Hex("123a4a"), Color.Hex("1f7a8c"), Color.Hex("4ad0d0"), Color.Hex("c0fff4")];

    // Lighting tints (what full darkness multiplies the scene toward)
    public static readonly Color DungeonDark = Color.Hex("120e24");
    public static readonly Color ForestDark = Color.Hex("0f2028");
    public static readonly Color TorchGlow = Color.Hex("ff9a40");
}
