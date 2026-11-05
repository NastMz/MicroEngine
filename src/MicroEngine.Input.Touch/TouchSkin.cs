using System.Numerics;
using MicroEngine.Graphics;

namespace MicroEngine.Input;

/// <summary>The outline of a drawn touch control.</summary>
public enum TouchShape
{
    /// <summary>A disc (the default).</summary>
    Circle,
    /// <summary>A square with rounded corners (<see cref="TouchStyle.Corner"/>).</summary>
    RoundedSquare,
}

/// <summary>
/// How touch controls look: shape, colours of each state, outline, press feedback and optional images. Only the
/// drawing changes: hit areas, finger tracking and actions stay those of the layout, so a restyled control behaves
/// exactly as before. Colours left null follow <see cref="TouchLayout.Tint"/> and <see cref="TouchLayout.PressedTint"/>.
/// Set one style for every control kind (<see cref="TouchLayout.ButtonStyle"/>, <see cref="TouchLayout.DPadStyle"/>,
/// <see cref="TouchLayout.StickStyle"/>) or for one control (<see cref="TouchControl.Style"/>).
/// </summary>
public sealed record TouchStyle
{
    /// <summary>The outline: a disc or a rounded square.</summary>
    public TouchShape Shape { get; init; } = TouchShape.Circle;
    /// <summary>Corner radius of a rounded square, as a fraction of the control's radius (0..1).</summary>
    public float Corner { get; init; } = 0.35f;
    /// <summary>Fill of an idle control (null: the tint, faint).</summary>
    public Color? Fill { get; init; }
    /// <summary>Fill of a pressed control (null: the pressed tint).</summary>
    public Color? PressedFill { get; init; }
    /// <summary>Outline and arrows of an idle control (null: the tint).</summary>
    public Color? Outline { get; init; }
    /// <summary>Outline, lit arrows and the joystick's direction arrow while pressed (null: the tint, brighter, and the pressed tint for arrows).</summary>
    public Color? PressedOutline { get; init; }
    /// <summary>Colour of a button's label (null: the tint, white while pressed).</summary>
    public Color? Label { get; init; }
    /// <summary>Thickness of the outline in dp (0: none).</summary>
    public float OutlineWidth { get; init; } = 2.2f;
    /// <summary>Whether a dark ring is drawn around the outline, so controls stay visible over light scenes.</summary>
    public bool Halo { get; init; } = true;
    /// <summary>Size of a pressed button relative to an idle one (1: no shrink).</summary>
    public float PressedScale { get; init; } = 0.92f;
    /// <summary>Opacity of idle controls with this style, 0..1 (null: <see cref="TouchLayout.Opacity"/>).</summary>
    public float? Opacity { get; init; }
    /// <summary>
    /// An image drawn over the control's square (twice its radius) instead of fill and outline: a sprite button, a
    /// d-pad cross, a joystick base. Pixel art scales without blur.
    /// </summary>
    public PixelImage? Image { get; init; }
    /// <summary>The image while pressed (null: <see cref="Image"/>, tinted with the pressed fill).</summary>
    public PixelImage? PressedImage { get; init; }
    /// <summary>For joysticks: the knob's image (null: a disc).</summary>
    public PixelImage? KnobImage { get; init; }
    /// <summary>For joysticks: the knob's radius as a fraction of the stick's radius.</summary>
    public float KnobSize { get; init; } = 0.46f;
    /// <summary>For d-pads: whether the four arrows are drawn (an image may already show them).</summary>
    public bool Arrows { get; init; } = true;
}

/// <summary>Everything a skin needs to draw one control as it is this frame, in screen pixels.</summary>
/// <param name="Control">The control as declared in the layout.</param>
/// <param name="Style">The style that applies (the control's own, else its kind's from the layout, else the default).</param>
/// <param name="Center">The control's centre.</param>
/// <param name="Radius">Its drawn radius.</param>
/// <param name="PixelsPerDp">The layout's scale, for line widths in dp.</param>
/// <param name="Opacity">Opacity of the idle control, 0..1.</param>
/// <param name="Glow">1 while pressed, fading to 0 shortly after release (press feedback).</param>
/// <param name="Pressed">Whether the control is pressed (held, toggled on, pushed).</param>
/// <param name="Toggled">For toggle buttons: whether it is on.</param>
/// <param name="Held">Whether a finger is on it.</param>
/// <param name="Origin">Where a joystick's base is (it moves with a floating stick); the centre for other controls.</param>
/// <param name="Deflection">The thumb's offset from the base, each axis -1..1 (sticks and d-pads).</param>
/// <param name="Value">A joystick's value after its dead zone, or a d-pad's direction.</param>
/// <param name="Directions">The direction actions pressed now (sticks and d-pads).</param>
/// <param name="Layout">The layout, for its tints.</param>
public readonly record struct TouchControlView(TouchControl Control, TouchStyle Style, Vector2 Center, float Radius, float PixelsPerDp, float Opacity, float Glow,
    bool Pressed, bool Toggled, bool Held, Vector2 Origin, Vector2 Deflection, Vector2 Value, IReadOnlySet<InputMap.StickDirection> Directions, TouchLayout Layout);

/// <summary>
/// Draws touch controls. The engine calls it for each shown control with its state; a game may replace the whole look
/// (<see cref="TouchLayout.Skin"/>) and still delegate kinds it does not change to <see cref="DefaultTouchSkin.Instance"/>.
/// </summary>
public interface ITouchSkin
{
    /// <summary>Draws one control.</summary>
    void Draw(IOverlayCanvas canvas, in TouchControlView view);
}

/// <summary>
/// The built-in look, shaped by <see cref="TouchStyle"/>. Idle controls are an outlined shape at the style's opacity;
/// a pressed one is filled, outlined thicker and drawn slightly smaller (pressed in), so the press reads by shape as
/// well as colour, even at low opacity. A dark halo keeps outlines legible on light scenes. A d-pad lights the arrows
/// it presses and marks where the thumb is; a joystick shows its base, its dead zone, the knob at the thumb's
/// deflection and an arrow on the rim whose opacity follows the intensity. Images replace fills and outlines.
/// </summary>
public sealed class DefaultTouchSkin : ITouchSkin
{
    /// <summary>The shared instance.</summary>
    public static DefaultTouchSkin Instance { get; } = new();

    /// <inheritdoc/>
    public void Draw(IOverlayCanvas canvas, in TouchControlView v)
    {
        var s = v.Style;
        float op = Math.Clamp(s.Opacity ?? v.Opacity, 0.05f, 1), g = v.Glow, r = v.Radius;
        float line = s.OutlineWidth <= 0 ? 0 : Math.Max(1.5f, s.OutlineWidth * v.PixelsPerDp);
        var tint = s.Outline ?? v.Layout.Tint;
        var pressedTint = s.PressedFill ?? v.Layout.PressedTint;
        var outline = Color.Lerp(tint, s.PressedOutline ?? tint, g).WithAlpha(Math.Min(1, 0.7f * op + 0.3f * g + 0.1f));
        var halo = new Color(0, 0, 0, (byte)(90 * op + 60 * g));
        switch (v.Control)
        {
            case TouchButton b:
            {
                float drawn = r * (1 - (1 - Math.Clamp(s.PressedScale, 0.5f, 1)) * g);
                if (s.Image is { } image)
                {
                    var picture = v.Pressed && s.PressedImage is { } p ? p : image;
                    var shade = v.Pressed && s.PressedImage is null ? Color.Lerp(Color.White, pressedTint, 0.5f) : Color.White;
                    canvas.Image(picture, Square(v.Center, drawn), shade.WithAlpha(Math.Min(1, op + 0.4f * g)));
                }
                else
                {
                    var fill = Color.Lerp((s.Fill ?? tint.WithAlpha(0.16f)).WithAlpha((s.Fill?.A / 255f ?? 0.16f) * op), pressedTint.WithAlpha(Math.Min(1, 0.45f + op * 0.5f)), g);
                    if (s.Halo && line > 0) Outline(canvas, s, v.Center, drawn + line, line, halo);
                    Fill(canvas, s, v.Center, drawn, fill);
                    if (line > 0) Outline(canvas, s, v.Center, drawn, line * (1 + g), outline);
                }
                if (b.Mode == TouchButtonMode.Toggle && v.Toggled) Outline(canvas, s, v.Center, drawn * 0.72f, Math.Max(1.5f, line), outline);
                if (b.Label is { Length: > 0 } label)
                {
                    var color = s.Label ?? (g > 0.5f ? Color.White : tint);
                    canvas.Text(label, v.Center, drawn * (label.Length > 2 ? 0.42f : 0.62f), color.WithAlpha(Math.Min(1, 0.75f * op + 0.35f + 0.25f * g)));
                }
                break;
            }
            case TouchDPad:
            {
                if (s.Image is { } image)
                    canvas.Image(v.Pressed && s.PressedImage is { } p ? p : image, Square(v.Center, r), Color.White.WithAlpha(Math.Min(1, op + 0.3f * g)));
                else
                {
                    if (s.Halo && line > 0) Outline(canvas, s, v.Center, r + line, line, halo);
                    Fill(canvas, s, v.Center, r, (s.Fill ?? tint).WithAlpha((s.Fill is { } f ? f.A / 255f : 0.14f) * op + 0.06f * g));
                    if (line > 0) Outline(canvas, s, v.Center, r, line, outline);
                }
                if (s.Arrows)
                    foreach (var dir in new[] { InputMap.StickDirection.Left, InputMap.StickDirection.Right, InputMap.StickDirection.Up, InputMap.StickDirection.Down })
                    {
                        bool lit = v.Directions.Contains(dir);
                        var d = Unit(dir);
                        var side = new Vector2(-d.Y, d.X);
                        float tip = lit ? 0.86f : 0.8f, back = 0.48f, half = lit ? 0.26f : 0.21f;
                        var color = lit ? (s.PressedOutline ?? pressedTint).WithAlpha(Math.Min(1, 0.7f + 0.3f * op)) : tint.WithAlpha(0.55f * op + 0.15f);
                        canvas.FillTriangle(v.Center + d * r * tip, v.Center + (d * back + side * half) * r, v.Center + (d * back - side * half) * r, color);
                    }
                if (v.Directions.Count > 0) canvas.FillCircle(v.Center + v.Deflection * r * 0.55f, r * 0.2f, Color.White.WithAlpha(0.85f));
                break;
            }
            case TouchStick stick:
            {
                var origin = v.Origin;
                float idle = v.Held ? 1 : stick.Placement == StickPlacement.Floating ? 0.6f : 1;
                if (s.Image is { } image) canvas.Image(image, Square(origin, r), Color.White.WithAlpha(Math.Min(1, (op + 0.2f * g) * idle)));
                else
                {
                    if (s.Halo && line > 0) Outline(canvas, s, origin, r + line, line, halo);
                    Fill(canvas, s, origin, r, (s.Fill ?? tint).WithAlpha(((s.Fill is { } f ? f.A / 255f : 0.12f) * op + 0.06f * g) * idle));
                    if (line > 0) Outline(canvas, s, origin, r, line, outline.WithAlpha(outline.A / 255f * idle));
                    if (stick.DeadZone > 0) canvas.Ring(origin, r * stick.DeadZone, Math.Max(1, line * 0.6f), tint.WithAlpha(0.3f * op * idle));
                }
                var knob = origin + v.Deflection * r;
                float knobRadius = r * Math.Clamp(s.KnobSize, 0.1f, 1);
                if (s.KnobImage is { } knobImage) canvas.Image(knobImage, Square(knob, knobRadius), Color.White.WithAlpha(Math.Min(1, (0.6f * op + 0.4f) * idle + 0.3f * g)));
                else
                {
                    canvas.FillCircle(knob, knobRadius, Color.Lerp(tint.WithAlpha((0.35f * op + 0.1f) * idle), pressedTint.WithAlpha(0.95f), v.Held ? 0.4f + 0.6f * v.Value.Length() : 0));
                    if (line > 0) canvas.Ring(knob, knobRadius, line, (v.Held ? Color.White : tint).WithAlpha((0.6f * op + 0.3f) * idle));
                }
                // Direction and intensity: an arrow beyond the knob, more opaque the further the stick is pushed.
                float intensity = v.Value.Length();
                if (intensity > 0.01f && v.Deflection != Vector2.Zero)
                {
                    var dir = Vector2.Normalize(v.Deflection);
                    var side = new Vector2(-dir.Y, dir.X);
                    canvas.FillTriangle(origin + dir * r * 1.78f, origin + (dir * 1.5f + side * 0.22f) * r, origin + (dir * 1.5f - side * 0.22f) * r,
                        (s.PressedOutline ?? pressedTint).WithAlpha(0.35f + 0.65f * intensity));
                }
                break;
            }
        }
    }

    private static RectF Square(Vector2 center, float radius) => new(center.X - radius, center.Y - radius, radius * 2, radius * 2);

    private static Vector2 Unit(InputMap.StickDirection d) => d switch
    {
        InputMap.StickDirection.Left => new(-1, 0),
        InputMap.StickDirection.Right => new(1, 0),
        InputMap.StickDirection.Up => new(0, -1),
        _ => new(0, 1),
    };

    private static void Fill(IOverlayCanvas canvas, TouchStyle s, Vector2 center, float radius, Color color)
    {
        if (s.Shape == TouchShape.RoundedSquare) OverlayShapes.FillRoundedRect(canvas, Square(center, radius), radius * Math.Clamp(s.Corner, 0, 1), color);
        else canvas.FillCircle(center, radius, color);
    }

    private static void Outline(IOverlayCanvas canvas, TouchStyle s, Vector2 center, float radius, float thickness, Color color)
    {
        if (s.Shape == TouchShape.RoundedSquare) OverlayShapes.RoundedRectOutline(canvas, Square(center, radius), radius * Math.Clamp(s.Corner, 0, 1), thickness, color);
        else canvas.Ring(center, radius, thickness, color);
    }
}

/// <summary>Shapes built from <see cref="IOverlayCanvas"/> triangles and rectangles, which never overlap, so translucent colours stay even.</summary>
public static class OverlayShapes
{
    private const int CornerSegments = 8;

    /// <summary>Fills a rectangle with rounded corners of <paramref name="corner"/> pixels.</summary>
    public static void FillRoundedRect(IOverlayCanvas canvas, RectF rect, float corner, Color color)
    {
        if (color.A == 0 || rect.Width <= 0 || rect.Height <= 0) return;
        float c = Math.Clamp(corner, 0, Math.Min(rect.Width, rect.Height) / 2);
        if (c < 0.5f) { canvas.FillRect(rect, color); return; }
        canvas.FillRect(new RectF(rect.X + c, rect.Y, rect.Width - 2 * c, rect.Height), color);
        canvas.FillRect(new RectF(rect.X, rect.Y + c, c, rect.Height - 2 * c), color);
        canvas.FillRect(new RectF(rect.Right - c, rect.Y + c, c, rect.Height - 2 * c), color);
        foreach (var (center, start) in Corners(rect, c))
            for (int i = 0; i < CornerSegments; i++)
                canvas.FillTriangle(center, center + Polar(start + i * MathF.PI / 2 / CornerSegments) * c, center + Polar(start + (i + 1) * MathF.PI / 2 / CornerSegments) * c, color);
    }

    /// <summary>The outline of a rounded rectangle, <paramref name="thickness"/> pixels wide, drawn inward.</summary>
    public static void RoundedRectOutline(IOverlayCanvas canvas, RectF rect, float corner, float thickness, Color color)
    {
        if (color.A == 0 || thickness <= 0) return;
        float c = Math.Clamp(corner, 0, Math.Min(rect.Width, rect.Height) / 2), t = Math.Min(thickness, Math.Min(rect.Width, rect.Height) / 2);
        float straight = Math.Max(c, t);
        canvas.FillRect(new RectF(rect.X + straight, rect.Y, rect.Width - 2 * straight, t), color);
        canvas.FillRect(new RectF(rect.X + straight, rect.Bottom - t, rect.Width - 2 * straight, t), color);
        canvas.FillRect(new RectF(rect.X, rect.Y + straight, t, rect.Height - 2 * straight), color);
        canvas.FillRect(new RectF(rect.Right - t, rect.Y + straight, t, rect.Height - 2 * straight), color);
        if (c < t)
        {
            // Corners sharper than the line: square blocks fill them.
            foreach (var corner2 in new[] { new Vector2(rect.X, rect.Y), new Vector2(rect.Right - t, rect.Y), new Vector2(rect.X, rect.Bottom - t), new Vector2(rect.Right - t, rect.Bottom - t) })
                canvas.FillRect(new RectF(corner2.X, corner2.Y, t, t), color);
            return;
        }
        float inner = c - t;
        foreach (var (center, start) in Corners(rect, c))
            for (int i = 0; i < CornerSegments; i++)
            {
                Vector2 d0 = Polar(start + i * MathF.PI / 2 / CornerSegments), d1 = Polar(start + (i + 1) * MathF.PI / 2 / CornerSegments);
                canvas.FillTriangle(center + d0 * c, center + d1 * c, center + d1 * inner, color);
                if (inner > 0) canvas.FillTriangle(center + d0 * c, center + d1 * inner, center + d0 * inner, color);
            }
    }

    private static IEnumerable<(Vector2 Center, float Start)> Corners(RectF r, float c) =>
    [
        (new Vector2(r.X + c, r.Y + c), MathF.PI),
        (new Vector2(r.Right - c, r.Y + c), -MathF.PI / 2),
        (new Vector2(r.Right - c, r.Bottom - c), 0),
        (new Vector2(r.X + c, r.Bottom - c), MathF.PI / 2),
    ];

    private static Vector2 Polar(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));
}
