using MicroEngine.Input;

namespace CourtyardKey.UI;

/// <summary>
/// Remembers a prompt text until <see cref="ActionPrompts.Version"/> says it may have changed (another device,
/// another pad family, a new label), so drawing it every frame builds no new string.
/// </summary>
public sealed class CachedPrompt(ActionPrompts prompts, string template, bool isAction = false)
{
    private string? _text;
    private int _version;

    /// <summary>The text of an action (<see cref="ActionPrompts.Text"/>) or a template (<see cref="ActionPrompts.Format"/>).</summary>
    public string Text
    {
        get
        {
            int version = prompts.Version;
            if (_text is null || version != _version)
            {
                _text = isAction ? prompts.Text(template) : prompts.Format(template);
                _version = version;
            }
            return _text;
        }
    }

    /// <summary>The text of one action ("Z", "A", "tap").</summary>
    public static CachedPrompt ForAction(ActionPrompts prompts, string action) => new(prompts, action, isAction: true);
}

/// <summary>Formats a number into text only when the number changes.</summary>
public sealed class NumberText(Func<int, string> format)
{
    private string? _text;
    private int _value;

    public string For(int value)
    {
        if (_text is null || value != _value)
        {
            _text = format(value);
            _value = value;
        }
        return _text;
    }
}
