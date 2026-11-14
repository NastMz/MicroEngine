using System.Diagnostics;
using MicroEngine.Input;
using static SDL.SDL3;

namespace MicroEngine.Sdl;

/// <summary>
/// The system clipboard through SDL, on desktops and phones. A browser page has its own (<see cref="PageClipboard"/>):
/// SDL's would keep the text inside the game.
/// </summary>
/// <remarks>
/// On Windows only one program has the clipboard open at a time, and the programs that watch it (clipboard history,
/// download managers, remote desktops) open it for some tens of milliseconds after every change. SDL tries three
/// times over 20 ms, which those programs outlast, so this class asks again for as long as <see cref="Patience"/>.
/// </remarks>
internal sealed class SdlClipboard : IClipboard
{
    /// <summary>The longest a read or a write waits for a program that has the clipboard open.</summary>
    private static readonly TimeSpan Patience = TimeSpan.FromMilliseconds(100);

    public string? GetText()
    {
        if (!OperatingSystem.IsWindows()) return SDL_HasClipboardText() ? SDL_GetClipboardText() : null;
        long started = Stopwatch.GetTimestamp();
        while (SDL_HasClipboardText())
        {
            // SDL gives an empty text when it could not open the clipboard that it says holds one.
            if (SDL_GetClipboardText() is { Length: > 0 } text) return text;
            if (Stopwatch.GetElapsedTime(started) >= Patience) break;
            Thread.Sleep(1);
        }
        return null;
    }

    public bool SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        long started = Stopwatch.GetTimestamp();
        while (!SDL_SetClipboardText(text))
        {
            if (!OperatingSystem.IsWindows() || Stopwatch.GetElapsedTime(started) >= Patience) return false;
            Thread.Sleep(1);
        }
        return true;
    }
}
