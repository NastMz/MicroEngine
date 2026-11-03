# Namespace MicroEngine.Platform

<div data-pagefind-weight="0.3">

The platform contracts: the host (clock, event pump, main surface, capabilities, whether it may block), the surface, display pacing and precise waits, storage for saves and content sources.

Guide: [The platform host](../manual/backends.md#the-platform-host)  

### Classes

 [FolderContentSource](MicroEngine.Platform.FolderContentSource.md)

Content files in a folder on disk. Names must match the files' letter case exactly on every system: "Hero.png" for
"hero.png" works on Windows and macOS but not on Linux, Android or in browsers, so it fails everywhere, naming the
real spelling. Paths that leave the folder (<code>..</code>, another drive) are refused.

 [HostSurface](MicroEngine.Platform.HostSurface.md)

An [ISurface](MicroEngine.Platform.ISurface.md) for hosts: it reads its size, density and state from the host's window when asked, and
[Pump](MicroEngine.Platform.HostSurface.md#MicroEngine_Platform_HostSurface_Pump_MicroEngine_Platform_IEngineEventSink_) reports a size change the way the platform contract requires ([Resized](MicroEngine.Platform.HostSurface.md#MicroEngine_Platform_HostSurface_Resized) and the
sink's [Resized](MicroEngine.Platform.IEngineEventSink.md#MicroEngine_Platform_IEngineEventSink_Resized_MicroEngine_Kernel_SurfaceResized_), once per change).

### Interfaces

 [IContentSource](MicroEngine.Platform.IContentSource.md)

Where a game's content files come from: a folder next to the executable, a phone's unpacked package, later an
archive or a browser's fetch. Paths are relative to the source with '/' separators and match the stored name's
letter case exactly on every system.

 [IDisplayPacing](MicroEngine.Platform.IDisplayPacing.md)

The refresh rate a host's frames are paced to, when a display paces them (VSync). A capability
([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__)): a host without it never paces frames to a display.

 [IEngineEventSink](MicroEngine.Platform.IEngineEventSink.md)

Where a host delivers what happens to the game from outside its simulation ([PumpEvents](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_PumpEvents_MicroEngine_Platform_IEngineEventSink_)).
The engine's loop implements it and raises the matching [IEngineLifecycle](MicroEngine.Kernel.IEngineLifecycle.md) events.

 [IMonotonicClock](MicroEngine.Platform.IMonotonicClock.md)

A monotonic clock in seconds: it never goes back and does not depend on frames, steps or rates.

 [IPlatformHost](MicroEngine.Platform.IPlatformHost.md)

The platform a game runs on, as the loop sees it: a monotonic clock, the events of the window and the system, the
main surface, optional features found by type and whether the host may block its thread.

 [IPreciseDelay](MicroEngine.Platform.IPreciseDelay.md)

A precise way to wait, used by the loop to hold a frame back until the frame limit allows it. A capability
([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__)) of hosts that can block ([CanBlock](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_CanBlock));
without it the loop sleeps and then spins for the last milliseconds.

 [IStorage](MicroEngine.Platform.IStorage.md)

Where a game keeps what it saves: named slots of text that are written whole (files on desktops and phones, the
page's localStorage in browsers, a cloud service). Failures surface as `IOException` or
`UnauthorizedAccessException`.

 [ISurface](MicroEngine.Platform.ISurface.md)

A surface a host produces and a graphics device presents to: a window, a browser canvas, a phone's screen. This is
the graphics contract's surface (version 1); only backends read [NativeWindow](MicroEngine.Platform.ISurface.md#MicroEngine_Platform_ISurface_NativeWindow).

</div>
