using MicroEngine;

namespace TrailDuel.Visuals;

/// <summary>
/// The Trail Duel palette: sunlit, natural greens and earths with deep plum outlines, matched to the
/// handheld-RPG style of the creature and trainer sprites, in short 3-5 step ramps per material.
/// </summary>
internal static class Pal
{
    public static Color H(string hex) => Color.Hex(hex);

    // Ink and UI
    public static readonly Color Ink = H("2b2140");
    public static readonly Color InkSoft = H("4a3d63");
    public static readonly Color Cream = H("fbf5e6");
    public static readonly Color CreamShade = H("e9dcc2");
    public static readonly Color Paper = H("f4ead2");
    public static readonly Color Coral = H("e8735a");
    public static readonly Color CoralDark = H("b8503f");
    public static readonly Color Gold = H("f2c25c");
    public static readonly Color GoldDark = H("c48a35");
    public static readonly Color Sky = H("9fd6f0");
    public static readonly Color TextDark = H("3a3150");
    public static readonly Color TextShadow = H("d8ccb4");
    public static readonly Color White = H("fffdf6");

    // Ground
    public static readonly Color[] Grass = [H("2c6634"), H("438a3c"), H("5fa646"), H("88c65a")];
    public static readonly Color[] TallGrass = [H("1a3d24"), H("2f7034"), H("4a9a40"), H("7cc456")];
    public static readonly Color[] Path = [H("7a5434"), H("a87a4a"), H("c89c62"), H("dcbc84")];
    public static readonly Color[] Water = [H("244c94"), H("3268b8"), H("4c8ad6"), H("80b8ec"), H("e4f6ff")];
    public static readonly Color[] Canopy = [H("163a28"), H("21592f"), H("2f7a38"), H("48a046"), H("76c05a")];
    public static readonly Color[] Bark = [H("4a3030"), H("6b4a3a"), H("8a5f43")];
    public static readonly Color[] Stone = [H("4f4a5e"), H("6f6a80"), H("9590a6"), H("c4c0d0")];
    public static readonly Color[] Wood = [H("5e3b2e"), H("8a5a3c"), H("b07a4c"), H("d39d64")];
    public static readonly Color[] Floor = [H("9c6a45"), H("b98154"), H("cf9a68"), H("e2b884")];
    public static readonly Color[] Wall = [H("c9b08a"), H("e3d0aa"), H("f3e6c8")];
    public static readonly Color[] Plaster = [H("b8a68c"), H("d8c8aa"), H("efe3c8")];

    // Roofs
    public static readonly Color[] RoofRed = [H("7a2e3a"), H("b04848"), H("d8685a"), H("f0907a")];
    public static readonly Color[] RoofBlue = [H("2c3f74"), H("3f5fa4"), H("5b82c8"), H("86aee2")];
    public static readonly Color[] RoofTeal = [H("1f5458"), H("2f7d78"), H("47a596"), H("74ccb4")];
    public static readonly Color[] RoofPink = [H("7a3458"), H("b0507a"), H("d87aa0"), H("f2a6c2")];
    public static readonly Color[] RoofGold = [H("6e4220"), H("a8662c"), H("d8973c"), H("f2c25c")];

    // Flowers
    public static readonly Color[] FlowerColors = [H("f27a8a"), H("fbe36a"), H("ffffff"), H("b88ae8")];

    // Characters
    public static readonly Color Skin = H("f6c9a0");
    public static readonly Color SkinShade = H("d9966e");

    // Type accent colours used by UI badges and particle effects.
    public static Color TypeColor(Rules.ElementType t) => t switch
    {
        Rules.ElementType.Flame => H("ec6a3c"),
        Rules.ElementType.Tide => H("4f93d6"),
        Rules.ElementType.Leaf => H("5cb04f"),
        Rules.ElementType.Volt => H("f2c838"),
        Rules.ElementType.Stone => H("a88a62"),
        Rules.ElementType.Gale => H("8ec6e0"),
        _ => H("b6a8a0"),
    };

    public static Color TypeColorDark(Rules.ElementType t) => TypeColor(t).Shade(0.62f);
}
