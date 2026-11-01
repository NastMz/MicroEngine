# Class SdlPlatform

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Sdl](MicroEngine.Sdl.md)  
Assembly: MicroEngine.Sdl.dll  
Guide: [Backends and platforms](../manual/backends.md)  

SDL3-backed platform. Games draw through SDL's 2D renderer into a texture of the virtual size, which is scaled to
the window by the largest integer factor that fits (letterboxed), as on the raylib platform. SDL chooses the
graphics API per system (Direct3D, Metal, OpenGL, OpenGL ES, Vulkan, or its software renderer); the
<code>SDL_RENDER_DRIVER</code> environment variable forces one (for example <code>software</code> on a machine without a GPU).
Fingers go to the game's on-screen controls through [ITouchScreen](MicroEngine.Input.ITouchScreen.md) ([SdlTouchScreen](MicroEngine.Sdl.md)), which
also reports the window's safe area and density; a system that sends the app to the background suspends it
([Suspended](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_Suspended)).

```csharp
public sealed class SdlPlatform : IPlatform, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SdlPlatform](MicroEngine.Sdl.SdlPlatform.md)

#### Implements

IPlatform, 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Constructors

### <a id="MicroEngine_Sdl_SdlPlatform__ctor_MicroEngine_Core_PlatformRequest_System_String_"></a> SdlPlatform\(PlatformRequest, string?\)

Opens the SDL window, renderer and (unless the request says otherwise) audio for <code class="paramref">request</code>. Hosts usually go through [Profile](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_Profile) or [CreateProfile](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_CreateProfile_System_String_) instead.

```csharp
public SdlPlatform(PlatformRequest request, string? renderer = null)
```

#### Parameters

`request` PlatformRequest

The game's settings and window options.

`renderer` [string](https://learn.microsoft.com/dotnet/api/system.string)?

An SDL render driver name (see [RenderDrivers](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_RenderDrivers)), or null to let SDL choose.

## Properties

### <a id="MicroEngine_Sdl_SdlPlatform_Audio"></a> Audio

Plays sound; a silent device when there is no audio.

```csharp
public IAudioDevice Audio { get; }
```

#### Property Value

 IAudioDevice

### <a id="MicroEngine_Sdl_SdlPlatform_CloseRequested"></a> CloseRequested

Whether the user closed the window; the loop stops at the next frame.

```csharp
public bool CloseRequested { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_SdlPlatform_Graphics"></a> Graphics

Creates textures.

```csharp
public IGraphicsDevice Graphics { get; }
```

#### Property Value

 IGraphicsDevice

### <a id="MicroEngine_Sdl_SdlPlatform_Input"></a> Input

Keyboard, gamepad and pointer state, read once per update.

```csharp
public IInputSource Input { get; }
```

#### Property Value

 IInputSource

### <a id="MicroEngine_Sdl_SdlPlatform_Now"></a> Now

Monotonic time in seconds.

```csharp
public double Now { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Sdl_SdlPlatform_Profile"></a> Profile

The SDL3 platform as a catalog entry. SDL3 is packaged for Windows, Linux, macOS, Android and iOS, not for browsers.

```csharp
public static PlatformProfile Profile { get; }
```

#### Property Value

 PlatformProfile

### <a id="MicroEngine_Sdl_SdlPlatform_RealTime"></a> RealTime

Whether time advances with the wall clock (desktop) or one step per frame (headless).

```csharp
public bool RealTime { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_SdlPlatform_RenderDrivers"></a> RenderDrivers

The render drivers this SDL build offers on this system, in SDL's order of preference.

```csharp
public static IReadOnlyList<string> RenderDrivers { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Sdl_SdlPlatform_RendererName"></a> RendererName

The graphics API SDL chose ("direct3d11", "metal", "opengl", "software"...), for logs and tests.

```csharp
public string RendererName { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Sdl_SdlPlatform_Suspended"></a> Suspended

Whether the system took the game away (a phone app sent to the background): while true the loop neither
simulates nor renders, music is paused and the game is told through <code>OnSuspended</code>/<code>OnResumed</code>.
Platforms without such a state keep the default.

```csharp
public bool Suspended { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_SdlPlatform_Touch"></a> Touch

The touch screen, if the platform has one: touches for [VirtualControls](MicroEngine.Input.VirtualControls.md), the safe area and a layer
drawn over the frame. Platforms without touch keep the default (null).

```csharp
public ITouchScreen Touch { get; }
```

#### Property Value

 ITouchScreen

## Methods

### <a id="MicroEngine_Sdl_SdlPlatform_BeginFrame"></a> BeginFrame\(\)

Starts a frame and returns the renderer that draws into the virtual canvas.

```csharp
public IRenderer BeginFrame()
```

#### Returns

 IRenderer

### <a id="MicroEngine_Sdl_SdlPlatform_CreateProfile_System_String_"></a> CreateProfile\(string?\)

The SDL3 platform with a fixed render driver ("direct3d11", "direct3d12", "metal", "opengl", "opengles2",
"vulkan", "gpu", "software"), named <code>sdl-&lt;driver&gt;</code>; null lets SDL choose (the <code>sdl</code> profile).
A driver this system lacks fails when the platform is created, naming the drivers it has.

```csharp
public static PlatformProfile CreateProfile(string? renderer = null)
```

#### Parameters

`renderer` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 PlatformProfile

### <a id="MicroEngine_Sdl_SdlPlatform_Dispose"></a> Dispose\(\)

Closes the window, the renderer and the audio device the platform opened.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Sdl_SdlPlatform_EndFrame"></a> EndFrame\(\)

Finishes the frame and presents it.

```csharp
public void EndFrame()
```

### <a id="MicroEngine_Sdl_SdlPlatform_PollEvents"></a> PollEvents\(\)

Processes pending system events (window, keyboard, gamepad, touch). Called at the start of every frame.

```csharp
public void PollEvents()
```

### <a id="MicroEngine_Sdl_SdlPlatform_SaveScreenshot_System_String_"></a> SaveScreenshot\(string\)

Writes the most recently completed virtual frame as a PNG.

```csharp
public void SaveScreenshot(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Sdl_SdlPlatform_ToString"></a> ToString\(\)

"SDL renderer" followed by the name of the render driver in use.

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Sdl_SdlPlatform_ToggleFullscreen"></a> ToggleFullscreen\(\)

Switches between window and full screen where the platform has a window; does nothing elsewhere.

```csharp
public void ToggleFullscreen()
```

</div>
