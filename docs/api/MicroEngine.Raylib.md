# Namespace MicroEngine.Raylib

<div data-pagefind-weight="0.3">

The raylib platform: desktops and browsers (WebAssembly), with the browser launcher and the browser page it reads.

Guide: [Backends and platforms](../manual/backends.md)  

### Classes

 [BrowserLauncher](MicroEngine.Raylib.BrowserLauncher.md)

Entry point for browser builds (raylib compiled to WebAssembly): the shared launcher model ([GameHost](MicroEngine.Core.GameHost.md))
for a page. A page cannot block its only thread in a game loop, so this returns a [GameSession](MicroEngine.Core.GameSession.md): the page
starts it with a [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md) (a script in development packages), ticks it once per
<code>requestAnimationFrame</code> and may start it again after the game ends. Every run composes the game with the same
<code>Configure</code> entry point its desktop <code>Program.cs</code> uses, so what the game configures there (platforms, request
changes, engine services) reaches the browser too; a scripted run is in deterministic time with seed 1, exactly as on
desktop and phones. Saves go to localStorage by default ([DefaultStorage](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultStorage_System_String_)).

 [BrowserPage](MicroEngine.Raylib.BrowserPage.md)

One browser page's way into the raylib platform it hosts: the page listens to Pointer Events on the canvas and
forwards touch and pen pointers here, in canvas pixels, with the page's safe-area insets (CSS
<code>env(safe-area-inset-*)</code>) and density, its visibility and how it vibrates. The browser host
(src/MicroEngine.Cli/Hosts/web) owns one and calls these from its JavaScript exports; every platform it builds
([BrowserProfile](MicroEngine.Raylib.RaylibPlatform.md#MicroEngine_Raylib_RaylibPlatform_BrowserProfile_MicroEngine_Raylib_BrowserPage_)) reads this page and no other, so nothing is shared through the process.

 [DesktopLauncher](MicroEngine.Raylib.DesktopLauncher.md)

Entry point for desktop games on raylib only: the shared launcher model ([GameHost](MicroEngine.Core.GameHost.md), the arguments of
[GameLauncher](MicroEngine.Core.GameLauncher.md)) with the <code>raylib</code> and <code>headless</code> platforms. Games that should run on the
recommended platform of each system use <code>MicroEngine.Desktop.DesktopLauncher</code> instead.

 [DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)

Window options of a raylib platform created directly (browser hosts); hosts with a [PlatformRequest](MicroEngine.Core.PlatformRequest.md) use
[#ctor](MicroEngine.Raylib.RaylibPlatform.md). Every option is set by name
(<code>new DesktopOptions { Hidden = true, Mute = true }</code>): there is no positional constructor, so a call written for
the earlier positional shape (whose third argument was a fixed-step flag) fails to compile instead of meaning
something else.

 [RaylibDevice](MicroEngine.Raylib.RaylibDevice.md)

The tier-0 graphics device on raylib's OpenGL layer (rlgl): textures and render targets are OpenGL textures and
framebuffers, buffers are device-owned CPU memory whose triangles are sent through rlgl's batch, the built-in pipeline
is rlgl's default shader with the pass's blend factors, and samplers are texture parameters set before each draw.
Offscreen targets are rendered with row 0 at the top of the texture's memory, so uploads, sampling and readback need no
flipping and OpenGL's tie rule gives the contract's top-left rule; the window's target (bottom-up in OpenGL) moves
geometry up by 1/128 px for the same rule. Desktop OpenGL declares render targets, repeat and mirror addressing,
separate filters and custom blend states; in browsers (WebGL) only render targets are declared.

 [RaylibDeviceFactory](MicroEngine.Raylib.RaylibDeviceFactory.md)

Factory of the tier-0 raylib device ("raylib"). raylib owns one window and one OpenGL context per process, so the
device accepts only raylib's own window: the surface whose native window is raylib's, once raylib has opened it.

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
([BrowserPage](MicroEngine.Raylib.BrowserPage.md)), with their pointer ids, the page's safe-area insets and its density. A desktop raylib
window reports no touches; its controls still lay out for scripted touches.

</div>
