using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// A title menu built on the widget tree: a column of items with a bobbing cursor beside the focused one, a blip when
/// the cursor moves, an option that cannot be chosen yet, and the mouse moving the cursor as it passes over an item.
/// The items are buttons drawn by a skin of the example's own; their look and their sound are the game's.
/// </summary>
public sealed class MenuExample : Scene
{
    private Widget? _hovered;

    /// <summary>The items, top to bottom, for tests.</summary>
    public Button[] Items { get; private set; } = [];
    /// <summary>What the last chosen item said, for tests.</summary>
    public Label Result { get; private set; } = null!;
    /// <summary>How many times the cursor moved, for tests.</summary>
    public int Moves { get; private set; }

    #region menu
    public override void Enter()
    {
        Result = new Label();
        Items =
        [
            new Button("NEW GAME", () => Result.Text = "A NEW ADVENTURE BEGINS"),
            new Button("CONTINUE") { Enabled = false },                       // no save yet: muted, and the cursor skips it
            new Button("OPTIONS", () => Result.Text = "OPTIONS WOULD OPEN HERE"),
            new Button("QUIT", () => Game.Scenes.Pop()),
        ];
        var column = new Stack { Spacing = 3, Align = Align.Start };
        foreach (var item in Items)
        {
            item.Skin = MenuLine;                                             // the game's look for a line of the menu
            item.Padding = new Vector2(5, 0);                                 // as tall as its text, room for the cursor
            column.Add(item);
        }
        Widgets.Content = new Stack { Spacing = 12 }.Add(column).Add(Result);
        Widgets.FocusChanged += change =>
        {
            if (change.From is null) return;                                 // the first focus is not a move
            Moves++;
            Audio.TryPlay("select", 0.3f);                                   // a blip, if the game registered one
        };
    }

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) { Game.Scenes.Pop(); return; }
        // The cursor follows the mouse: when the pointer reaches another item, that item takes the focus.
        var hovered = Widgets.Hovered;
        if (!ReferenceEquals(hovered, _hovered) && hovered is { Focusable: true }) Widgets.Focus(hovered);
        _hovered = hovered;
    }
    #endregion

    public override void Draw(IRenderer r) => Heading.Draw(r, Font, "MENU");

    #region item
    /// <summary>
    /// A line of the menu: its text, and a cursor that bobs beside it while it has the focus. A skin draws all of a
    /// button from its state; the button keeps the focus, confirm and the click.
    /// </summary>
    private static void MenuLine(IRenderer r, in UiContext context, in ControlState state)
    {
        var theme = context.Theme;
        float u = context.Unit;
        var color = !state.Enabled ? theme.Muted : state.Focused ? theme.Accent : theme.Text;
        if (state.Focused)
        {
            float bob = MathF.Round(MathX.Wave(context.Time, 2f, 1f) * u);
            context.Font.Draw(r, "▶", new Vector2(state.Bounds.X + bob, state.Bounds.Y), theme.Accent);
        }
        context.Font.DrawShadowed(r, state.Text, new Vector2(state.Bounds.X + 10 * u, state.Bounds.Y), color, theme.TextShadow, new Vector2(u));
    }
    #endregion
}
