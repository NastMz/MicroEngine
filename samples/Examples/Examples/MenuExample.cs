using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// A title menu built on the widget tree: a column of items with a bobbing cursor beside the focused one, a blip when
/// the cursor moves, an option that cannot be chosen yet, and the mouse moving the cursor as it passes over an item.
/// The item is a small widget of the example's own; its look and its sound are the game's.
/// </summary>
public sealed class MenuExample : Scene
{
    private Widget? _hovered;

    /// <summary>The items, top to bottom, for tests.</summary>
    public MenuItem[] Items { get; private set; } = [];
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
            new MenuItem("NEW GAME", () => Result.Text = "A NEW ADVENTURE BEGINS"),
            new MenuItem("CONTINUE", () => { }) { Enabled = false },          // no save yet: muted, and the cursor skips it
            new MenuItem("OPTIONS", () => Result.Text = "OPTIONS WOULD OPEN HERE"),
            new MenuItem("QUIT", () => Game.Scenes.Pop()),
        ];
        var column = new Stack { Spacing = 3, Align = Align.Start };
        foreach (var item in Items) column.Add(item);
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
}

#region item
/// <summary>A menu line: its text, and a cursor that bobs beside it while it has the focus. Confirm or a click chooses it.</summary>
public sealed class MenuItem(string text, Action chosen) : Widget
{
    public string Text { get; } = text;
    public override bool Focusable => true;

    protected override Vector2 MeasureContent(UiContext context) =>
        new(context.Font.MeasureWidth(Text) + 10 * context.Unit, context.Font.LineHeight);

    protected override bool OnAction(UiAction action)
    {
        if (action != UiAction.Confirm) return false;
        chosen();
        return true;
    }

    protected override void OnClick() => chosen();

    protected override void DrawContent(IRenderer r, UiContext context)
    {
        var theme = context.Theme;
        float u = context.Unit;
        var color = !IsEnabledInTree ? theme.Muted : IsFocused ? theme.Accent : theme.Text;
        if (IsFocused)
        {
            float bob = MathF.Round(MathX.Wave(context.Time, 2f, 1f) * u);
            context.Font.Draw(r, "▶", new Vector2(Bounds.X + bob, Bounds.Y), theme.Accent);
        }
        context.Font.DrawShadowed(r, Text, new Vector2(Bounds.X + 10 * u, Bounds.Y), color, theme.TextShadow, new Vector2(u));
    }
}
#endregion
