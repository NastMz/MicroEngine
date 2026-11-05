using System.Numerics;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;

namespace MicroEngine.UI;

/// <summary>
/// One line of editable text: a name, a seed, a chat message. While it has the focus it turns the input system's text
/// channel on (the on-screen keyboard on a phone, the input method's candidates next to the caret), inserts what is
/// typed at the caret, shows what an input method is still composing, and takes the keyboard from gameplay: Backspace,
/// Delete, the arrows, Home and End edit, Enter submits, Escape leaves the field, Tab and the up and down keys move the
/// focus. A gamepad edits nothing but moves the caret with left and right and submits with confirm.
/// </summary>
public sealed class TextField : Widget
{
    private string _text = "";
    private int _caret;

    /// <summary>The text; setting it cuts it to <see cref="MaxLength"/> and puts the caret at its end.</summary>
    public string Text
    {
        get => _text;
        set
        {
            var text = Cut(value ?? "");
            if (text == _text) { _caret = Math.Min(_caret, _text.Length); return; }
            _text = text;
            _caret = _text.Length;
            Changed?.Invoke(_text);
        }
    }

    /// <summary>Where new text goes, as an index into <see cref="Text"/>.</summary>
    public int Caret { get => _caret; set => _caret = Math.Clamp(value, 0, _text.Length); }
    /// <summary>The text an input method is composing and has not committed yet (empty when none); drawn underlined at the caret.</summary>
    public string Composition { get; private set; } = "";
    /// <summary>Shown in the muted colour while the field is empty and does not have the focus.</summary>
    public string Placeholder { get; set; } = "";
    /// <summary>The most characters (UTF-16 code units) the text may have (64 by default).</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive.</exception>
    public int MaxLength
    {
        get => _maxLength;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _maxLength = value;
            if (_text.Length > value) Text = _text;
        }
    }
    private int _maxLength = 64;
    /// <summary>The width, in layout units, the field measures when <see cref="Widget.Width"/> is not set (96 by default).</summary>
    public float Columns { get; set; } = 96;
    /// <summary>Raised with the new text whenever it changes.</summary>
    public event Action<string>? Changed;
    /// <summary>Raised with the text when Enter or confirm submits it.</summary>
    public event Action<string>? Submitted;
    /// <inheritdoc/>
    public override bool Focusable => true;

    private string Cut(string text) => text.Length <= _maxLength ? text : text[.._maxLength];

    /// <summary>Inserts <paramref name="text"/> at the caret, as far as <see cref="MaxLength"/> allows, and moves the caret after it.</summary>
    public void Insert(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        int room = _maxLength - _text.Length;
        if (room <= 0) return;
        if (text.Length > room) text = text[..room];
        _text = _text.Insert(_caret, text);
        _caret += text.Length;
        Changed?.Invoke(_text);
    }

    private void Delete(int from, int count)
    {
        if (count <= 0 || from < 0 || from + count > _text.Length) return;
        _text = _text.Remove(from, count);
        _caret = from;
        Changed?.Invoke(_text);
    }

    private void Submit() => Submitted?.Invoke(_text);

    /// <summary>A key the root took for the field while it has the focus; returns whether the field used it.</summary>
    internal bool OnKey(Key key)
    {
        switch (key)
        {
            case Key.Backspace: if (_caret > 0) Delete(_caret - 1, 1); return true;
            case Key.Delete: if (_caret < _text.Length) Delete(_caret, 1); return true;
            case Key.Left: Caret--; return true;
            case Key.Right: Caret++; return true;
            case Key.Home: Caret = 0; return true;
            case Key.End: Caret = _text.Length; return true;
            case Key.Enter or Key.KeypadEnter: Submit(); return true;
            default: return false;
        }
    }

    /// <inheritdoc/>
    protected internal override bool OnAction(UiAction action)
    {
        int before = _caret;
        switch (action)
        {
            case UiAction.Left: Caret--; return _caret != before;
            case UiAction.Right: Caret++; return _caret != before;
            case UiAction.Confirm: Submit(); return true;
            default: return false;
        }
    }

    /// <inheritdoc/>
    protected internal override void OnFocusChanged(bool focused)
    {
        if (Root is not { } root) return;
        if (focused) root.StartText(this);
        else
        {
            Composition = "";
            root.StopText(this);
        }
    }

    /// <inheritdoc/>
    protected internal override void Update(float dt)
    {
        if (!IsFocused || Root?.TextInput is not { } text) return;
        Insert(text.Typed);
        Composition = text.Composition;
    }

    /// <inheritdoc/>
    protected internal override void HashState(StateHasher hash)
    {
        hash.Add(_text);
        hash.Add(_caret);
    }

    /// <inheritdoc/>
    protected override Vector2 MeasureContent(UiContext context) =>
        new(Columns * context.Unit + 8 * context.Unit, context.Font.LineHeight + 6 * context.Unit);

    /// <inheritdoc/>
    protected override void DrawContent(IRenderer renderer, UiContext context)
    {
        var theme = context.Theme;
        float u = context.Unit;
        bool focused = IsFocused && IsEnabledInTree;
        renderer.Panel(Bounds, focused ? theme.Panel with { Border = theme.Accent } : theme.Panel);
        var font = context.Font;
        float left = Bounds.X + 4 * u, top = MathF.Round(Bounds.Y + (Bounds.Height - font.LineHeight) / 2), room = Bounds.Width - 8 * u;
        if (_text.Length == 0 && Composition.Length == 0 && !focused)
        {
            font.Draw(renderer, Placeholder, new Vector2(left, top), theme.Muted);
            return;
        }
        string before = _text[.._caret], after = _text[_caret..];
        float caretX = font.MeasureWidth(before + Composition);
        // Scrolls so the caret stays inside the field.
        float scroll = MathF.Max(0, caretX - room + u);
        renderer.SetClip(new RectF(left, Bounds.Y, room, Bounds.Height));
        float x = left - scroll;
        var color = IsEnabledInTree ? theme.Text : theme.Muted;
        font.Draw(renderer, before, new Vector2(x, top), color);
        x += font.MeasureWidth(before);
        if (Composition.Length > 0)
        {
            float w = font.MeasureWidth(Composition);
            font.Draw(renderer, Composition, new Vector2(x, top), theme.Accent);
            renderer.FillRect(new RectF(x, top + font.LineHeight, w, u), theme.Accent);
            x += w;
        }
        font.Draw(renderer, after, new Vector2(x, top), color);
        if (focused && (int)(context.Time * 2) % 2 == 0)
            renderer.FillRect(new RectF(left - scroll + caretX, top - u, u, font.LineHeight + 2 * u), theme.Accent);
        renderer.SetClip(null);
    }
}
