# Namespace MicroEngine.Sdl

<div data-pagefind-weight="0.3">

The SDL 3 platform: Windows, Linux, macOS, Android, iOS and browsers (WebAssembly), with the browser launcher and the browser page it reads.

Guide: [Backends and platforms](../manual/backends.md)  

### Classes

 [BrowserLauncher](MicroEngine.Sdl.BrowserLauncher.md)

Entry point for browser builds (SDL 3 compiled to WebAssembly): the shared launcher model ([GameHost](MicroEngine.Core.GameHost.md))
for a page. A page cannot block its only thread in a game loop, so this returns a [GameSession](MicroEngine.Core.GameSession.md): the page
starts it with a [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md) (a script in development packages), ticks it once per
<code>requestAnimationFrame</code> and may start it again after the game ends. Every run composes the game with the same
<code>Configure</code> entry point its desktop <code>Program.cs</code> uses, so what the game configures there (platforms, request
changes, engine services) reaches the browser too; a scripted run is in deterministic time with seed 1, exactly as on
desktop and phones. Saves go to localStorage by default ([DefaultStorage](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultStorage_System_String_)).

 [BrowserPage](MicroEngine.Sdl.BrowserPage.md)

One browser page's way into the SDL platform it hosts. SDL reads the keyboard, the mouse and gamepads from the page
itself; what it cannot know comes from here: the page listens to Pointer Events on the canvas and forwards touch and
pen pointers, in canvas pixels, with the page's safe-area insets (CSS <code>env(safe-area-inset-*)</code>) and density,
its visibility and how it vibrates. The browser host (src/MicroEngine.Cli/Hosts/web) owns one and calls these from
its JavaScript exports; every platform it builds ([BrowserProfile](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_BrowserProfile_MicroEngine_Sdl_BrowserPage_)) reads this page and no
other, so nothing is shared through the process.

 [SdlAudioDevice](MicroEngine.Sdl.SdlAudioDevice.md)

SDL3 audio: the engine's mixer ([MixerAudioDevice](MicroEngine.Audio.MixerAudioDevice.md), the same voices, priorities and music on every
backend) playing through SDL's default playback device. A feeder thread keeps [DefaultLatencyMilliseconds](MicroEngine.Sdl.SdlAudioDevice.md#MicroEngine_Sdl_SdlAudioDevice_DefaultLatencyMilliseconds)
of audio queued in an SDL audio stream (48 kHz stereo float), so main-thread stalls and garbage collections shorter
than that are not heard. It works without a window, so a host can use it with another platform. In a browser SDL's
own audio is not used: the mixer plays through the page's AudioWorklet queue, filled once per frame
([BrowserAudioSink](MicroEngine.Sdl.md)).

 [SdlPlatform](MicroEngine.Sdl.SdlPlatform.md)

SDL3-backed platform. Games draw through SDL's 2D renderer into a texture of the virtual size, which is scaled to
the window by the largest integer factor that fits (letterboxed), as on the raylib platform. SDL chooses the
graphics API per system (Direct3D, Metal, OpenGL, OpenGL ES, Vulkan, or its software renderer); the
<code>SDL_RENDER_DRIVER</code> environment variable forces one (for example <code>software</code> on a machine without a GPU).
Fingers go to the game's on-screen controls through [ITouchScreen](MicroEngine.Input.ITouchScreen.md) ([SdlTouchScreen](MicroEngine.Sdl.md)), which
also reports the window's safe area and density; a system that sends the app to the background suspends it
([Suspending](MicroEngine.Platform.IEngineEventSink.md#MicroEngine_Platform_IEngineEventSink_Suspending)). Its capabilities ([TryGetCapability``1](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_TryGetCapability__1___0__)):
[IDisplayControl](MicroEngine.Core.IDisplayControl.md), [IPlatformDiagnostics](MicroEngine.Core.IPlatformDiagnostics.md), [IDisplayPacing](MicroEngine.Platform.IDisplayPacing.md),
[ITouchScreen](MicroEngine.Input.ITouchScreen.md) and, outside browsers, [IPreciseDelay](MicroEngine.Platform.IPreciseDelay.md).

 [SdlRendererBackendFactory](MicroEngine.Sdl.SdlRendererBackendFactory.md)

Factory of the tier-0 SDL_Renderer backend ("sdl-renderer").

 [SdlRendererDevice](MicroEngine.Sdl.SdlRendererDevice.md)

The tier-0 graphics device on SDL_Renderer, with SDL's default render driver unless
[Api](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md#MicroEngine_Graphics_Devices_GraphicsDeviceOptions_Api) names one ("direct3d11", "direct3d12", "opengl", "vulkan", "metal", "gpu",
"software"...). Buffers are device-owned CPU memory submitted through <code>SDL_RenderGeometryRaw</code> (the
[Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md) layout is passed with strides, without conversion); the built-in pipeline is an SDL blend mode;
samplers are SDL scale and address modes applied per draw; command lists execute inside
[Submit](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Submit_MicroEngine_Graphics_Devices_CommandList_) into SDL's own queue, which SDL flushes at readback and present. Declared
features: render targets, repeat addressing and, where the driver composes every blend factor, custom blend states.
SDL's <code>software</code> driver is a diagnostic device: its triangle coverage differs from the contract's pixel-centre
rule along rotated edges, and it does not claim conformance.

</div>
