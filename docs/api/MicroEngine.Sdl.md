# Namespace MicroEngine.Sdl

<div data-pagefind-weight="0.3">

The SDL 3 platform: Windows, Linux, macOS, Android and iOS.

Guide: [Backends and platforms](../manual/backends.md)  

### Classes

 [SdlAudioDevice](MicroEngine.Sdl.SdlAudioDevice.md)

SDL3 audio. SDL mixes every audio stream bound to a device, so the device keeps a pool of streams (voices) for sound
effects, each converting its clip's rate and channels and applying pitch (frequency ratio) and volume (gain); pan is
applied when the clip is copied into its voice. Music is one stream topped up from [Update](MicroEngine.Sdl.SdlAudioDevice.md#MicroEngine_Sdl_SdlAudioDevice_Update), decoded from
PCM or OGG Vorbis (NVorbis) in managed code. It works without a window, so a host can use it with another platform.

 [SdlPlatform](MicroEngine.Sdl.SdlPlatform.md)

SDL3-backed platform. Games draw through SDL's 2D renderer into a texture of the virtual size, which is scaled to
the window by the largest integer factor that fits (letterboxed), as on the raylib platform. SDL chooses the
graphics API per system (Direct3D, Metal, OpenGL, OpenGL ES, Vulkan, or its software renderer); the
<code>SDL_RENDER_DRIVER</code> environment variable forces one (for example <code>software</code> on a machine without a GPU).
Fingers go to the game's on-screen controls through [ITouchScreen](MicroEngine.Input.ITouchScreen.md) ([SdlTouchScreen](MicroEngine.Sdl.md)), which
also reports the window's safe area and density; a system that sends the app to the background suspends it
([Suspended](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_Suspended)).

</div>
