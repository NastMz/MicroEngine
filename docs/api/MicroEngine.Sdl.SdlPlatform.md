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
([Suspending](MicroEngine.Platform.IEngineEventSink.md#MicroEngine_Platform_IEngineEventSink_Suspending)). Its capabilities ([TryGetCapability``1](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_TryGetCapability__1___0__)):
[IDisplayControl](MicroEngine.Core.IDisplayControl.md), [IPlatformDiagnostics](MicroEngine.Core.IPlatformDiagnostics.md), [IDisplayPacing](MicroEngine.Platform.IDisplayPacing.md),
[ITouchScreen](MicroEngine.Input.ITouchScreen.md) and, outside browsers, [IPreciseDelay](MicroEngine.Platform.IPreciseDelay.md).

```csharp
public sealed class SdlPlatform : IPlatform, IPlatformHost, IDisposable, IPlatformDiagnostics, IDisplayControl, IDisplayPacing, IPreciseDelay, IMonotonicClock, IScreenView, IViewCapture
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SdlPlatform](MicroEngine.Sdl.SdlPlatform.md)

#### Implements

IPlatform, 
IPlatformHost, 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable), 
IPlatformDiagnostics, 
IDisplayControl, 
IDisplayPacing, 
IPreciseDelay, 
IMonotonicClock, 
IScreenView, 
IViewCapture

## Remarks

In a browser (SDL compiled to WebAssembly, [BrowserProfile](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_BrowserProfile_MicroEngine_Sdl_BrowserPage_)) the platform is page-driven
([CanBlock](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_CanBlock) is false): the page calls one frame per animation frame, so nothing waits (no VSync, no
delay, no wait while suspended); the window is the page's canvas, sized from the canvas after start; a hidden page
suspends the game; touches, safe-area insets, density and vibration come from the page ([BrowserPage](MicroEngine.Sdl.BrowserPage.md));
sound plays through the page's AudioWorklet ([SdlAudioDevice](MicroEngine.Sdl.SdlAudioDevice.md)); full screen and the window's place
belong to the page. A few SDL calls go through declarations of the engine's own there (<code>SdlWeb</code>), because .NET's
WebAssembly interpreter mispasses SDL3-CS's 64-bit enum parameters.

## Constructors

### <a id="MicroEngine_Sdl_SdlPlatform__ctor_MicroEngine_Core_PlatformRequest_System_String_"></a> SdlPlatform\(PlatformRequest, string?\)

Opens the SDL window, renderer and (unless the request says otherwise) audio for <code class="paramref">request</code>, with
the audio device and extra input sources the request supplies. Hosts usually go through [Profile](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_Profile) or
[CreateProfile](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_CreateProfile_System_String_) instead.

```csharp
public SdlPlatform(PlatformRequest request, string? renderer = null)
```

#### Parameters

`request` PlatformRequest

The game's settings and window options.

`renderer` [string](https://learn.microsoft.com/dotnet/api/system.string)?

An SDL render driver name (see [RenderDrivers](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_RenderDrivers)), or null to let SDL choose.

## Fields

### <a id="MicroEngine_Sdl_SdlPlatform_GpuDevice"></a> GpuDevice

The tier-1 graphics device ([Device](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_Device), <code>--graphics sdl-gpu</code>): SDL_GPU (Direct3D 12,
Vulkan, Metal) with custom shaders and depth, which the 3D path needs. A system without an SDL_GPU driver refuses
it when the platform starts, saying why. On-screen touch controls are not drawn on it.

```csharp
public const string GpuDevice = "sdl-gpu"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Sdl_SdlPlatform_RendererDevice"></a> RendererDevice

The default graphics device ([Device](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_Device)): SDL's 2D renderer behind the device contract, tier 0.

```csharp
public const string RendererDevice = "sdl-renderer"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Sdl_SdlPlatform_Audio"></a> Audio

Plays sound; a silent device when there is no audio.

```csharp
public IAudioDevice Audio { get; }
```

#### Property Value

 IAudioDevice

### <a id="MicroEngine_Sdl_SdlPlatform_CanBlock"></a> CanBlock

True on SDL's native hosts, which own their thread; false in a browser, whose page drives the frames.

```csharp
public bool CanBlock { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_SdlPlatform_Canvas"></a> Canvas

The canvas the game draws into, with the 2D batcher's counts for the last frame.

```csharp
public DeviceCanvas Canvas { get; }
```

#### Property Value

 DeviceCanvas

### <a id="MicroEngine_Sdl_SdlPlatform_Clock"></a> Clock

SDL's nanosecond ticks since the platform opened, in seconds (the platform is its own clock).

```csharp
public IMonotonicClock Clock { get; }
```

#### Property Value

 IMonotonicClock

### <a id="MicroEngine_Sdl_SdlPlatform_CurrentDisplay"></a> CurrentDisplay

Index in [Displays](MicroEngine.Core.IDisplayControl.md#MicroEngine_Core_IDisplayControl_Displays) of the display the window is on; -1 when unknown.

```csharp
public int CurrentDisplay { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Sdl_SdlPlatform_Device"></a> Device

The graphics device the platform draws with: SDL's renderer behind the device contract ([SdlRendererDevice](MicroEngine.Sdl.SdlRendererDevice.md)),
or SDL_GPU (<code>SdlGpuDevice</code>) when [Device](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_Device) is [GpuDevice](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_GpuDevice).

```csharp
public GraphicsDeviceBase Device { get; }
```

#### Property Value

 GraphicsDeviceBase

### <a id="MicroEngine_Sdl_SdlPlatform_Diagnostics"></a> Diagnostics

The facts, read when asked (a value may change, such as the display's refresh rate).

```csharp
public IReadOnlyDictionary<string, string> Diagnostics { get; }
```

#### Property Value

 [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Sdl_SdlPlatform_DisplayRate"></a> DisplayRate

The refresh rate of the display the window is on while VSync paces the frames; 0 otherwise.

```csharp
public double DisplayRate { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Sdl_SdlPlatform_Displays"></a> Displays

The displays the system reports, in its order.

```csharp
public IReadOnlyList<DisplayInfo> Displays { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<DisplayInfo\>

### <a id="MicroEngine_Sdl_SdlPlatform_Graphics"></a> Graphics

Creates textures.

```csharp
public IGraphicsDevice Graphics { get; }
```

#### Property Value

 IGraphicsDevice

### <a id="MicroEngine_Sdl_SdlPlatform_GraphicsDeviceName"></a> GraphicsDeviceName

The graphics device the platform draws with: [RendererDevice](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_RendererDevice) or [GpuDevice](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_GpuDevice).

```csharp
public string GraphicsDeviceName { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Sdl_SdlPlatform_Input"></a> Input

The keyboard, gamepads and mouse as a polled source, read together with the request's extra sources. The engine
reads SDL's input as events instead (the [IInputBackend](MicroEngine.Input.IInputBackend.md) capability).

```csharp
public IInputSource Input { get; }
```

#### Property Value

 IInputSource

### <a id="MicroEngine_Sdl_SdlPlatform_Letterbox"></a> Letterbox

Colour of the window around the frame. Starts as [Letterbox](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Letterbox); the start screen sets its
own colour while it shows. Platforms without a window keep it and ignore it.

```csharp
public Color Letterbox { get; set; }
```

#### Property Value

 Color

### <a id="MicroEngine_Sdl_SdlPlatform_MainSurface"></a> MainSurface

The window: an <code>SDL_Window*</code> as [NativeWindow](MicroEngine.Platform.ISurface.md#MicroEngine_Platform_ISurface_NativeWindow), its size in pixels and density.

```csharp
public ISurface MainSurface { get; }
```

#### Property Value

 ISurface

### <a id="MicroEngine_Sdl_SdlPlatform_Now"></a> Now

Seconds since an arbitrary start (the host's creation); never decreases.

```csharp
public double Now { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Sdl_SdlPlatform_Profile"></a> Profile

The SDL3 platform as a catalog entry for Windows, Linux, macOS, Android and iOS. A browser page uses
[BrowserProfile](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_BrowserProfile_MicroEngine_Sdl_BrowserPage_), which knows its page.

```csharp
public static PlatformProfile Profile { get; }
```

#### Property Value

 PlatformProfile

### <a id="MicroEngine_Sdl_SdlPlatform_RenderDrivers"></a> RenderDrivers

The render drivers this SDL build offers on this system, in SDL's order of preference.

```csharp
public static IReadOnlyList<string> RenderDrivers { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Sdl_SdlPlatform_RendererName"></a> RendererName

The graphics API in use, for logs and tests: SDL_Renderer's driver ("direct3d11", "metal", "opengl", "software"...),
or "sdl-gpu" and the SDL_GPU API ("sdl-gpu vulkan") on the tier-1 device.

```csharp
public string RendererName { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Sdl_SdlPlatform_Suspended"></a> Suspended

Whether the system took the app away (a phone app in the background, a hidden page); no frame runs meanwhile.

```csharp
public bool Suspended { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_SdlPlatform_Touch"></a> Touch

The touch screen (also the [ITouchScreen](MicroEngine.Input.ITouchScreen.md) capability).

```csharp
public ITouchScreen Touch { get; }
```

#### Property Value

 ITouchScreen

### <a id="MicroEngine_Sdl_SdlPlatform_VSync"></a> VSync

Whether frames wait for the display's refresh.

```csharp
public bool VSync { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_SdlPlatform_WindowMode"></a> WindowMode

The window's current mode.

```csharp
public WindowMode WindowMode { get; }
```

#### Property Value

 WindowMode

## Methods

### <a id="MicroEngine_Sdl_SdlPlatform_BeginFrame"></a> BeginFrame\(\)

Starts a frame and returns the renderer that draws into the virtual canvas.

```csharp
public IRenderer BeginFrame()
```

#### Returns

 IRenderer

### <a id="MicroEngine_Sdl_SdlPlatform_BrowserProfile_MicroEngine_Sdl_BrowserPage_"></a> BrowserProfile\(BrowserPage\)

The SDL3 platform of a browser page, named <code>sdl</code>: the WebGL canvas, page input and Web Audio sound, with the
touches, screen facts, visibility and vibration of <code class="paramref">page</code>. The browser host offers this
profile; a game's composition may offer others.

```csharp
public static PlatformProfile BrowserProfile(BrowserPage page)
```

#### Parameters

`page` [BrowserPage](MicroEngine.Sdl.BrowserPage.md)

#### Returns

 PlatformProfile

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">page</code> is null.

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

### <a id="MicroEngine_Sdl_SdlPlatform_Delay_System_Double_"></a> Delay\(double\)

Waits with SDL's precise delay (a high-resolution timer, then a short spin); never offered in a browser, whose page cannot be blocked.

```csharp
public void Delay(double seconds)
```

#### Parameters

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

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

### <a id="MicroEngine_Sdl_SdlPlatform_MoveToDisplay_System_Int32_"></a> MoveToDisplay\(int\)

Moves the window to the centre of display <code class="paramref">index</code>; a full-screen window follows to that display.

```csharp
public bool MoveToDisplay(int index)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_SdlPlatform_PumpEvents_MicroEngine_Platform_IEngineEventSink_"></a> PumpEvents\(IEngineEventSink\)

Processes SDL's pending events and delivers the engine's: a trip to the background (latched by the lifecycle
watch, so one that ended before this pump is still delivered as <code>Suspending</code> then <code>Resumed</code>), one quit
request however many close and quit events arrived together, focus changes and size changes of the window.
While suspended it waits 50 ms instead of spinning, except in a browser, whose page stops calling while hidden.

```csharp
public bool PumpEvents(IEngineEventSink sink)
```

#### Parameters

`sink` IEngineEventSink

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_SdlPlatform_SaveScreenshot_System_String_"></a> SaveScreenshot\(string\)

Writes the most recently completed virtual frame as a PNG.

```csharp
public void SaveScreenshot(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Sdl_SdlPlatform_SetVSync_System_Boolean_"></a> SetVSync\(bool\)

Turns VSync on or off; hidden windows and requests that do not wait for the display keep it off.

```csharp
public bool SetVSync(bool on)
```

#### Parameters

`on` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_SdlPlatform_SetWindowMode_MicroEngine_Core_WindowMode_"></a> SetWindowMode\(WindowMode\)

Changes the window's mode; phones keep theirs (always full screen), and a page owns its full screen (the Fullscreen API needs a gesture).

```csharp
public bool SetWindowMode(WindowMode mode)
```

#### Parameters

`mode` WindowMode

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_SdlPlatform_ToString"></a> ToString\(\)

"SDL renderer" followed by the name of the render driver in use, or "SDL sdl-gpu" and the API.

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Sdl_SdlPlatform_ToggleFullscreen"></a> ToggleFullscreen\(\)

Switches between the window and borderless full screen (exclusive full screen goes back to the window).

```csharp
public void ToggleFullscreen()
```

### <a id="MicroEngine_Sdl_SdlPlatform_TryGetCapability__1___0__"></a> TryGetCapability<T\>\(out T?\)

The optional feature of type <code class="typeparamref">T</code> this host offers, if it offers one.

```csharp
public bool TryGetCapability<T>(out T? capability) where T : class
```

#### Parameters

`capability` T?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T`

</div>
