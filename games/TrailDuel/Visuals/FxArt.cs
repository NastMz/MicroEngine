using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Graphics;
using TrailDuel.Rules;

namespace TrailDuel.Visuals;

/// <summary>
/// Hand-animated battle effects from the Ninja Adventure pack (CC0, see CREDITS.md), sliced from
/// <c>Content/fx</c> into atlas frames "fx:{name}0", "fx:{name}1", ... The creatures stay procedural;
/// these give every element its own crafted impact.
/// </summary>
internal static class FxArt
{
    /// <summary>Effect name, file, frame size and the pivot the effect is centred on.</summary>
    private static readonly (string Name, int W, int H, float OriginY)[] Sheets =
    [
        ("flame", 25, 30, 0.75f),
        ("explosion", 40, 40, 0.6f),
        ("splash", 44, 33, 0.7f),
        ("geyser", 30, 41, 0.9f),
        ("leaves", 30, 28, 0.5f),
        ("thunder", 20, 28, 0.85f),
        ("rocks", 30, 30, 0.7f),
        ("rock-spike", 54, 48, 0.85f),
        ("gale", 63, 55, 0.5f),
        ("claw", 32, 32, 0.5f),
        ("cut", 32, 32, 0.5f),
        ("boost", 53, 35, 0.8f),
        ("sparkle", 27, 35, 0.6f),
        ("smoke", 32, 32, 0.6f),
        ("spark-ring", 32, 32, 0.5f),
        ("aura", 25, 24, 0.8f),
    ];

    public static void AddAll(SpriteAtlasBuilder b, Func<string, PixelImage> image)
    {
        foreach (var (name, w, h, oy) in Sheets)
            b.AddGrid("fx:" + name, image($"fx/{name}.png"), w, h, origin: new Vector2(w / 2f, h * oy));
    }

    /// <summary>The impact a move of this element leaves on its target; strong moves get the bigger version.</summary>
    public static string Impact(ElementType type, bool big) => type switch
    {
        ElementType.Flame => big ? "explosion" : "flame",
        ElementType.Tide => big ? "geyser" : "splash",
        ElementType.Leaf => "leaves",
        ElementType.Volt => "thunder",
        ElementType.Stone => big ? "rock-spike" : "rocks",
        ElementType.Gale => "gale",
        _ => big ? "cut" : "claw",
    };
}
