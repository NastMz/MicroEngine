using TrailDuel.Rules;

namespace TrailDuel.Visuals;

/// <summary>
/// Hand-animated battle effects from the Ninja Adventure pack (CC0, see CREDITS.md). <c>Content/fx/fx.sheet</c> says
/// how each image is cut into frames, where its pivot is and at what rate it plays; they give every element its own
/// crafted impact.
/// </summary>
internal static class FxArt
{
    /// <summary>The effects' description: frames "fx:{name}0", "fx:{name}1"... and a clip "fx:{name}" each.</summary>
    public const string Sheet = "fx/fx.sheet";

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
