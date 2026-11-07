namespace MicroEngine.UI;

/// <summary>Reveals text character by character. Confirm skips to the end; a second confirm advances.</summary>
[Obsolete("Reveal text with Label.VisibleCharacters, raised a little every update. It is removed in a later SDK release; see the migration notes.")]
public sealed class Typewriter
{
    private float _visible;
    /// <summary>The full text being revealed.</summary>
    public string Text { get; private set; } = "";
    /// <summary>Reveal speed (45 by default).</summary>
    public float CharactersPerSecond { get; set; } = 45f;
    /// <summary>Number of characters revealed so far.</summary>
    public int VisibleCount => Math.Min(Text.Length, (int)_visible);
    /// <summary>Whether the whole text is revealed.</summary>
    public bool Complete => VisibleCount >= Text.Length;
    /// <summary>The revealed part of the text.</summary>
    public string Visible => Text[..VisibleCount];
    /// <summary>Raised for each newly revealed non-space character (for blip sounds).</summary>
    public event Action<char>? Revealed;

    /// <summary>Starts revealing <paramref name="text"/> from the beginning; null counts as empty.</summary>
    public void Start(string text) => Start(text, 0);

    /// <summary>Starts revealing <paramref name="text"/> with its first <paramref name="revealed"/> characters already shown (text laid out again mid-reveal).</summary>
    public void Start(string text, int revealed)
    {
        Text = text ?? "";
        _visible = Math.Clamp(revealed, 0, Text.Length);
    }

    /// <summary>Reveals more characters for <paramref name="dt"/> seconds and raises <see cref="Revealed"/> for each new visible one.</summary>
    public void Update(float dt)
    {
        int before = VisibleCount;
        _visible = MathF.Min(Text.Length, _visible + CharactersPerSecond * dt);
        for (int i = before; i < VisibleCount; i++)
            if (!char.IsWhiteSpace(Text[i])) Revealed?.Invoke(Text[i]);
    }

    /// <summary>Reveals the rest of the text at once, without raising <see cref="Revealed"/>.</summary>
    public void Finish() => _visible = Text.Length;
}
