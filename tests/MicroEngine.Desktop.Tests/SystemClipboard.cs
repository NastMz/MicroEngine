using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// What the clipboard tests need from the machine. The system clipboard is one per desktop session: every test
/// process on the machine writes the same one, and on Windows any program may keep it open for a moment (clipboard
/// history, download managers and remote desktops all read it after each change).
/// </summary>
internal static partial class SystemClipboard
{
    /// <summary>
    /// The clipboard for this test alone, until disposed: tests in other processes of this session (the same suite
    /// run from another checkout) wait for their turn instead of overwriting each other's text.
    /// </summary>
    public static IDisposable Exclusive()
    {
        var turn = new Mutex(false, @"Local\MicroEngine.Tests.SystemClipboard");
        try
        {
            if (!turn.WaitOne(TimeSpan.FromSeconds(60)))
            {
                turn.Dispose();
                Assert.Fail("another test process kept the system clipboard's turn for a minute");
            }
        }
        catch (AbandonedMutexException)
        {
            // The process that had the turn died: the turn is ours.
        }
        return new Turn(turn);
    }

    private sealed class Turn(Mutex turn) : IDisposable
    {
        public void Dispose()
        {
            turn.ReleaseMutex();
            turn.Dispose();
        }
    }

    /// <summary>
    /// Keeps the clipboard open from another thread for <paramref name="time"/>, as another program reading it does,
    /// without touching what it holds. Returns once it is open; null where the clipboard cannot be held (not Windows).
    /// </summary>
    public static Task? Hold(TimeSpan time)
    {
        if (!OperatingSystem.IsWindows()) return null;
        using var open = new ManualResetEventSlim();
        var held = Task.Factory.StartNew(() =>
        {
            // Other programs may have it right now (they read it after each change): wait for them.
            var waited = Stopwatch.StartNew();
            while (!OpenClipboard(0))
            {
                if (waited.Elapsed > TimeSpan.FromSeconds(5)) throw new TimeoutException($"the clipboard stayed open: {Holder()}");
                Thread.Sleep(1);
            }
            try
            {
                var clock = Stopwatch.StartNew();
                open.Set();
                while (clock.Elapsed < time) Thread.Sleep(1);
            }
            finally { CloseClipboard(); }
        }, TaskCreationOptions.LongRunning);
        while (!open.Wait(10))
            if (held.IsCompleted) held.GetAwaiter().GetResult(); // throws why it could not be opened
        return held;
    }

    /// <summary>Who has the clipboard open now, for a failure's message; empty when nobody or where it cannot be told.</summary>
    public static string Holder()
    {
        if (!OperatingSystem.IsWindows()) return "";
        nint window = GetOpenClipboardWindow();
        if (window == 0) return "";
        _ = GetWindowThreadProcessId(window, out uint process);
        try { return $"the clipboard is open by {Process.GetProcessById((int)process).ProcessName} (process {process})"; }
        catch (ArgumentException) { return $"the clipboard is open by process {process}"; }
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(nint window);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll")]
    private static partial nint GetOpenClipboardWindow();

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(nint window, out uint process);
}
