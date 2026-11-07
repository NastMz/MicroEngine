using System.Numerics;
using System.Text;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;

namespace MicroEngine.UI;

/// <summary>
/// One line of editable text: a name, a seed, a chat message. While it has the focus it turns the input system's text
/// channel on (the on-screen keyboard on a phone, the input method's candidates next to the caret), inserts what is
/// typed at the caret, shows what an input method is still composing, and takes the keyboard from gameplay: Backspace,
/// Delete, the arrows, Home and End edit, Enter submits, Escape leaves the field, Tab and the up and down keys move the
/// focus. Shift with the arrows, Home or End selects, as does dragging the pointer across the text; Ctrl (Command on
/// macOS) with A selects everything, and with C, X and V copies, cuts and pastes through the root's
/// <see cref="UiRoot.Clipboard"/>. A gamepad edits nothing but moves the caret with left and right and submits with confirm.
/// </summary>
public sealed class TextField : Widget
{
    private string _text = "";
    private int _caret, _anchor;
    private float _textX;          // where the first character was last drawn (scroll included), UI units
    private IFont? _font;          // the font it was last drawn with
    private bool _selecting;       // a pointer press is dragging the selection

    /// <summary>The text; setting it cuts it to <see cref="MaxLength"/>, puts the caret at its end and ends any selection.</summary>
    public string Text
    {
        get => _text;
        set
        {
            var text = Limit(value ?? "");
            if (text == _text) { _caret = _anchor = Math.Min(_caret, _text.Length); return; }
            _text = text;
            _caret = _anchor = _text.Length;
            Changed?.Invoke(_text);
        }
    }

    /// <summary>Where new text goes, as an index into <see cref="Text"/>; setting it ends any selection.</summary>
    public int Caret { get => _caret; set => _caret = _anchor = Math.Clamp(value, 0, _text.Length); }
    /// <summary>Where the selection starts, as an index into <see cref="Text"/>: the end opposite the caret.</summary>
    public int SelectionStart => Math.Min(_anchor, _caret);
    /// <summary>How many characters (UTF-16 code units) are selected; 0 when none are.</summary>
    public int SelectionLength => Math.Abs(_caret - _anchor);
    /// <summary>Whether any text is selected.</summary>
    public bool HasSelection => _caret != _anchor;
    /// <summary>The selected text, or "" when none is.</summary>
    public string SelectedText => _text.Substring(SelectionStart, SelectionLength);
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

    private string Limit(string text) => text.Length <= _maxLength ? text : text[.._maxLength];

    /// <summary>
    /// Selects the text between <paramref name="anchor"/> and <paramref name="caret"/> (indices into <see cref="Text"/>,
    /// clamped to it); the caret goes to <paramref name="caret"/>, so the selection grows from there.
    /// </summary>
    public void Select(int anchor, int caret)
    {
        _anchor = Math.Clamp(anchor, 0, _text.Length);
        _caret = Math.Clamp(caret, 0, _text.Length);
    }

    /// <summary>Selects the whole text, with the caret at its end.</summary>
    public void SelectAll() => Select(0, _text.Length);

    /// <summary>
    /// Inserts <paramref name="text"/> at the caret, in place of the selection when there is one, as far as
    /// <see cref="MaxLength"/> allows, and moves the caret after it.
    /// </summary>
    public void Insert(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        bool replaced = RemoveSelection();
        int room = _maxLength - _text.Length;
        if (room > 0)
        {
            if (text.Length > room) text = text[..room];
            _text = _text.Insert(_caret, text);
            _caret = _anchor = _caret + text.Length;
        }
        else if (!replaced) return;
        Changed?.Invoke(_text);
    }

    /// <summary>Puts the selected text on the root's clipboard; false when nothing is selected or there is no clipboard.</summary>
    public bool Copy() => HasSelection && Root?.Clipboard is { } clipboard && clipboard.SetText(SelectedText);

    /// <summary>Puts the selected text on the root's clipboard and deletes it; false (and nothing deleted) when it could not copy.</summary>
    public bool Cut()
    {
        if (!Copy()) return false;
        RemoveSelection();
        Changed?.Invoke(_text);
        return true;
    }

    /// <summary>
    /// Inserts the root's clipboard text at the caret, in place of the selection, as one line (line breaks and tabs
    /// become spaces) cut to <see cref="MaxLength"/>; false when there is no clipboard or it holds no text.
    /// </summary>
    public bool Paste()
    {
        if (Root?.Clipboard?.GetText() is not { Length: > 0 } text) return false;
        var line = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n') continue; // CR LF is one break
            line.Append(char.IsControl(text[i]) ? ' ' : text[i]);
        }
        Insert(line.ToString());
        return true;
    }

    /// <summary>Deletes the selected text, leaving the caret where it was; returns whether there was any (Changed is the caller's).</summary>
    private bool RemoveSelection()
    {
        if (!HasSelection) return false;
        int start = SelectionStart;
        _text = _text.Remove(start, SelectionLength);
        _caret = _anchor = start;
        return true;
    }

    private void Delete(int from, int count)
    {
        if (RemoveSelection())
        {
            Changed?.Invoke(_text);
            return;
        }
        if (count <= 0 || from < 0 || from + count > _text.Length) return;
        _text = _text.Remove(from, count);
        _caret = _anchor = from;
        Changed?.Invoke(_text);
    }

    /// <summary>Moves the caret to <paramref name="to"/>; with <paramref name="extend"/> the selection grows or shrinks with it, otherwise it ends.</summary>
    private void MoveTo(int to, bool extend)
    {
        _caret = Math.Clamp(to, 0, _text.Length);
        if (!extend) _anchor = _caret;
    }

    /// <summary>One step left or right: without Shift, a selection ends at that side instead.</summary>
    private void Step(int direction, bool extend)
    {
        if (!extend && HasSelection) MoveTo(direction < 0 ? SelectionStart : SelectionStart + SelectionLength, false);
        else MoveTo(_caret + direction, extend);
    }

    private void Submit() => Submitted?.Invoke(_text);

    /// <summary>A key the root took for the field while it has the focus, with the modifiers held; returns whether the field used it.</summary>
    internal bool OnKey(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        bool shift = (modifiers & KeyModifiers.Shift) != 0;
        if ((modifiers & (KeyModifiers.Control | KeyModifiers.Command)) != 0)
        {
            switch (key)
            {
                case Key.A: SelectAll(); return true;
                case Key.C: Copy(); return true;
                case Key.X: Cut(); return true;
                case Key.V: Paste(); return true;
            }
        }
        switch (key)
        {
            case Key.Backspace: Delete(_caret - 1, 1); return true;
            case Key.Delete: Delete(_caret, 1); return true;
            case Key.Left: Step(-1, shift); return true;
            case Key.Right: Step(1, shift); return true;
            case Key.Home: MoveTo(0, shift); return true;
            case Key.End: MoveTo(_text.Length, shift); return true;
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
            case UiAction.Left: Step(-1, false); return _caret != before || HasSelection;
            case UiAction.Right: Step(1, false); return _caret != before || HasSelection;
            case UiAction.Confirm: Submit(); return true;
            default: return false;
        }
    }

    /// <summary>The index between characters nearest to <paramref name="x"/> (UI units), by where the text was last drawn.</summary>
    private int IndexAt(float x)
    {
        if (_font is not { } font) return _caret;
        float previous = 0;
        for (int i = 1; i <= _text.Length; i++)
        {
            float width = font.MeasureWidth(_text.AsSpan(0, i).ToString());
            if (x - _textX < (previous + width) / 2) return i - 1;
            previous = width;
        }
        return _text.Length;
    }

    /// <inheritdoc/>
    protected internal override void OnPointerDown(Vector2 position)
    {
        MoveTo(IndexAt(position.X), false);
        _selecting = true;
    }

    /// <inheritdoc/>
    protected internal override void OnPointerMove(Vector2 position)
    {
        if (_selecting) MoveTo(IndexAt(position.X), true);
    }

    /// <inheritdoc/>
    protected internal override void OnPointerUp(Vector2 position)
    {
        if (_selecting) MoveTo(IndexAt(position.X), true);
        _selecting = false;
    }

    /// <inheritdoc/>
    protected internal override void OnPointerCancel() => _selecting = false;

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
        hash.Add(_anchor);
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
        var font = _font = context.Font;
        float left = Bounds.X + 4 * u, top = MathF.Round(Bounds.Y + (Bounds.Height - font.LineHeight) / 2), room = Bounds.Width - 8 * u;
        _textX = left;
        if (_text.Length == 0 && Composition.Length == 0 && !focused)
        {
            font.Draw(renderer, Placeholder, new Vector2(left, top), theme.Muted);
            return;
        }
        string before = _text[.._caret], after = _text[_caret..];
        float caretX = font.MeasureWidth(before + Composition);
        // Scrolls so the caret stays inside the field.
        float scroll = MathF.Max(0, caretX - room + u);
        _textX = left - scroll;
        // Within any clip of the containers around the field (a scrolling list), restored after.
        using var clip = context.ClipTo(renderer, new RectF(left, Bounds.Y, room, Bounds.Height), out _);
        if (focused && HasSelection && Composition.Length == 0)
        {
            float from = font.MeasureWidth(_text[..SelectionStart]), to = font.MeasureWidth(_text[..(SelectionStart + SelectionLength)]);
            renderer.FillRect(new RectF(_textX + from, top - u, to - from, font.LineHeight + 2 * u), theme.Accent.WithAlpha(0.35f));
        }
        float x = _textX;
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
            renderer.FillRect(new RectF(_textX + caretX, top - u, u, font.LineHeight + 2 * u), theme.Accent);
    }
}
