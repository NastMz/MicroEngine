using MicroEngine.Input;
using MicroEngine.Kernel;

namespace MicroEngine.UI;

/// <summary>
/// The cursor of a list a scene draws itself: a title menu, a bag, a shop, a two-by-two battle menu. It keeps which
/// option is highlighted, moves it with the direction actions (wrapping or stopping at the ends, in one column or a
/// grid), chooses with confirm, lets the cursor rest on an option that cannot be chosen, and gives the first row to
/// show of a list longer than its box. It holds no labels and draws nothing: the scene draws its rows in its own look
/// and reads <see cref="Index"/>. For a menu with layout, the mouse and touch built in, use widgets instead
/// (<see cref="Button"/> in a <see cref="Stack"/>, with a <see cref="ControlSkin"/> for the look); with a cursor, the
/// scene maps its own pointer through <see cref="MoveTo"/>.
/// </summary>
/// <example>
/// <code>
/// private readonly MenuCursor _menu = new(3);          // RESUME, OPTIONS, QUIT
///
/// public override void Enter() =&gt; _menu.Moved += () =&gt; Audio.TryPlay("move");
///
/// public override void Update(float dt)
/// {
///     if (_menu.Update(Input) is { } chosen) Choose(chosen);
/// }
///
/// public override void Draw(IRenderer r)
/// {
///     for (int i = 0; i &lt; Labels.Length; i++)
///         Font.Draw(r, Labels[i], new Vector2(120, 60 + i * 12), i == _menu.Index ? Gold : Cream);
/// }
/// </code>
/// </example>
public sealed class MenuCursor : IStateHashed
{
    private readonly HashSet<int> _disabled = [];
    private int _count, _columns, _index, _top;

    /// <summary>A cursor on the first of <paramref name="count"/> options, in rows of <paramref name="columns"/> (1: a vertical list).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative, or <paramref name="columns"/> is less than 1.</exception>
    public MenuCursor(int count, int columns = 1)
    {
        Count = count;
        Columns = columns;
    }

    /// <summary>How many options the list has; the cursor stays inside when it shrinks. Zero is an empty list: nothing moves or is chosen.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int Count
    {
        get => _count;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _count = value;
            _index = Math.Clamp(_index, 0, Math.Max(0, value - 1));
        }
    }

    /// <summary>
    /// How many options a row has (1 by default: a vertical list, moved by up and down). With more, left and right
    /// move within the row and up and down between rows; a cell that does not exist in a last, shorter row is never reached.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1.</exception>
    public int Columns
    {
        get => _columns;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _columns = value;
        }
    }

    /// <summary>The highlighted option; a value outside the list is clamped to its first or last. Setting it raises nothing.</summary>
    public int Index { get => _index; set => _index = Math.Clamp(value, 0, Math.Max(0, _count - 1)); }

    /// <summary>Whether moving past an end goes on from the other end of the column or row (true by default).</summary>
    public bool Wrap { get; set; } = true;

    /// <summary>Raised when a direction or <see cref="MoveTo"/> changes the highlighted option (for the cursor's sound).</summary>
    public event Action? Moved;

    /// <summary>
    /// Says whether the option at <paramref name="index"/> can be chosen. The cursor still rests on one that cannot:
    /// <see cref="Update"/> then leaves the confirm press to the scene, which can answer with a refusal sound.
    /// </summary>
    public void SetEnabled(int index, bool enabled) { if (enabled) _disabled.Remove(index); else _disabled.Add(index); }

    /// <summary>Whether the option at <paramref name="index"/> can be chosen.</summary>
    public bool IsEnabled(int index) => !_disabled.Contains(index);

    /// <summary>
    /// Puts the cursor on <paramref name="index"/> as a move of the player's (the row the pointer is over): raises
    /// <see cref="Moved"/> when it changes. False when it is already there or is not an option.
    /// </summary>
    public bool MoveTo(int index)
    {
        if (index == _index || (uint)index >= (uint)_count) return false;
        _index = index;
        Moved?.Invoke();
        return true;
    }

    /// <summary>Moves the cursor one option in <paramref name="direction"/>; false when it did not move (an end without <see cref="Wrap"/>, a single option, confirm or cancel).</summary>
    public bool Move(UiAction direction)
    {
        if (_count <= 1) return false;
        int column = _index % _columns, row = _index / _columns, rows = (_count + _columns - 1) / _columns;
        int next = _index;
        switch (direction)
        {
            case UiAction.Left or UiAction.Right:
            {
                int width = Math.Min(_columns, _count - row * _columns);      // the last row may be shorter
                int to = column + (direction == UiAction.Right ? 1 : -1);
                if (Wrap) to = (to + width) % width;
                else if (to < 0 || to >= width) return false;
                next = row * _columns + to;
                break;
            }
            case UiAction.Up or UiAction.Down:
            {
                int height = column < _count - (rows - 1) * _columns ? rows : rows - 1;   // rows that have this column
                int to = row + (direction == UiAction.Down ? 1 : -1);
                if (Wrap) to = (to + height) % height;
                else if (to < 0 || to >= height) return false;
                next = to * _columns + column;
                break;
            }
            default:
                return false;
        }
        return MoveTo(next);
    }

    /// <summary>
    /// Moves the cursor by the direction actions pressed in this update: left and right first (in a grid), else up and
    /// down; opposite directions pressed together cancel out. It reads presses, so a held direction moves once: there
    /// is no repeat. Actions left null in <paramref name="actions"/> (or all
    /// of them, by default) are the input map's own names. Returns whether it moved.
    /// </summary>
    public bool Move(InputMap input, UiActions? actions = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        actions ??= UiActions.Default;
        var names = input.Names;
        if (_columns > 1)
        {
            int dx = (input.Pressed(actions.NameOf(UiAction.Right, names)) ? 1 : 0) - (input.Pressed(actions.NameOf(UiAction.Left, names)) ? 1 : 0);
            if (dx != 0 && Move(dx > 0 ? UiAction.Right : UiAction.Left)) return true;
        }
        int dy = (input.Pressed(actions.NameOf(UiAction.Down, names)) ? 1 : 0) - (input.Pressed(actions.NameOf(UiAction.Up, names)) ? 1 : 0);
        return dy != 0 && Move(dy > 0 ? UiAction.Down : UiAction.Up);
    }

    /// <summary>
    /// One update of the menu: moves the cursor (<see cref="Move(InputMap, UiActions?)"/>), then returns the highlighted
    /// option when confirm is pressed on one that can be chosen, else null. The press that chooses is consumed
    /// (<see cref="InputMap.Consume"/>), so nothing read after the menu acts on it too; a confirm on an option that
    /// cannot be chosen is left as it is.
    /// </summary>
    public int? Update(InputMap input, UiActions? actions = null)
    {
        Move(input, actions);
        if (_count == 0) return null;
        string confirm = (actions ?? UiActions.Default).NameOf(UiAction.Confirm, input.Names);
        if (!input.Pressed(confirm) || !IsEnabled(_index)) return null;
        input.Consume(confirm);
        return _index;
    }

    /// <summary>
    /// The first row to draw of a list that shows <paramref name="visible"/> rows at a time: it moves just enough to
    /// keep the cursor's row in the window, as a scrolling list does, and never leaves blank rows under the last one
    /// when the list has enough rows to fill the window (also after it shrinks). Call it with the same count every frame.
    /// </summary>
    public int Top(int visible)
    {
        visible = Math.Max(1, visible);
        int row = _index / _columns, rows = (_count + _columns - 1) / _columns;
        _top = Math.Clamp(_top, Math.Max(0, row - visible + 1), row);
        _top = Math.Min(_top, Math.Max(0, rows - visible));
        return _top;
    }

    /// <summary>Adds the highlighted option, the list's shape, the window and the options that cannot be chosen to a state hash.</summary>
    public void HashState(StateHasher hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        hash.Add(_index);
        hash.Add(_count);
        hash.Add(_columns);
        hash.Add(_top);
        hash.Add(Wrap);
        int disabled = 0;
        foreach (int i in _disabled) disabled ^= unchecked(i * 16777619 + 1);    // a set: the order does not count
        hash.Add(_disabled.Count);
        hash.Add(disabled);
    }
}
