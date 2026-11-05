# Namespace MicroEngine.Raylib

<div data-pagefind-weight="0.3">

The raylib platform for desktops (in the published SDK, also the browser platform).

Guide: [Backends and platforms](../manual/backends.md)  

### Classes

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

 [RaylibDeviceFactory](MicroEngine.Raylib.RaylibDeviceFactory.md)

Factory of the tier-0 raylib device ("raylib"). raylib owns one window and one OpenGL context per process, so the
device accepts only raylib's own window: the surface whose native window is raylib's, once raylib has opened it.

 [RaylibDevice](MicroEngine.Raylib.RaylibDevice.md)

The tier-0 graphics device on raylib's OpenGL layer (rlgl): textures and render targets are OpenGL textures and
framebuffers, buffers are device-owned CPU memory whose triangles are sent through rlgl's batch, the built-in pipeline
is rlgl's default shader with the pass's blend factors, and samplers are texture parameters set before each draw.
Offscreen targets are rendered with row 0 at the top of the texture's memory, so uploads, sampling and readback need no
flipping and OpenGL's tie rule gives the contract's top-left rule; the window's target (bottom-up in OpenGL) moves
geometry up by 1/128 px for the same rule. Desktop OpenGL declares render targets, repeat and mirror addressing,
separate filters and custom blend states; in browsers (WebGL) only render targets are declared.

 [RaylibPlatform](MicroEngine.Raylib.RaylibPlatform.md)

raylib-backed platform. Games draw into a virtual-resolution render texture which is
scaled to the window by the largest integer factor that fits (letterboxed), keeping pixel art crisp. A desktop
platform (the second implementation of the platform contract beside SDL); browsers run the SDL platform. Its
capabilities ([TryGetCapability``1](MicroEngine.Raylib.RaylibPlatform.md#MicroEngine_Raylib_RaylibPlatform_TryGetCapability__1___0__)): [IDisplayControl](MicroEngine.Core.IDisplayControl.md), [IDisplayPacing](MicroEngine.Platform.IDisplayPacing.md),
[ITouchScreen](MicroEngine.Input.ITouchScreen.md) and [IPreciseDelay](MicroEngine.Platform.IPreciseDelay.md).

 [RaylibTouchScreen](MicroEngine.Raylib.RaylibTouchScreen.md)

Touches for the raylib platform. raylib's own touch state is a per-frame list of points indexed by position (no
cancel, no reliable identity across frames), so it is not used: a raylib window reports no touches; its controls
still lay out for scripted touches.

</div>
