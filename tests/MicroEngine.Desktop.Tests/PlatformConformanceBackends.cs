using MicroEngine.Core;
using MicroEngine.Raylib;
using MicroEngine.Sdl;
using SDL;
using static SDL.SDL3;

namespace MicroEngine.Desktop.Tests;

/// <summary>The headless host: every event is driven through its test hooks, so it runs everywhere.</summary>
[TestClass]
public sealed class HeadlessPlatformConformance : PlatformConformance
{
    protected override IPlatform? Open(PlatformRequest request) => HeadlessPlatform.For(request);

    protected override bool Resize(IPlatform host, int width, int height)
    {
        ((HeadlessPlatform)host).Resize(width, height);
        return true;
    }

    protected override bool Focus(IPlatform host, bool focused)
    {
        ((HeadlessPlatform)host).Focus(focused);
        return true;
    }

    protected override bool Background(IPlatform host, bool background)
    {
        var headless = (HeadlessPlatform)host;
        if (background) headless.Suspend();
        else headless.Resume();
        return true;
    }

    protected override bool RequestQuit(IPlatform host)
    {
        ((HeadlessPlatform)host).RequestQuit();
        return true;
    }
}

/// <summary>Opens a real desktop host for the conformance suite: real windows, hidden.</summary>
public abstract class NativePlatformConformance : PlatformConformance
{
    /// <summary>The host's catalog entry.</summary>
    protected abstract PlatformProfile Profile { get; }

    [TestInitialize]
    public void NotOnMacOS()
    {
        if (OperatingSystem.IsMacOS()) Assert.Inconclusive("windows can only be opened on the main thread on macOS; see web/tests/desktop.mjs");
    }

    protected override IPlatform? Open(PlatformRequest request)
    {
        try { return Profile.Create(request); }
        catch (InvalidOperationException e)
        {
            Native.Require(false, $"{Profile.Name} cannot open here: {e.Message}");
            return null;
        }
    }
}

/// <summary>
/// SDL, driven through SDL's own event queue: the test pushes the events the system would send (a window's focus, a
/// phone's trip to the background, a quit) and resizes the window for real.
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed unsafe class SdlPlatformConformance : NativePlatformConformance
{
    protected override PlatformProfile Profile => SdlPlatform.Profile;

    private static SDL_Window* Window(IPlatform host) => (SDL_Window*)host.MainSurface!.NativeWindow;

    protected override bool Resize(IPlatform host, int width, int height)
    {
        var window = Window(host);
        // The window system may apply the size later (X11 answers with an event); the test pumps until it does.
        if (!SDL_SetWindowSize(window, width, height)) return false;
        SDL_SyncWindow(window);
        return true;
    }

    protected override unsafe (int Width, int Height) ToolkitWindowSize(IPlatform host)
    {
        int w, h;
        return SDL_GetWindowSizeInPixels(Window(host), &w, &h) ? (w, h) : (0, 0);
    }

    protected override bool Focus(IPlatform host, bool focused)
    {
        var e = new SDL_Event();
        e.window.type = focused ? SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED : SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST;
        e.window.windowID = SDL_GetWindowID(Window(host));
        return SDL_PushEvent(&e);
    }

    protected override bool Background(IPlatform host, bool background)
    {
        var e = new SDL_Event { type = (uint)(background ? SDL_EventType.SDL_EVENT_DID_ENTER_BACKGROUND : SDL_EventType.SDL_EVENT_DID_ENTER_FOREGROUND) };
        return SDL_PushEvent(&e);
    }

    protected override bool RequestQuit(IPlatform host)
    {
        // A window's close button sends both; the host delivers one request.
        var close = new SDL_Event();
        close.window.type = SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED;
        close.window.windowID = SDL_GetWindowID(Window(host));
        var quit = new SDL_Event { type = (uint)SDL_EventType.SDL_EVENT_QUIT };
        return SDL_PushEvent(&close) && SDL_PushEvent(&quit);
    }
}

/// <summary>The backends' own creation options carry no time mode either: display pacing is an explicit option.</summary>
[TestClass]
public sealed class BackendOptionsTests
{
    [TestMethod]
    public void Raylib_desktop_options_have_an_explicit_display_wait_and_no_time_mode()
    {
        Assert.IsNull(typeof(DesktopOptions).GetProperty("FixedStep"), "deterministic time is the loop's business only");
        Assert.IsTrue(new DesktopOptions().WaitForDisplay, "a visible window waits for the display by default");
        Assert.IsFalse(new DesktopOptions { Hidden = true, WaitForDisplay = false }.WaitForDisplay);
    }

    [TestMethod]
    public void A_call_written_for_the_old_positional_options_no_longer_compiles()
    {
        // new DesktopOptions(scale, hidden, fixedStep) would otherwise bind its third argument to another option:
        // with no constructor taking arguments, every positional call fails to compile.
        var withArguments = typeof(DesktopOptions).GetConstructors().Where(c => c.GetParameters().Length > 0 && c.GetParameters()[0].ParameterType != typeof(DesktopOptions)).ToList();
        Assert.IsEmpty(withArguments, string.Join("; ", withArguments));
        var options = new DesktopOptions();
        Assert.IsNull(options.Scale);
        Assert.IsFalse(options.Hidden);
        Assert.IsFalse(options.Mute);
        Assert.AreEqual(3, options.CaptureScale);
    }
}

/// <summary>
/// raylib on desktop: its window can be resized; GLFW gives no way to focus a hidden window or to ask it to close from
/// code, and a desktop raylib window never suspends (only a hidden browser page does), so those tests are skipped.
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class RaylibPlatformConformance : NativePlatformConformance
{
    protected override PlatformProfile Profile => RaylibPlatform.Profile;

    protected override bool Resize(IPlatform host, int width, int height)
    {
        Raylib_cs.Raylib.SetWindowSize(width, height);
        return true;
    }

    // raylib reads the window system's events when a frame ends (GLFW's poll in EndDrawing), so a frame is presented
    // before each pump, as the loop does.
    protected override void BetweenPumps(IPlatform host)
    {
        host.BeginFrame();
        host.EndFrame();
    }

    protected override (int Width, int Height) ToolkitWindowSize(IPlatform host) => (Raylib_cs.Raylib.GetRenderWidth(), Raylib_cs.Raylib.GetRenderHeight());
}
