namespace MicroEngine.Input;

/// <summary>
/// The system clipboard's text: a platform capability (<c>Game.TryGetCapability</c>). Text fields copy, cut and paste
/// through it (<c>MicroEngine.UI.TextField</c>); a game may use it for its own "copy the seed" or "paste a code" buttons.
/// </summary>
/// <remarks>
/// On desktops it is the system clipboard. In a browser, reading gives the text of the page's latest paste (the
/// player pressed Ctrl+V or Command+V, or chose Paste in the browser's menu), because a page cannot read the
/// clipboard without asking; writing puts the text on the system clipboard when the browser allows it, which it does
/// right after a key press or a click.
/// </remarks>
public interface IClipboard
{
    /// <summary>The text on the clipboard; null when it holds no text or cannot be read.</summary>
    string? GetText();
    /// <summary>Puts <paramref name="text"/> on the clipboard; returns whether the platform took it.</summary>
    bool SetText(string text);
}

/// <summary>A clipboard that only remembers its text: the headless platform's, and a stand-in where there is no system clipboard.</summary>
public sealed class MemoryClipboard : IClipboard
{
    /// <summary>The text on this clipboard, or null.</summary>
    public string? Text { get; set; }

    /// <inheritdoc/>
    public string? GetText() => Text;

    /// <inheritdoc/>
    public bool SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
        return true;
    }
}
