using System.Globalization;
using System.Text;
using MicroEngine.Kernel;

namespace MicroEngine.UI;

/// <summary>
/// Text shown a character at a time: the typewriter of a dialogue box, a caption or a sign. It counts characters as a
/// reader does (a letter with its accents and marks, an emoji made of several code points or a line break is one), so
/// text in any script is never cut in the middle of one, and it reveals them in reading order, which a right-to-left
/// line shows growing from its right edge. It holds no look: a <see cref="Label"/> reveals its text with one
/// (<see cref="Label.Reveal"/>), and a box a game draws itself reads <see cref="Visible"/> or
/// <see cref="VisibleLines"/>. Advance it with the clock of whatever shows it (a scene's <c>dt</c>); it allocates
/// nothing while it runs.
/// </summary>
/// <example>
/// <code>
/// var reveal = new TextReveal { CharactersPerSecond = 45 };
/// reveal.CharacterRevealed += _ =&gt; Audio.TryPlay("blip", 0.3f);
/// reveal.Start("The gems only grow at night.");
/// // every update
/// reveal.Update(dt);
/// if (Input.Pressed("confirm"))
/// {
///     if (!reveal.Complete) reveal.Finish();      // the first press shows the rest
///     else NextPage();
/// }
/// </code>
/// </example>
public sealed class TextReveal : IStateHashed
{
    private readonly List<int> _ends = [];      // where each character ends in Text (UTF-16 index)
    private float _shown;                       // characters shown, with the fraction of the next one
    private float _pause;                       // seconds left of a pause
    private string[]? _lines, _visible;
    private int _visibleFor = -1;

    /// <summary>The whole text being revealed.</summary>
    public string Text { get; private set; } = "";

    /// <summary>
    /// How many characters show per second (45 by default). A value that is not a finite number above zero (zero,
    /// negative, NaN, infinite) is ignored while it lasts: the reveal stays where it is, and nothing is owed when a
    /// valid rate returns.
    /// </summary>
    public float CharactersPerSecond { get; set; } = 45f;

    /// <summary>
    /// Seconds the reveal waits after showing a character, given its first code point; null (the default) or zero
    /// waits for none. A pause at the end of a sentence is <c>rune =&gt; rune.Value is '.' or '!' or '?' ? 0.3f : 0</c>.
    /// Nothing waits after the last character of the text.
    /// </summary>
    public Func<Rune, float>? PauseAfter { get; set; }

    /// <summary>How many characters the text has, as a reader counts them (spaces and line breaks included).</summary>
    public int Count => _ends.Count;

    /// <summary>How many of them show.</summary>
    public int Revealed => Math.Clamp((int)_shown, 0, _ends.Count);

    /// <summary>
    /// The length of the part of <see cref="Text"/> that shows, in <c>char</c>s: always at the end of a whole
    /// character. A <see cref="Label"/> takes it as its <see cref="Label.VisibleCharacters"/>.
    /// </summary>
    public int VisibleLength => Revealed is var n && n > 0 ? _ends[n - 1] : 0;

    /// <summary>Whether the whole text shows.</summary>
    public bool Complete => Revealed >= _ends.Count;

    /// <summary>The part of <see cref="Text"/> that shows, without a copy.</summary>
    public ReadOnlySpan<char> Visible => Text.AsSpan(0, VisibleLength);

    /// <summary>The lines of <see cref="Text"/> (separated by <c>\n</c>), whole.</summary>
    public IReadOnlyList<string> Lines => _lines ??= Text.Split('\n');

    /// <summary>
    /// What shows of each of <see cref="Lines"/>, for a box that draws its lines itself: a line that shows whole is
    /// that line, one not reached yet is empty, and only the line being revealed is a new string, made when another
    /// character of it shows. The list is the same one until <see cref="Start"/>; its entries change as the text shows.
    /// </summary>
    public IReadOnlyList<string> VisibleLines
    {
        get
        {
            var lines = _lines ??= Text.Split('\n');
            if (_visible is null || _visible.Length != lines.Length)
            {
                _visible = new string[lines.Length];
                _visibleFor = -1;
            }
            int length = VisibleLength;
            if (_visibleFor == length) return _visible;
            int start = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int shown = Math.Clamp(length - start, 0, line.Length);
                if (shown == line.Length) _visible[i] = line;
                else if (shown == 0) _visible[i] = "";
                else if (_visible[i] is not { } was || was.Length != shown) _visible[i] = line[..shown];
                start += line.Length + 1;
            }
            _visibleFor = length;
            return _visible;
        }
    }

    /// <summary>
    /// Raised for each character as it shows, with its first code point, unless it is a space or a line break: play
    /// the typing sound from it. <see cref="Finish"/> raises none.
    /// </summary>
    public event Action<Rune>? CharacterRevealed;

    /// <summary>
    /// Starts revealing <paramref name="text"/> (null counts as empty) with its first <paramref name="revealed"/>
    /// characters already showing: zero from the beginning, or what was showing when the same text is laid out again.
    /// </summary>
    public void Start(string text, int revealed = 0)
    {
        Text = text ?? "";
        _ends.Clear();
        var rest = Text.AsSpan();
        int at = 0;
        while (!rest.IsEmpty)
        {
            int length = StringInfo.GetNextTextElementLength(rest);
            at += length;
            _ends.Add(at);
            rest = rest[length..];
        }
        _shown = Math.Clamp(revealed, 0, _ends.Count);
        _pause = 0;
        _lines = null;
        _visibleFor = -1;
    }

    /// <summary>
    /// Shows more of the text for <paramref name="dt"/> seconds, raising <see cref="CharacterRevealed"/> for each new
    /// character. A step that is not a finite number above zero is ignored, like such a rate
    /// (<see cref="CharactersPerSecond"/>): <see cref="Revealed"/> is always between zero and <see cref="Count"/>.
    /// </summary>
    public void Update(float dt)
    {
        if (!(dt > 0) || !float.IsFinite(dt)) return;
        if (_pause > 0)
        {
            float waited = MathF.Min(_pause, dt);
            _pause -= waited;
            dt -= waited;
            if (_pause > 0 || dt <= 0) return;
        }
        int before = Revealed, count = _ends.Count;
        if (before >= count) return;
        float rate = CharactersPerSecond;
        if (!(rate > 0) || !float.IsFinite(rate)) return;
        _shown = MathF.Min(count, _shown + rate * dt);
        int after = Revealed;
        for (int i = before; i < after; i++)
        {
            Rune.DecodeFromUtf16(Text.AsSpan(i == 0 ? 0 : _ends[i - 1]), out var rune, out _);
            if (!Rune.IsWhiteSpace(rune)) CharacterRevealed?.Invoke(rune);
            if (PauseAfter is { } pause && i < count - 1 && pause(rune) is > 0 and var seconds)
            {
                _shown = i + 1;                 // what the step had left is not carried over the pause
                _pause = seconds;
                return;
            }
        }
    }

    /// <summary>Shows the rest of the text at once, silently, and ends a pause.</summary>
    public void Finish()
    {
        _shown = _ends.Count;
        _pause = 0;
    }

    /// <summary>Adds the text, how much of it shows and the pause in course to a state hash: what the next confirm does depends on them.</summary>
    public void HashState(StateHasher hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        hash.Add(Text);
        hash.Add(_shown);
        hash.Add(_pause);
        hash.Add(CharactersPerSecond);
    }
}
