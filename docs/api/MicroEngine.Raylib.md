# Namespace MicroEngine.Raylib

<div data-pagefind-weight="0.3">

The raylib platform: desktops and browsers (WebAssembly), with the browser launcher and browser storage.

Guide: [Backends and platforms](../manual/backends.md)  

### Classes

 [BrowserLauncher](MicroEngine.Raylib.BrowserLauncher.md)

Entry point for browser builds (raylib compiled to WebAssembly). A page cannot block its only thread in
a game loop, so this returns a [GameSession](MicroEngine.Core.GameSession.md): the page starts it, ticks it once per
<code>requestAnimationFrame</code> and may start it again after the game ends. Saves go to localStorage
([BrowserSaveStorage](MicroEngine.Raylib.BrowserSaveStorage.md)); that choice is made here, before any game is built, so games
construct their [SaveStore](MicroEngine.Core.SaveStore.md) exactly as on desktop.

 [BrowserLifecycle](MicroEngine.Raylib.BrowserLifecycle.md)

The page's visibility: a hidden page (another tab, the phone locked or switched away) suspends the game.

 [BrowserSaveStorage](MicroEngine.Raylib.BrowserSaveStorage.md)

Save slots in the page's localStorage under "microengine/&lt;game&gt;/&lt;slot&gt;". The browser's file system
is in memory and vanishes on reload; localStorage writes are synchronous and survive it. The page registers
the "microengine" module (see web/README.md) before the game starts.

 [BrowserTouch](MicroEngine.Raylib.BrowserTouch.md)

The browser page's way into the raylib platform's touch screen ([RaylibTouchScreen](MicroEngine.Raylib.RaylibTouchScreen.md)): the page listens to
Pointer Events on the canvas and forwards touch and pen pointers here, in canvas pixels, with the page's safe-area
insets (CSS <code>env(safe-area-inset-*)</code>) and density. The browser host (src/MicroEngine.Cli/Hosts/web) calls these from its JS exports.

 [DesktopLauncher](MicroEngine.Raylib.DesktopLauncher.md)

Entry point for desktop games on raylib only: [GameLauncher](MicroEngine.Core.GameLauncher.md) with the <code>raylib</code> and <code>headless</code>
platforms (see [GameLauncher](MicroEngine.Core.GameLauncher.md) for the arguments). Games that should run on the recommended platform of
each system use <code>MicroEngine.Desktop.DesktopLauncher</code> instead.

 [DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)

Window options of a raylib platform created directly (browser hosts); hosts with a [PlatformRequest](MicroEngine.Core.PlatformRequest.md) use [#ctor](MicroEngine.Raylib.RaylibPlatform.md).

 [RaylibPlatform](MicroEngine.Raylib.RaylibPlatform.md)

raylib-backed platform. Games draw into a virtual-resolution render texture which is
scaled to the window by the largest integer factor that fits (letterboxed), keeping pixel art crisp.
In a browser (raylib built for emscripten) the page paces frames: the window never asks to close and
raylib's frame limiter is left off, because both would block the only thread; the page also owns
fullscreen, and the canvas has no minimum size: it is a page-driven host ([CanBlock](MicroEngine.Raylib.RaylibPlatform.md#MicroEngine_Raylib_RaylibPlatform_CanBlock) is false), whose
surface size is the canvas's, read after start. Its capabilities ([TryGetCapability``1](MicroEngine.Raylib.RaylibPlatform.md#MicroEngine_Raylib_RaylibPlatform_TryGetCapability__1___0__)):
[IDisplayControl](MicroEngine.Core.IDisplayControl.md), [IDisplayPacing](MicroEngine.Platform.IDisplayPacing.md), [ITouchScreen](MicroEngine.Input.ITouchScreen.md) and, outside browsers,
[IPreciseDelay](MicroEngine.Platform.IPreciseDelay.md).

 [RaylibTouchScreen](MicroEngine.Raylib.RaylibTouchScreen.md)

Touches for the raylib platform. raylib's own touch state is a per-frame list of points indexed by position (no
cancel, no reliable identity across frames), so it is not used: in a browser the page forwards Pointer Events
([BrowserTouch](MicroEngine.Raylib.BrowserTouch.md)), with their pointer ids, the page's safe-area insets and its density. A desktop raylib
window reports no touches; its controls still lay out for scripted touches.

</div>
