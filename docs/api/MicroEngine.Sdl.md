# Namespace MicroEngine.Sdl

<div data-pagefind-weight="0.3">

The SDL 3 platform: Windows, Linux, macOS, Android and iOS.

Guide: [Backends and platforms](../manual/backends.md)  

### Classes

 [SdlAudioDevice](MicroEngine.Sdl.SdlAudioDevice.md)

SDL3 audio: the engine's mixer ([MixerAudioDevice](MicroEngine.Audio.MixerAudioDevice.md), the same voices, priorities and music on every
backend) playing through SDL's default playback device. A feeder thread keeps [DefaultLatencyMilliseconds](MicroEngine.Sdl.SdlAudioDevice.md#MicroEngine_Sdl_SdlAudioDevice_DefaultLatencyMilliseconds)
of audio queued in an SDL audio stream (48 kHz stereo float), so main-thread stalls and garbage collections shorter
than that are not heard. It works without a window, so a host can use it with another platform.

 [SdlPlatform](MicroEngine.Sdl.SdlPlatform.md)

SDL3-backed platform. Games draw through SDL's 2D renderer into a texture of the virtual size, which is scaled to
the window by the largest integer factor that fits (letterboxed), as on the raylib platform. SDL chooses the
graphics API per system (Direct3D, Metal, OpenGL, OpenGL ES, Vulkan, or its software renderer); the
<code>SDL_RENDER_DRIVER</code> environment variable forces one (for example <code>software</code> on a machine without a GPU).
Fingers go to the game's on-screen controls through [ITouchScreen](MicroEngine.Input.ITouchScreen.md) ([SdlTouchScreen](MicroEngine.Sdl.md)), which
also reports the window's safe area and density; a system that sends the app to the background suspends it
([Suspending](MicroEngine.Platform.IEngineEventSink.md#MicroEngine_Platform_IEngineEventSink_Suspending)). Its capabilities ([TryGetCapability``1](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_TryGetCapability__1___0__)):
[IDisplayControl](MicroEngine.Core.IDisplayControl.md), [IPlatformDiagnostics](MicroEngine.Core.IPlatformDiagnostics.md), [IDisplayPacing](MicroEngine.Platform.IDisplayPacing.md),
[IPreciseDelay](MicroEngine.Platform.IPreciseDelay.md) and [ITouchScreen](MicroEngine.Input.ITouchScreen.md).

</div>
