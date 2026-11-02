namespace TrailDuel.Ui;

/// <summary>
/// Text built from up to two numbers (HP "12/20", money, a level, a prompt version) and built again only when they
/// change, so a screen that shows it every frame makes no garbage. Pass a <c>static</c> lambda as the format; use one
/// overload per instance.
/// </summary>
internal struct CachedText
{
    private string? _text;
    private int _a, _b;

    public string Get(int a, Func<int, string> format)
    {
        if (_text is null || a != _a) (_text, _a) = (format(a), a);
        return _text;
    }

    public string Get(int a, int b, Func<int, int, string> format)
    {
        if (_text is null || a != _a || b != _b) (_text, _a, _b) = (format(a, b), a, b);
        return _text;
    }

    /// <summary>Built from <paramref name="state"/> while <paramref name="key"/> stays the same (for example <c>Prompts.Version</c>).</summary>
    public string Get<TState>(int key, TState state, Func<TState, string> format)
    {
        if (_text is null || key != _a) (_text, _a) = (format(state), key);
        return _text;
    }
}
