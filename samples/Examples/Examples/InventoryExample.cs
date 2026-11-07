using System.Numerics;
using MicroEngine;
using MicroEngine.Graphics;
using MicroEngine.Scenes;
using MicroEngine.UI;

namespace Examples;

/// <summary>
/// An inventory: twelve slots in a grid of four columns, the focused slot's description below it. The arrows move
/// between slots by rows and columns; confirm or a click uses one item. The slot is a small widget of the example's own.
/// </summary>
public sealed class InventoryExample : Scene
{
    private static readonly (string Name, int Count, string About)[] Items =
    [
        ("POTION", 3, "RESTORES A LITTLE HEALTH"), ("ETHER", 1, "RESTORES MAGIC"), ("KEY", 1, "OPENS THE OLD GATE"),
        ("ROPE", 2, "CLIMB DOWN A PIT"), ("BOMB", 5, "BREAKS CRACKED WALLS"), ("MAP", 1, "SHOWS THE FLOOR"),
        ("", 0, "EMPTY"), ("", 0, "EMPTY"), ("", 0, "EMPTY"), ("", 0, "EMPTY"), ("", 0, "EMPTY"), ("", 0, "EMPTY"),
    ];

    private Label _about = null!;

    #region grid
    public override void Enter()
    {
        _about = new Label(" ");
        var grid = new Grid(columns: 4);
        foreach (var (name, count, about) in Items)
        {
            var slot = new ItemSlot(name, count);
            slot.Focused += () => _about.Text = about;                // the description follows the focus
            slot.Used += () => _about.Text = slot.Count > 0 ? $"USED {name}, {slot.Count} LEFT" : $"NO {name} LEFT";
            grid.Add(slot);
        }
        Widgets.Content = new Panel(new Stack().Add(grid).Add(_about));
        Widgets.Cancelled += () => Game.Scenes.Pop();
    }
    #endregion

    public override void Update(float dt)
    {
        if (Input.Pressed("pause")) Game.Scenes.Pop();
    }

    public override void Draw(IRenderer r) => Heading.Draw(r, Font, "INVENTORY");
}

#region slot
/// <summary>One inventory slot: the item's name and count in a square box. Confirm or a click uses one.</summary>
public sealed class ItemSlot(string name, int count) : Widget
{
    public string Name { get; } = name;
    public int Count { get; private set; } = count;
    public event Action? Focused;
    public event Action? Used;
    public override bool Focusable => true;

    protected override Vector2 MeasureContent(UiContext context) => new(40 * context.Unit);

    protected override bool OnAction(UiAction action)
    {
        if (action != UiAction.Confirm) return false;
        Use();
        return true;
    }

    protected override void OnClick() => Use();

    protected override void OnFocusChanged(bool focused)
    {
        if (focused) Focused?.Invoke();
    }

    private void Use()
    {
        if (Count == 0) return;
        Count--;
        Used?.Invoke();
    }

    protected override void DrawContent(IRenderer r, UiContext context)
    {
        var theme = context.Theme;
        r.Panel(Bounds, IsFocused ? theme.Panel with { Border = theme.Accent } : theme.Panel);
        if (Count == 0) return;
        var at = new Vector2(Bounds.X + 3 * context.Unit, Bounds.Y + 3 * context.Unit);
        context.Font.Draw(r, Name, at, IsFocused ? theme.Accent : theme.Text);
        string n = $"x{Count}";
        context.Font.Draw(r, n, new Vector2(Bounds.Right - 3 * context.Unit - context.Font.MeasureWidth(n), Bounds.Bottom - 3 * context.Unit - context.Font.LineHeight), theme.Muted);
    }
}
#endregion
