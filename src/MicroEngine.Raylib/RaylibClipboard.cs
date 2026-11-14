using System.Diagnostics;
using System.Runtime.InteropServices;
using MicroEngine.Input;
using Rl = Raylib_cs.Raylib;

namespace MicroEngine.Raylib;

/// <summary>
/// The system clipboard of a raylib window. On Windows it is read and written with the system's own calls instead of
/// raylib's (GLFW), for two reasons. Only one program has the clipboard open at a time, and the programs that watch
/// it (clipboard history, download managers, remote desktops) open it for some tens of milliseconds after every
/// change: GLFW gives up after three tries a millisecond apart and reports nothing, where this class asks again for
/// as long as <see cref="Patience"/>. And a failed try leaves GLFW's error pending on the thread: the next window
/// raylib opens in the process takes it for its own failure and raylib ends the process.
/// </summary>
internal sealed class RaylibClipboard : IClipboard
{
    /// <summary>The longest a read or a write waits for a program that has the clipboard open.</summary>
    private static readonly TimeSpan Patience = TimeSpan.FromMilliseconds(100);
    private const uint UnicodeText = 13; // CF_UNICODETEXT
    private const uint Moveable = 0x0002; // GMEM_MOVEABLE, what the clipboard takes

    public string? GetText()
    {
        if (!OperatingSystem.IsWindows()) return Rl.GetClipboardText_() is { Length: > 0 } text ? text : null;
        // No text on the clipboard is an answer; text that cannot be reached is another program holding it.
        if (IsClipboardFormatAvailable(UnicodeText) == 0 || !Open()) return null;
        try
        {
            nint data = GetClipboardData(UnicodeText);
            if (data == 0) return null;
            nint chars = GlobalLock(data);
            if (chars == 0) return null;
            try { return Marshal.PtrToStringUni(chars) is { Length: > 0 } text ? text : null; }
            finally { GlobalUnlock(data); }
        }
        finally { CloseClipboard(); }
    }

    public bool SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!OperatingSystem.IsWindows())
        {
            Rl.SetClipboardText(text);
            return true;
        }
        nint data = GlobalAlloc(Moveable, (nuint)(text.Length + 1) * sizeof(char));
        if (data == 0) return false;
        nint chars = GlobalLock(data);
        if (chars == 0)
        {
            GlobalFree(data);
            return false;
        }
        Marshal.Copy(text.ToCharArray(), 0, chars, text.Length);
        Marshal.WriteInt16(chars, text.Length * sizeof(char), 0);
        GlobalUnlock(data);
        if (!Open())
        {
            GlobalFree(data);
            return false;
        }
        try
        {
            EmptyClipboard();
            if (SetClipboardData(UnicodeText, data) != 0) return true; // the clipboard owns the memory now
            GlobalFree(data);
            return false;
        }
        finally { CloseClipboard(); }
    }

    /// <summary>Opens the clipboard for the game's window, waiting for a program that has it open.</summary>
    private static unsafe bool Open()
    {
        nint window = (nint)Rl.GetWindowHandle();
        long started = Stopwatch.GetTimestamp();
        while (OpenClipboard(window) == 0)
        {
            if (Stopwatch.GetElapsedTime(started) >= Patience) return false;
            Thread.Sleep(1);
        }
        return true;
    }

    [DllImport("user32.dll")]
    private static extern int IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll")]
    private static extern int OpenClipboard(nint window);

    [DllImport("user32.dll")]
    private static extern int CloseClipboard();

    [DllImport("user32.dll")]
    private static extern int EmptyClipboard();

    [DllImport("user32.dll")]
    private static extern nint GetClipboardData(uint format);

    [DllImport("user32.dll")]
    private static extern nint SetClipboardData(uint format, nint data);

    [DllImport("kernel32.dll")]
    private static extern nint GlobalAlloc(uint flags, nuint bytes);

    [DllImport("kernel32.dll")]
    private static extern nint GlobalFree(nint memory);

    [DllImport("kernel32.dll")]
    private static extern nint GlobalLock(nint memory);

    [DllImport("kernel32.dll")]
    private static extern int GlobalUnlock(nint memory);
}
