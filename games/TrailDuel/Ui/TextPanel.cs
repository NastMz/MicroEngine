using System.Collections;
using System.Numerics;
using MicroEngine;
using MicroEngine.Audio;
using MicroEngine.Graphics;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.UI;
using TrailDuel.Visuals;

namespace TrailDuel.Ui;

/// <summary>
/// The message box used everywhere: typewriter reveal with soft blips, automatic paging at two
/// lines, a speaker name tag and a bouncing "more" arrow. Coroutines await <see cref="Say"/>.
/// </summary>
/// <remarks>
/// The game's own box rather than one built on the engine's widgets: battle menus sit under a last page that stays up
/// (<see cref="WaitForConfirmOnLast"/>), battle narration closes by itself (<see cref="AutoAdvance"/>), cancel also turns
/// a typed page, and the box, name tag and bobbing arrow have the game's own look.
/// </remarks>
internal sealed class TextPanel : IStateHashed
{
    private readonly BitmapFont _font;
    private readonly AudioMixer _audio;
    private readonly DemoKit.Typewriter _typewriter = new() { CharactersPerSecond = 60 };
    private readonly Queue<string> _pages = new();
    private float _time;
    private int _blips;

    public const int Lines = 2;
    public RectF Area { get; set; } = new(4, 134, 312, 42);
    public string? Speaker { get; private set; }
    public bool Active { get; private set; }
    /// <summary>When false the last page stays up until code calls <see cref="Close"/> (battle menus sit under it).</summary>
    public bool WaitForConfirmOnLast { get; set; } = true;
    public bool PageComplete => Active && _typewriter.Complete;
    public bool Silent { get; set; }
    /// <summary>When above zero, a fully typed last page closes by itself after this many seconds.</summary>
    public float AutoAdvance { get; set; }
    private float _completeTime;

    public TextPanel(BitmapFont font, AudioMixer audio)
    {
        _font = font;
        _audio = audio;
        _typewriter.Revealed += _ =>
        {
            if (Silent || _blips++ % 3 != 0) return;
            _audio.Play("blip", 0.35f, 1f, 0.06f);
        };
    }

    public void Show(string text, string? speaker = null)
    {
        Speaker = speaker;
        _pages.Clear();
        var lines = _font.Wrap(text, (int)Area.Width - 20);
        for (int i = 0; i < lines.Count; i += Lines)
            _pages.Enqueue(string.Join("\n", lines.Skip(i).Take(Lines)));
        Next();
    }

    private void Next()
    {
        if (_pages.Count == 0) { Close(); return; }
        _typewriter.Start(_pages.Dequeue());
        Active = true;
        _time = 0;
    }

    public void Close()
    {
        Active = false;
        _pages.Clear();
    }

    public void FinishTyping() => _typewriter.Finish();

    /// <summary>
    /// State hash: whether it is open, the speaker, the page being typed and how much of it shows, the pages waiting,
    /// the paging settings, how long the page has been complete and the blip counter (which reveals play a sound).
    /// Left out: <c>_time</c> (only bobs the "more" arrow) and the visible-line cache that drawing fills.
    /// </summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(Active);
        hash.Add(Speaker);
        hash.Add(_typewriter.Text);
        hash.Add(_typewriter.VisibleCount);
        hash.Add(_pages.Count);
        foreach (var page in _pages) hash.Add(page);
        hash.Add(WaitForConfirmOnLast);
        hash.Add(Silent);
        hash.Add(AutoAdvance);
        hash.Add(_completeTime);
        hash.Add(_blips);
    }

    public void Update(float dt, InputMap input)
    {
        if (!Active) return;
        _time += dt;
        _typewriter.Update(dt);
        _completeTime = _typewriter.Complete ? _completeTime + dt : 0;
        if (AutoAdvance > 0 && _pages.Count == 0 && WaitForConfirmOnLast && _completeTime >= AutoAdvance) { Next(); return; }
        if (!input.Pressed("confirm") && !(input.Pressed("cancel") && _typewriter.Complete)) return;
        if (!_typewriter.Complete) { _typewriter.Finish(); return; }
        if (_pages.Count == 0 && !WaitForConfirmOnLast) return;
        _audio.Play("text", 0.5f);
        Next();
    }

    /// <summary>Shows text and waits until the player dismisses the last page.</summary>
    public IEnumerator Say(string text, string? speaker = null)
    {
        WaitForConfirmOnLast = true;
        Show(text, speaker);
        while (Active) yield return null;
    }

    private string? _linesText;
    private int _linesCount = -1;
    private string[] _lines = [];

    /// <summary>The revealed lines, split again only when another character shows (not every frame).</summary>
    private string[] VisibleLines()
    {
        if (!ReferenceEquals(_linesText, _typewriter.Text) || _linesCount != _typewriter.VisibleCount)
        {
            (_linesText, _linesCount) = (_typewriter.Text, _typewriter.VisibleCount);
            _lines = _typewriter.Visible.Split('\n');
        }
        return _lines;
    }

    public void Draw(IRenderer r)
    {
        if (!Active) return;
        UiKit.Box(r, Area);
        float y = Area.Y + 9;
        foreach (var line in VisibleLines())
        {
            UiKit.Text(r, _font, line, Area.X + 10, y);
            y += 13;
        }
        if (Speaker is { Length: > 0 } name)
        {
            int w = _font.MeasureWidth(name) + 12;
            var tag = new RectF(Area.X + 8, Area.Y - 10, w, 13);
            r.FillRect(new RectF(tag.X + 1, tag.Y, tag.Width - 2, tag.Height), UiKit.Border);
            r.FillRect(new RectF(tag.X, tag.Y + 1, tag.Width, tag.Height - 2), UiKit.Border);
            r.FillRect(new RectF(tag.X + 1, tag.Y + 1, tag.Width - 2, tag.Height - 3), Pal.Coral);
            _font.Draw(r, name, new Vector2(tag.X + 6, tag.Y + 3), Pal.White);
        }
        if (_typewriter.Complete && (_pages.Count > 0 || WaitForConfirmOnLast))
        {
            float bob = MathF.Round(MathX.Wave(_time, 2f, 1.5f));
            _font.Draw(r, "▼", new Vector2(Area.Right - 14, Area.Bottom - 12 + bob), Pal.Coral);
        }
    }
}

/// <summary>
/// A small vertical menu box (Yes/No, learning moves...). Returns the chosen index, or -1 on cancel. The engine's
/// <see cref="DemoKit.OptionList"/> moves the cursor and chooses; the box and cursor are drawn in the game's <see cref="UiKit"/> look.
/// </summary>
internal sealed class ChoiceMenu : IStateHashed
{
    /// <summary>
    /// State hash: the options, the cursor, whether it can be cancelled and the result. Left out: <c>_time</c> (cursor
    /// animation) and the width that drawing measures.
    /// </summary>
    public void HashState(StateHasher hash)
    {
        hash.Add(_list.Options.Count);
        foreach (var option in _list.Options) hash.Add(option);
        hash.Add(Index);
        hash.Add(CanCancel);
        hash.Add(Result is not null);
        hash.Add(Result ?? 0);
    }

    private readonly DemoKit.OptionList _list;
    private readonly BitmapFont _font;
    private readonly AudioMixer _audio;
    private float _time;
    private int _width = -1;
    public int Index => _list.Index;
    public bool CanCancel { get; init; } = true;
    public int? Result { get; private set; }

    public ChoiceMenu(BitmapFont font, AudioMixer audio, params string[] options)
    {
        _font = font;
        _audio = audio;
        _list = UiKit.List(audio, options);
    }

    public RectF Bounds(Vector2 anchorBottomRight)
    {
        if (_width < 0) _width = _list.Options.Max(o => _font.MeasureWidth(o)) + 24;
        int w = _width;
        int h = _list.Options.Count * 12 + 10;
        return new RectF(anchorBottomRight.X - w, anchorBottomRight.Y - h, w, h);
    }

    public void Update(float dt, InputMap input)
    {
        _time += dt;
        if (Result is not null) return;
        if (_list.Update(input) is { } chosen) { Result = chosen; _audio.Play("confirm", 0.7f); }
        else if (CanCancel && input.Pressed("cancel")) { Result = -1; _audio.Play("cancel", 0.7f); }
    }

    public void Draw(IRenderer r, Vector2 anchorBottomRight)
    {
        var b = Bounds(anchorBottomRight);
        UiKit.Box(r, b);
        for (int i = 0; i < _list.Options.Count; i++)
        {
            float y = b.Y + 6 + i * 12;
            UiKit.Text(r, _font, _list.Options[i], b.X + 14, y);
            if (i == Index) UiKit.Cursor(r, _font, b.X + 5, y, _time);
        }
    }
}

/// <summary>
/// A question's menu beside the text box, the same in the overworld and in battle: a cutscene awaits
/// <see cref="Choose"/>, and the scene updates and draws the open <see cref="Menu"/>.
/// </summary>
internal sealed class ChoicePrompt(BitmapFont font, AudioMixer audio) : IStateHashed
{
    /// <summary>The open menu, or null between questions.</summary>
    public ChoiceMenu? Menu { get; private set; }

    /// <summary>Opens a menu, waits until it is answered, closes it and hands the answer (-1 when cancelled) to <paramref name="result"/>.</summary>
    public IEnumerator Choose(string[] options, Action<int> result, bool canCancel = true)
    {
        Menu = new ChoiceMenu(font, audio, options) { CanCancel = canCancel };
        while (Menu.Result is null) yield return null;
        int chosen = Menu.Result.Value;
        Menu = null;
        result(chosen);
    }

    public void Update(float dt, InputMap input) => Menu?.Update(dt, input);

    public void Draw(IRenderer r, Vector2 anchorBottomRight) => Menu?.Draw(r, anchorBottomRight);

    /// <summary>State hash: the open menu, if any.</summary>
    public void HashState(StateHasher hash) => hash.Add(Menu);
}
