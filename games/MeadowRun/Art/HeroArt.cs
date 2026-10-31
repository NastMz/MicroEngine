using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;

namespace MeadowRun.Art;

/// <summary>Which of Pip's three forms a frame is drawn for.</summary>
public enum PipForm { Sprout, Bloom, Ember }

/// <summary>
/// Pip, a round sprout spirit, composed from a pose so every animation frame shares the same
/// proportions. Frames are 20x24 with the pivot at the feet (10, 23), facing right.
/// </summary>
internal static class HeroArt
{
    public const int W = 20, H = 24;
    public static readonly Vector2 Pivot = new(10, 23);

    private enum Eyes { Open, Closed, Hurt, Happy, Squint, Wide }

    private sealed record Pose(
        float BodyY = 0, float Stretch = 0, float FootL = -3, float FootLY = 0, float FootR = 3, float FootRY = 0,
        Eyes Eyes = Eyes.Open, float Sway = 0, bool ArmsUp = false, float ArmY = 0, bool Mouth = false, float Lean = 0);

    private static readonly Dictionary<string, Pose[]> Clips = new()
    {
        ["idle"] = [new(), new(BodyY: 0.6f, Stretch: -0.4f, Sway: 0.5f)],
        ["blink"] = [new(Eyes: Eyes.Closed)],
        ["run"] =
        [
            new(FootL: -5, FootLY: -1, FootR: 3, Sway: -1, Lean: 0.6f),
            new(BodyY: -1, FootL: -2, FootLY: -2, FootR: 1, Sway: -1.5f, Lean: 0.6f),
            new(FootL: -3, FootR: 5, FootRY: -1, Sway: -1, Lean: 0.6f),
            new(BodyY: -1, FootL: -1, FootR: 2, FootRY: -2, Sway: -1.5f, Lean: 0.6f),
        ],
        ["skid"] = [new(BodyY: 0.5f, FootL: 0, FootR: 5, Eyes: Eyes.Squint, Sway: 2, Lean: -1.2f)],
        ["jump"] = [new(BodyY: -1, Stretch: 0.8f, FootL: -2, FootLY: -2, FootR: 2, FootRY: -1, Sway: 2, Mouth: true)],
        ["fall"] = [new(Stretch: 0.3f, FootL: -3, FootLY: 0, FootR: 3, FootRY: 0, Sway: -2, Eyes: Eyes.Wide)],
        ["hurt"] = [new(Eyes: Eyes.Hurt, Mouth: true, Sway: 2, ArmsUp: true)],
        ["victory"] = [new(Eyes: Eyes.Happy, ArmsUp: true, Mouth: true, Sway: 1), new(BodyY: -2, Eyes: Eyes.Happy, ArmsUp: true, ArmY: -1, Mouth: true, Sway: -1)],
        ["throw"] = [new(Lean: 1, FootL: -4, FootR: 4, Eyes: Eyes.Squint, Mouth: true, Sway: -1)],
        ["crouch"] = [new(BodyY: 2, Stretch: -1.4f, FootL: -4, FootR: 4, Eyes: Eyes.Squint, Sway: 1.5f)],
    };

    public static IEnumerable<string> ClipNames => Clips.Keys;

    public static IEnumerable<PixelImage> Frames(string clip, PipForm form) => Clips[clip].Select(p => Draw(p, form));

    private static (Color[] Body, Color Belly, Color Cheek) Skin(PipForm form) => form switch
    {
        PipForm.Ember => ([Pal.Wine, Pal.Rust, Pal.Flame, Pal.Gold], Pal.Butter, Pal.Blush),
        _ => ([Pal.Fern, Pal.Leaf, Pal.Lime, Pal.Sprout], Pal.Butter, Pal.Coral),
    };

    private static PixelImage Draw(Pose p, PipForm form)
    {
        var img = new PixelImage(W, H);
        var (body, belly, cheek) = Skin(form);
        float cx = 10 + p.Lean * 0.5f, cy = 16.5f + p.BodyY - p.Stretch * 0.5f;
        float rx = 6.4f - p.Stretch * 0.35f, ry = 5.6f + p.Stretch * 0.5f;

        // Feet behind the body: small dark bean shapes.
        var foot = new[] { Pal.Bark, Pal.Soil, Pal.Clay, Pal.Sand };
        Brush.Ellipse(img, 10 + p.FootL, 21.6f + p.FootLY, 2.2f, 1.4f, foot, -0.1f);
        Brush.Ellipse(img, 10 + p.FootR, 21.6f + p.FootRY, 2.2f, 1.4f, foot);

        // Arms (tiny nubs) for victory, hurt and throw poses.
        if (p.ArmsUp)
        {
            Brush.Ellipse(img, cx - 6.5f, cy - 4.5f + p.ArmY, 1.4f, 1.6f, body);
            Brush.Ellipse(img, cx + 6.5f, cy - 4.5f + p.ArmY, 1.4f, 1.6f, body);
        }

        Brush.Ellipse(img, cx, cy, rx, ry, body);
        // Belly highlight on the lower front.
        Brush.Flat(img, cx + 1.6f, cy + 2.4f, rx * 0.5f, ry * 0.42f, Color.Lerp(body[3], belly, 0.6f));
        Brush.Flat(img, cx + 1.9f, cy + 2.8f, rx * 0.34f, ry * 0.26f, belly);

        DrawHead(img, p, form, cx, cy - ry);
        DrawFace(img, p, cx, cy, cheek);

        Brush.SelOutline(img, Pal.Ink);
        return img;
    }

    private static void DrawHead(PixelImage img, Pose p, PipForm form, float cx, float top)
    {
        int sx = (int)MathF.Round(cx - 1 + p.Sway * 0.3f);
        int ty = (int)MathF.Round(top);
        var stem = new[] { Pal.Pine, Pal.Fern, Pal.Leaf, Pal.Lime };
        img.Line((int)MathF.Round(cx - 1), ty, sx, ty - 3, Pal.Fern);
        if (form == PipForm.Sprout)
        {
            float sway = p.Sway;
            Brush.Ellipse(img, sx - 2.6f - sway * 0.2f, ty - 3.4f + sway * 0.3f, 2.8f, 1.5f, stem);
            Brush.Ellipse(img, sx + 2.6f + sway * 0.2f, ty - 4.2f - sway * 0.3f, 2.8f, 1.5f, stem, 0.1f);
            return;
        }
        // Bloom and Ember: five petals around a bright centre.
        Color[] petals = form == PipForm.Bloom ? [Pal.Wine, Pal.Berry, Pal.Coral, Pal.Blush] : [Pal.Rust, Pal.Flame, Pal.Gold, Pal.Butter];
        float fx = sx + p.Sway * 0.25f, fy = ty - 4.5f;
        for (int i = 0; i < 5; i++)
        {
            float a = -MathF.PI / 2 + i * MathF.Tau / 5 + p.Sway * 0.08f;
            Brush.Ellipse(img, fx + MathF.Cos(a) * 2.6f, fy + MathF.Sin(a) * 2.3f, 1.9f, 1.9f, petals);
        }
        Brush.Ellipse(img, fx, fy, 1.4f, 1.4f, form == PipForm.Bloom ? Pal.GoldRamp : [Pal.Amber, Pal.Gold, Pal.Butter, Pal.Cream]);
        Brush.Ellipse(img, sx - 3.2f, ty - 0.8f, 2.2f, 1.1f, stem);
    }

    private static void DrawFace(PixelImage img, Pose p, float cx, float cy, Color cheek)
    {
        int ey = (int)MathF.Round(cy - 2.2f);
        int e1 = (int)MathF.Round(cx - 0.4f + p.Lean * 0.4f), e2 = e1 + 3;
        foreach (int ex in new[] { e1, e2 })
        {
            switch (p.Eyes)
            {
                case Eyes.Open:
                case Eyes.Wide:
                    img.FillRect(ex, ey, 2, p.Eyes == Eyes.Wide ? 4 : 3, Pal.Ink);
                    img[ex, ey] = Pal.Cream;
                    break;
                case Eyes.Closed:
                    img.FillRect(ex, ey + 2, 2, 1, Pal.Ink);
                    break;
                case Eyes.Squint:
                    img.FillRect(ex, ey + 1, 2, 1, Pal.Ink);
                    img[ex + (ex == e1 ? 0 : 1), ey + 2] = Pal.Ink;
                    break;
                case Eyes.Happy:
                    img[ex, ey + 1] = Pal.Ink; img[ex + 1, ey] = Pal.Ink; img[ex + 2, ey + 1] = Pal.Ink;
                    break;
                case Eyes.Hurt:
                    img[ex, ey] = Pal.Ink; img[ex + 1, ey + 1] = Pal.Ink; img[ex, ey + 2] = Pal.Ink;
                    img[ex + 1, ey + 2] = Pal.Ink; img[ex + 1, ey] = Pal.Ink;
                    break;
            }
        }
        img[e1 - 2, ey + 3] = cheek;
        img[e1 - 1, ey + 3] = Color.Lerp(cheek, Pal.Leaf, 0.4f);
        if (p.Mouth)
        {
            img.FillRect(e1 + 2, ey + 4, 2, 2, Pal.Wine);
            img[e1 + 2, ey + 4] = Pal.Ink;
            img[e1 + 3, ey + 4] = Pal.Ink;
        }
        else
        {
            img[e1 + 2, ey + 4] = Pal.Ink;
            img[e1 + 3, ey + 4] = Pal.Ink;
        }
    }
}
