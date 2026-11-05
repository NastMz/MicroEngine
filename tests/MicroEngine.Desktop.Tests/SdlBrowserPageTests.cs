using System.Numerics;
using MicroEngine.Core;
using MicroEngine.Input;
using MicroEngine.Kernel;
using MicroEngine.Platform;
using MicroEngine.Sdl;

namespace MicroEngine.Desktop.Tests;

/// <summary>
/// The SDL platform's browser page path (<see cref="BrowserPage"/>, the SDL web host): what the page feeds reaches the
/// game as it does in a browser. The page path is the same code in a browser and on a desktop window, so it is checked
/// here on a hidden SDL window; what only a browser has (WebGL, the page's own events) is checked by web/tests.
/// </summary>
[TestClass]
[TestCategory("Native")]
public sealed class SdlBrowserPageTests
{
    private readonly BrowserPage _page = new();
    private SdlPlatform? _platform;

    [TestInitialize]
    public void Open()
    {
        try { _platform = SdlPlatform.ForPage(new PlatformRequest(new GameSettings("page test", 160, 90, 2)) { Hidden = true, Mute = true }, _page); }
        catch (InvalidOperationException e) { Native.Require(false, $"SDL cannot open a window here: {e.Message}"); }
    }

    [TestCleanup]
    public void Close() => _platform?.Dispose();

    private List<TouchEvent> Touches()
    {
        var events = new List<TouchEvent>();
        _platform!.Touch.ReadTouches(events);
        return events;
    }

    [TestMethod]
    public void The_page_s_pointers_are_the_touches_in_order_with_their_ids()
    {
        _page.Pointer(7, 0, 10, 20);
        _page.Pointer(8, 0, 300, 40);
        _page.Pointer(7, 1, 12, 22);
        _page.Pointer(7, 2, 12, 22);
        _page.Pointer(8, 3, 300, 40);

        CollectionAssert.AreEqual(new[]
        {
            new TouchEvent(7, TouchPhase.Began, new Vector2(10, 20)),
            new TouchEvent(8, TouchPhase.Began, new Vector2(300, 40)),
            new TouchEvent(7, TouchPhase.Moved, new Vector2(12, 22)),
            new TouchEvent(7, TouchPhase.Ended, new Vector2(12, 22)),
            new TouchEvent(8, TouchPhase.Canceled, new Vector2(300, 40)),
        }, Touches());
        Assert.AreEqual(0, Touches().Count, "Each touch is read once.");
    }

    [TestMethod]
    public void The_page_s_screen_is_the_window_while_the_game_draws_into_its_canvas()
    {
        var screen = ((IScreenView)_platform!).ScreenPixels;
        _page.Screen(4, 0, 4, 0, 1, touchFirst: false);
        _platform.BeginFrame(); // the game's canvas (160×90) is the render target until the frame ends
        Assert.AreEqual(new Vector2(screen.Width, screen.Height), _platform.Touch.Size, "Measured on the window, not on the bound canvas.");
        Assert.AreEqual(new RectF(4, 0, screen.Width - 8, screen.Height), _platform.Touch.SafeArea);
        var frame = ((IScreenView)_platform).WorldFrame;
        Assert.IsTrue(frame.Width > 160 && frame.Width <= screen.Width - 8, $"The frame is laid out on the window ({frame}).");
        _platform.EndFrame();
    }

    [TestMethod]
    public void A_new_run_on_the_same_page_starts_without_the_last_run_s_touches()
    {
        _page.Pointer(3, 0, 10, 10);
        _platform!.Dispose();
        _platform = SdlPlatform.ForPage(new PlatformRequest(new GameSettings("page test", 160, 90, 2)) { Hidden = true, Mute = true }, _page);
        Assert.AreEqual(0, Touches().Count, "Touches the ended run never read are dropped.");
        _page.Pointer(4, 0, 20, 20);
        Assert.AreEqual(1, Touches().Count, "The page still feeds the new run.");
    }

    [TestMethod]
    public void The_page_s_insets_density_and_touch_first_are_the_screen_s()
    {
        var size = _platform!.Touch.Size;
        Assert.IsTrue(size.X > 40 && size.Y > 40, $"The window has a size ({size}).");
        _page.Screen(10, 4, 6, 8, 2.5f, touchFirst: true);

        Assert.AreEqual(new RectF(10, 4, size.X - 16, size.Y - 12), _platform.Touch.SafeArea);
        Assert.AreEqual(2.5f, _platform.Touch.PixelsPerDp);
        Assert.IsTrue(_platform.Touch.TouchFirst);

        _page.Screen(-3, -3, -3, -3, 1, touchFirst: false);
        Assert.AreEqual(new RectF(0, 0, size.X, size.Y), _platform.Touch.SafeArea, "Negative insets count as none.");
        Assert.IsFalse(_platform.Touch.TouchFirst);
    }

    [TestMethod]
    public void A_hidden_page_suspends_the_game_and_cancels_held_fingers_until_it_is_shown()
    {
        var sink = new RecordingSink();
        _page.Pointer(1, 0, 50, 50);
        Assert.IsTrue(_platform!.PumpEvents(sink), "A visible page runs frames.");
        Assert.AreEqual(1, Touches().Count);

        _page.Hidden = true;
        Assert.IsFalse(_platform.PumpEvents(sink), "A hidden page runs no frame.");
        Assert.IsTrue(_platform.Suspended);
        CollectionAssert.AreEqual(new[] { new TouchEvent(1, TouchPhase.Canceled, new Vector2(50, 50)) }, Touches(), "The finger held when the page was hidden is cancelled.");
        Assert.IsFalse(_platform.PumpEvents(sink), "Still suspended while hidden.");

        _page.Hidden = false;
        Assert.IsTrue(_platform.PumpEvents(sink), "Frames run again once shown.");
        Assert.IsFalse(_platform.Suspended);
        CollectionAssert.AreEqual(new[] { "suspending", "resumed" }, sink.Lifecycle, "One suspension, one resumption.");
    }

    [TestMethod]
    public void Haptics_use_the_page_s_vibrator_only_where_touch_is_the_main_input()
    {
        var asked = new List<int>();
        _page.Vibrator = ms => { asked.Add(ms); return true; };
        Assert.IsFalse(_platform!.Touch.CanVibrate, "A page whose main input is not touch does not vibrate.");
        Assert.IsFalse(_platform.Touch.Vibrate(1, 30));

        _page.Screen(0, 0, 0, 0, 1, touchFirst: true);
        Assert.IsTrue(_platform.Touch.CanVibrate);
        Assert.IsTrue(_platform.Touch.Vibrate(0.5f, 0));
        CollectionAssert.AreEqual(new[] { 1 }, asked, "At least one millisecond is asked of the page.");
    }

    [TestMethod]
    public void A_desktop_sdl_window_without_a_page_keeps_its_own_screen_facts()
    {
        using var desktop = new SdlPlatform(new PlatformRequest(new GameSettings("desktop", 160, 90, 2)) { Hidden = true, Mute = true });
        _page.Screen(10, 10, 10, 10, 3, touchFirst: true);
        var size = desktop.Touch.Size;
        Assert.AreEqual(new RectF(0, 0, size.X, size.Y), desktop.Touch.SafeArea, "Another platform's page does not reach this window.");
        Assert.IsFalse(desktop.Touch.TouchFirst);
        Assert.IsTrue(desktop.CanBlock, "A desktop SDL window owns its thread.");
    }

    [TestMethod]
    public void The_browser_profile_runs_only_in_a_browser_and_the_desktop_one_everywhere_else()
    {
        var profile = SdlPlatform.BrowserProfile(_page);
        Assert.AreEqual("sdl", profile.Name);
        Assert.AreEqual("a browser page profile runs only in a browser", profile.UnsupportedReason);
        Assert.IsNull(SdlPlatform.Profile.UnsupportedReason);
        Assert.ThrowsExactly<ArgumentNullException>(() => SdlPlatform.BrowserProfile(null!));
    }

    private sealed class RecordingSink : IEngineEventSink
    {
        public List<string> Lifecycle { get; } = [];
        public void Resized(SurfaceResized size) { }
        public void FocusChanged(bool focused) { }
        public void Suspending() => Lifecycle.Add("suspending");
        public void Resumed() => Lifecycle.Add("resumed");
        public void QuitRequested() { }
    }
}
