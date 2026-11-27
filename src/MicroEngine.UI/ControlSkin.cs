using MicroEngine.Graphics;

namespace MicroEngine.UI;

/// <summary>The state a control is in when it is drawn, as its skin receives it (<see cref="ControlSkin"/>).</summary>
/// <param name="Bounds">Where the control is, in the root's units (canvas pixels for a root on the canvas).</param>
/// <param name="Text">The control's text in the language in use: a button's label; empty for a panel.</param>
/// <param name="Enabled">Whether the control and every widget around it are enabled.</param>
/// <param name="Focused">Whether the control has the focus: the cursor of a menu is on it.</param>
/// <param name="Hovered">Whether the pointer is over the control.</param>
/// <param name="Pressed">Whether a pointer press that started on the control is held.</param>
/// <param name="FocusTime">
/// Seconds the control has had the focus, on the root's clock; zero without it. A cursor that bobs from where it
/// starts, or a highlight that grows in, reads it; <see cref="UiContext.Time"/> is the clock that never starts again.
/// </param>
public readonly record struct ControlState(RectF Bounds, string Text, bool Enabled, bool Focused, bool Hovered, bool Pressed, float FocusTime);

/// <summary>
/// Draws a control in the game's own look: everything the control would draw itself (a button's box and label, a
/// panel's box), from its <paramref name="state"/>. Set on one control (<see cref="Button.Skin"/>,
/// <see cref="Panel.Skin"/>) or on a theme for every control of a root (<see cref="UiTheme.ButtonSkin"/>,
/// <see cref="UiTheme.PanelSkin"/>). The control keeps its behaviour, its size and its place in the layout; the theme in
/// <paramref name="context"/> is already faded by the opacity around the control, and a colour of the skin's own goes
/// through <see cref="UiContext.Fade(Color)"/>.
/// </summary>
/// <example>
/// A menu row drawn as a bar, an underline and a cursor that bobs, with no box:
/// <code>
/// static void Row(IRenderer r, in UiContext ui, in ControlState s)
/// {
///     if (s.Focused)
///     {
///         r.FillRect(s.Bounds, ui.Theme.Accent.WithAlpha(0.22f));
///         r.FillRect(new RectF(s.Bounds.X, s.Bounds.Bottom - 1, s.Bounds.Width, 1), ui.Theme.Accent);
///         ui.Font.Draw(r, "&gt;", new Vector2(s.Bounds.X + 4 + (int)(s.FocusTime * 4) % 2, s.Bounds.Y + 2), ui.Theme.Accent);
///     }
///     ui.Font.Draw(r, s.Text, new Vector2(s.Bounds.X + 14, s.Bounds.Y + 2), s.Enabled ? ui.Theme.Text : ui.Theme.Muted);
/// }
/// </code>
/// </example>
/// <param name="renderer">The renderer of the view the control is drawn in.</param>
/// <param name="context">The font, the theme, the layout unit and the root's clock.</param>
/// <param name="state">The control's bounds, text and state.</param>
public delegate void ControlSkin(IRenderer renderer, in UiContext context, in ControlState state);
