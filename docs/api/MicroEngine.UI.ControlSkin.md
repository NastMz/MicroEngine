# Delegate ControlSkin

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

Draws a control in the game's own look: everything the control would draw itself (a button's box and label, a
panel's box), from its <code class="paramref">state</code>. Set on one control ([Skin](MicroEngine.UI.Button.md#MicroEngine_UI_Button_Skin),
[Skin](MicroEngine.UI.Panel.md#MicroEngine_UI_Panel_Skin)) or on a theme for every control of a root ([ButtonSkin](MicroEngine.UI.UiTheme.md#MicroEngine_UI_UiTheme_ButtonSkin),
[PanelSkin](MicroEngine.UI.UiTheme.md#MicroEngine_UI_UiTheme_PanelSkin)). The control keeps its behaviour, its size and its place in the layout; the theme in
<code class="paramref">context</code> is already faded by the opacity around the control, and a colour of the skin's own goes
through [Fade](MicroEngine.UI.UiContext.md#MicroEngine_UI_UiContext_Fade_MicroEngine_Color_).

```csharp
public delegate void ControlSkin(IRenderer renderer, in UiContext context, in ControlState state)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer of the view the control is drawn in.

`context` [UiContext](MicroEngine.UI.UiContext.md)

The font, the theme, the layout unit and the root's clock.

`state` [ControlState](MicroEngine.UI.ControlState.md)

The control's bounds, text and state.

## Examples

A menu row drawn as a bar, an underline and a cursor that bobs, with no box:

```csharp
static void Row(IRenderer r, in UiContext ui, in ControlState s)
{
    if (s.Focused)
    {
        r.FillRect(s.Bounds, ui.Theme.Accent.WithAlpha(0.22f));
        r.FillRect(new RectF(s.Bounds.X, s.Bounds.Bottom - 1, s.Bounds.Width, 1), ui.Theme.Accent);
        ui.Font.Draw(r, ">", new Vector2(s.Bounds.X + 4 + (int)(s.FocusTime * 4) % 2, s.Bounds.Y + 2), ui.Theme.Accent);
    }
    ui.Font.Draw(r, s.Text, new Vector2(s.Bounds.X + 14, s.Bounds.Y + 2), s.Enabled ? ui.Theme.Text : ui.Theme.Muted);
}
```

</div>
