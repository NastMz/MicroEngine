using System.Diagnostics;
using System.Runtime.InteropServices;
using MicroEngine.Input;
using Rl = Raylib_cs.Raylib;

namespace MicroEngine.Raylib;

/// <summary>
/// The system clipboard through raylib (GLFW). On Windows only one program has the clipboard open at a time, and the
/// programs that watch it (clipboard history, download managers, remote desktops) open it for some tens of
/// milliseconds after every change. GLFW gives up after three tries a millisecond apart and reports nothing, so this
/// class asks again for as long as <see cref="Patience"/>: a copy followed by a paste works, and a write that never
/// got through is reported as not taken.
/// </summary>
internal sealed class RaylibClipboard : IClipboard
{
    /// <summary>The longest a read or a write waits for a program that has the clipboard open.</summary>
    private static readonly TimeSpan Patience = TimeSpan.FromMilliseconds(100);
    private const uint UnicodeText = 13; // CF_UNICODETEXT, the only format GLFW reads and writes

    public string? GetText()
    {
        long started = Stopwatch.GetTimestamp();
        while (true)
        {
            if (Rl.GetClipboardText_() is { Length: > 0 } text) return text;
            // No text on the clipboard is an answer; text that could not be read is another program holding it.
            if (!OperatingSystem.IsWindows() || IsClipboardFormatAvailable(UnicodeText) == 0) return null;
            if (Stopwatch.GetElapsedTime(started) >= Patience) return null;
            Thread.Sleep(1);
        }
    }

    public bool SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!OperatingSystem.IsWindows())
        {
            Rl.SetClipboardText(text);
            return true;
        }
        // raylib does not say whether GLFW could open the clipboard; the clipboard's change count does.
        long started = Stopwatch.GetTimestamp();
        uint before = GetClipboardSequenceNumber();
        while (true)
        {
            Rl.SetClipboardText(text);
            if (GetClipboardSequenceNumber() != before) return true;
            if (Stopwatch.GetElapsedTime(started) >= Patience) return false;
            Thread.Sleep(1);
        }
    }

    [DllImport("user32.dll")]
    private static extern int IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();
}
