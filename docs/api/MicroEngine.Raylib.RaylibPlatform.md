# Class RaylibPlatform

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [Backends and platforms](../manual/backends.md)  

raylib-backed platform. Games draw into a virtual-resolution render texture which is
scaled to the window by the largest integer factor that fits (letterboxed), keeping pixel art crisp.
In a browser (raylib built for emscripten) the page paces frames: the window never asks to close and
raylib's frame limiter is left off, because both would block the only thread; the page also owns
fullscreen, and the canvas has no minimum size: it is a page-driven host ([CanBlock](MicroEngine.Raylib.RaylibPlatform.md#MicroEngine_Raylib_RaylibPlatform_CanBlock) is false), whose
surface size is the canvas's, read after start. Its capabilities ([TryGetCapability``1](MicroEngine.Raylib.RaylibPlatform.md#MicroEngine_Raylib_RaylibPlatform_TryGetCapability__1___0__)):
[IDisplayControl](MicroEngine.Core.IDisplayControl.md), [IDisplayPacing](MicroEngine.Platform.IDisplayPacing.md), [ITouchScreen](MicroEngine.Input.ITouchScreen.md) and, outside browsers,
[IPreciseDelay](MicroEngine.Platform.IPreciseDelay.md).

```csharp
public sealed class RaylibPlatform : IPlatform, IPlatformHost, IDisposable, IDisplayControl, IDisplayPacing, IPreciseDelay, IMonotonicClock, IScreenView, IViewCapture
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RaylibPlatform](MicroEngine.Raylib.RaylibPlatform.md)

#### Implements

IPlatform, 
IPlatformHost, 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable), 
IDisplayControl, 
IDisplayPacing, 
IPreciseDelay, 
IMonotonicClock, 
IScreenView, 
IViewCapture

## Constructors

### <a id="MicroEngine_Raylib_RaylibPlatform__ctor_MicroEngine_Core_GameSettings_MicroEngine_Raylib_DesktopOptions_"></a> RaylibPlatform\(GameSettings, DesktopOptions\)

Opens the raylib window (and audio device unless the options say otherwise) for <code class="paramref">settings</code>. Hosts usually go through [Profile](MicroEngine.Raylib.RaylibPlatform.md#MicroEngine_Raylib_RaylibPlatform_Profile) instead.

```csharp
public RaylibPlatform(GameSettings settings, DesktopOptions options)
```

#### Parameters

`settings` GameSettings

`options` [DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)

### <a id="MicroEngine_Raylib_RaylibPlatform__ctor_MicroEngine_Core_PlatformRequest_"></a> RaylibPlatform\(PlatformRequest\)

Opens the raylib window for <code class="paramref">request</code>: its window options, its audio device or raylib's own
(unless muted or silent), and its extra input sources read with raylib's.

```csharp
public RaylibPlatform(PlatformRequest request)
```

#### Parameters

`request` PlatformRequest

## Properties

### <a id="MicroEngine_Raylib_RaylibPlatform_Audio"></a> Audio

Plays sound; a silent device when there is no audio.

```csharp
public IAudioDevice Audio { get; }
```

#### Property Value

 IAudioDevice

### <a id="MicroEngine_Raylib_RaylibPlatform_CanBlock"></a> CanBlock

False in a browser, whose page drives the frames; true on desktop.

```csharp
public bool CanBlock { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_RaylibPlatform_Canvas"></a> Canvas

The canvas the game draws into, with the 2D batcher's counts for the last frame.

```csharp
public DeviceCanvas Canvas { get; }
```

#### Property Value

 DeviceCanvas

### <a id="MicroEngine_Raylib_RaylibPlatform_Clock"></a> Clock

raylib's timer (the platform is its own clock).

```csharp
public IMonotonicClock Clock { get; }
```

#### Property Value

 IMonotonicClock

### <a id="MicroEngine_Raylib_RaylibPlatform_CurrentDisplay"></a> CurrentDisplay

Index in [Displays](MicroEngine.Core.IDisplayControl.md#MicroEngine_Core_IDisplayControl_Displays) of the display the window is on; -1 when unknown.

```csharp
public int CurrentDisplay { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Raylib_RaylibPlatform_Device"></a> Device

The graphics device the platform draws with (raylib's OpenGL layer behind the device contract).

```csharp
public RaylibDevice Device { get; }
```

#### Property Value

 [RaylibDevice](MicroEngine.Raylib.RaylibDevice.md)

### <a id="MicroEngine_Raylib_RaylibPlatform_DisplayRate"></a> DisplayRate

The monitor's refresh rate when frames are paced to it (a visible real-time desktop window with VSync); 0 otherwise.

```csharp
public double DisplayRate { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Raylib_RaylibPlatform_Displays"></a> Displays

The monitors GLFW reports (none in a browser or on Android); GLFW lists the primary one first.

```csharp
public IReadOnlyList<DisplayInfo> Displays { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<DisplayInfo\>

### <a id="MicroEngine_Raylib_RaylibPlatform_Graphics"></a> Graphics

Creates textures.

```csharp
public IGraphicsDevice Graphics { get; }
```

#### Property Value

 IGraphicsDevice

### <a id="MicroEngine_Raylib_RaylibPlatform_Input"></a> Input

raylib's keyboard, gamepads and mouse as a polled source, read together with the request's extra sources. The
engine reads raylib's input as events instead (the [IInputBackend](MicroEngine.Input.IInputBackend.md) capability).

```csharp
public IInputSource Input { get; }
```

#### Property Value

 IInputSource

### <a id="MicroEngine_Raylib_RaylibPlatform_Letterbox"></a> Letterbox

Colour of the window around the frame. Starts as [Letterbox](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Letterbox); the start screen sets its
own colour while it shows. Platforms without a window keep it and ignore it.

```csharp
public Color Letterbox { get; set; }
```

#### Property Value

 Color

### <a id="MicroEngine_Raylib_RaylibPlatform_MainSurface"></a> MainSurface

The window, or the page's canvas in a browser: its size is the canvas's as it is now, never the size requested at creation.

```csharp
public ISurface MainSurface { get; }
```

#### Property Value

 ISurface

### <a id="MicroEngine_Raylib_RaylibPlatform_Now"></a> Now

Seconds since an arbitrary start (the host's creation); never decreases.

```csharp
public double Now { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Raylib_RaylibPlatform_Profile"></a> Profile

The raylib platform as a catalog entry. raylib has no iOS platform; its own audio device is not opened when the
request supplies another one.

```csharp
public static PlatformProfile Profile { get; }
```

#### Property Value

 PlatformProfile

### <a id="MicroEngine_Raylib_RaylibPlatform_Suspended"></a> Suspended

In a browser, a hidden page ([Hidden](MicroEngine.Raylib.BrowserPage.md#MicroEngine_Raylib_BrowserPage_Hidden) of the page the platform was built for); a desktop window never suspends.

```csharp
public bool Suspended { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_RaylibPlatform_Touch"></a> Touch

The touch screen (also the [ITouchScreen](MicroEngine.Input.ITouchScreen.md) capability).

```csharp
public ITouchScreen Touch { get; }
```

#### Property Value

 ITouchScreen

### <a id="MicroEngine_Raylib_RaylibPlatform_VSync"></a> VSync

Whether frames wait for the display's refresh.

```csharp
public bool VSync { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_RaylibPlatform_WindowMode"></a> WindowMode

The window's current mode.

```csharp
public WindowMode WindowMode { get; }
```

#### Property Value

 WindowMode

## Methods

### <a id="MicroEngine_Raylib_RaylibPlatform_BeginFrame"></a> BeginFrame\(\)

Starts a frame and returns the renderer that draws into the virtual canvas.

```csharp
public IRenderer BeginFrame()
```

#### Returns

 IRenderer

### <a id="MicroEngine_Raylib_RaylibPlatform_BrowserProfile_MicroEngine_Raylib_BrowserPage_"></a> BrowserProfile\(BrowserPage\)

The raylib platform of a browser page as a catalog entry (named <code>raylib</code>, like [Profile](MicroEngine.Raylib.RaylibPlatform.md#MicroEngine_Raylib_RaylibPlatform_Profile)): every
platform it builds reads touches, insets, visibility and vibration from <code class="paramref">page</code> only. The browser
host offers this profile; a game's composition may offer others.

```csharp
public static PlatformProfile BrowserProfile(BrowserPage page)
```

#### Parameters

`page` [BrowserPage](MicroEngine.Raylib.BrowserPage.md)

#### Returns

 PlatformProfile

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">page</code> is null.

### <a id="MicroEngine_Raylib_RaylibPlatform_Delay_System_Double_"></a> Delay\(double\)

Waits with raylib's timer (a sleep, then a short spin); never offered in a browser, whose page cannot be blocked.

```csharp
public void Delay(double seconds)
```

#### Parameters

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Raylib_RaylibPlatform_Dispose"></a> Dispose\(\)

Closes the window and the audio device the platform opened.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Raylib_RaylibPlatform_EndFrame"></a> EndFrame\(\)

Finishes the frame and presents it.

```csharp
public void EndFrame()
```

### <a id="MicroEngine_Raylib_RaylibPlatform_MoveToDisplay_System_Int32_"></a> MoveToDisplay\(int\)

Moves the window to monitor <code class="paramref">index</code> (GLFW's order) on desktop.

```csharp
public bool MoveToDisplay(int index)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_RaylibPlatform_PumpEvents_MicroEngine_Platform_IEngineEventSink_"></a> PumpEvents\(IEngineEventSink\)

Reads what raylib collected at the end of the last frame and delivers the engine's events: a hidden page's
suspension (browsers), the window's close request (desktop, once while it stays asked), focus changes and size
changes of the window or canvas.

```csharp
public bool PumpEvents(IEngineEventSink sink)
```

#### Parameters

`sink` IEngineEventSink

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_RaylibPlatform_SaveScreenshot_System_String_"></a> SaveScreenshot\(string\)

Writes the canvas as a PNG, opaque (alpha 255 everywhere) as on every backend: the frame is shown on an opaque backdrop.

```csharp
public void SaveScreenshot(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Raylib_RaylibPlatform_SetVSync_System_Boolean_"></a> SetVSync\(bool\)

Turns VSync on or off on a visible desktop window that may wait for the display; browsers and Android follow the display.

```csharp
public bool SetVSync(bool on)
```

#### Parameters

`on` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_RaylibPlatform_SetWindowMode_MicroEngine_Core_WindowMode_"></a> SetWindowMode\(WindowMode\)

Changes the window's mode on desktop (raylib's full screen is exclusive, its borderless mode is [Fullscreen](MicroEngine.Core.WindowMode.md)).

```csharp
public bool SetWindowMode(WindowMode mode)
```

#### Parameters

`mode` WindowMode

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_RaylibPlatform_ToggleFullscreen"></a> ToggleFullscreen\(\)

Switches between the window and a borderless full-screen window (exclusive full screen goes back to the window).

```csharp
public void ToggleFullscreen()
```

### <a id="MicroEngine_Raylib_RaylibPlatform_TryGetCapability__1___0__"></a> TryGetCapability<T\>\(out T?\)

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
