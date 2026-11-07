using MicroEngine.Input;
using static SDL.SDL3;

namespace MicroEngine.Sdl;

/// <summary>
/// The system clipboard through SDL, on desktops and phones. A browser page has its own (<see cref="PageClipboard"/>):
/// SDL's would keep the text inside the game.
/// </summary>
internal sealed class SdlClipboard : IClipboard
{
    public string? GetText() => SDL_HasClipboardText() ? SDL_GetClipboardText() : null;

    public bool SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return SDL_SetClipboardText(text);
    }
}
