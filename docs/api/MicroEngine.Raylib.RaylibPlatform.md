# Class RaylibPlatform

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [Backends and platforms](../manual/backends.md)  

raylib-backed platform. Games draw into a virtual-resolution render texture which is
scaled to the window by the largest integer factor that fits (letterboxed), keeping pixel art crisp.
In a browser (raylib built for emscripten) the page paces frames: the window never asks to close and
raylib's frame limiter is left off, because both would block the only thread; the page also owns
fullscreen, and the canvas has no minimum size.

```csharp
public sealed class RaylibPlatform : IPlatform, IDisposable, IDisplayControl
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RaylibPlatform](MicroEngine.Raylib.RaylibPlatform.md)

#### Implements

IPlatform, 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable), 
IDisplayControl

## Constructors

### <a id="MicroEngine_Raylib_RaylibPlatform__ctor_MicroEngine_Core_GameSettings_MicroEngine_Raylib_DesktopOptions_"></a> RaylibPlatform\(GameSettings, DesktopOptions\)

Opens the raylib window (and audio device unless the options say otherwise) for <code class="paramref">settings</code>. Hosts usually go through [Profile](MicroEngine.Raylib.RaylibPlatform.md#MicroEngine_Raylib_RaylibPlatform_Profile) instead.

```csharp
public RaylibPlatform(GameSettings settings, DesktopOptions options)
```

#### Parameters

`settings` GameSettings

`options` [DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)

## Properties

### <a id="MicroEngine_Raylib_RaylibPlatform_Audio"></a> Audio

Plays sound; a silent device when there is no audio.

```csharp
public IAudioDevice Audio { get; }
```

#### Property Value

 IAudioDevice

### <a id="MicroEngine_Raylib_RaylibPlatform_CloseRequested"></a> CloseRequested

Whether the user closed the window; the loop stops at the next frame.

```csharp
public bool CloseRequested { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_RaylibPlatform_CurrentDisplay"></a> CurrentDisplay

Index in [Displays](MicroEngine.Core.IDisplayControl.md#MicroEngine_Core_IDisplayControl_Displays) of the display the window is on; -1 when unknown.

```csharp
public int CurrentDisplay { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

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

Keyboard, gamepad and pointer state, read once per update.

```csharp
public IInputSource Input { get; }
```

#### Property Value

 IInputSource

### <a id="MicroEngine_Raylib_RaylibPlatform_Letterbox"></a> Letterbox

Colour of the window around the frame. Starts as [Letterbox](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Letterbox); the start screen sets its
own colour while it shows. Platforms without a window keep the default, which ignores changes.

```csharp
public Color Letterbox { get; set; }
```

#### Property Value

 Color

### <a id="MicroEngine_Raylib_RaylibPlatform_Now"></a> Now

Monotonic time in seconds.

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

### <a id="MicroEngine_Raylib_RaylibPlatform_RealTime"></a> RealTime

Whether time advances with the wall clock (desktop) or one step per frame (headless).

```csharp
public bool RealTime { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_RaylibPlatform_Suspended"></a> Suspended

In a browser, a hidden page ([Hidden](MicroEngine.Raylib.BrowserLifecycle.md#MicroEngine_Raylib_BrowserLifecycle_Hidden)); a desktop window never suspends.

```csharp
public bool Suspended { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_RaylibPlatform_Touch"></a> Touch

The touch screen, if the platform has one: touches for [VirtualControls](MicroEngine.Input.VirtualControls.md), the safe area and a layer
drawn over the frame. Platforms without touch keep the default (null).

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

### <a id="MicroEngine_Raylib_RaylibPlatform_PollEvents"></a> PollEvents\(\)

Processes pending system events (window, keyboard, gamepad, touch). Called at the start of every frame.

```csharp
public void PollEvents()
```

### <a id="MicroEngine_Raylib_RaylibPlatform_SaveScreenshot_System_String_"></a> SaveScreenshot\(string\)

Writes the most recently completed virtual frame as a PNG.

```csharp
public void SaveScreenshot(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Raylib_RaylibPlatform_SetVSync_System_Boolean_"></a> SetVSync\(bool\)

Turns VSync on or off on a visible desktop window; browsers and Android follow the display.

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

### <a id="MicroEngine_Raylib_RaylibPlatform_Wait_System_Double_"></a> Wait\(double\)

Waits with raylib's timer (a sleep, then a short spin); a browser page cannot be blocked.

```csharp
public bool Wait(double seconds)
```

#### Parameters

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
