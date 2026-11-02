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
public sealed class SdlPlatform : IPlatform, IDisposable, IPlatformDiagnostics, IDisplayControl
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SdlPlatform](MicroEngine.Sdl.SdlPlatform.md)

#### Implements

IPlatform, 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable), 
IPlatformDiagnostics, 
IDisplayControl

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

### <a id="MicroEngine_Sdl_SdlPlatform_CurrentDisplay"></a> CurrentDisplay

Index in [Displays](MicroEngine.Core.IDisplayControl.md#MicroEngine_Core_IDisplayControl_Displays) of the display the window is on; -1 when unknown.

```csharp
public int CurrentDisplay { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

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

### <a id="MicroEngine_Sdl_SdlPlatform_Input"></a> Input

Keyboard, gamepad and pointer state, read once per update.

```csharp
public IInputSource Input { get; }
```

#### Property Value

 IInputSource

### <a id="MicroEngine_Sdl_SdlPlatform_Letterbox"></a> Letterbox

Colour of the window around the frame. Starts as [Letterbox](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Letterbox); the start screen sets its
own colour while it shows. Platforms without a window keep the default, which ignores changes.

```csharp
public Color Letterbox { get; set; }
```

#### Property Value

 Color

### <a id="MicroEngine_Sdl_SdlPlatform_Now"></a> Now

A monotonic clock in seconds, and nothing more: the loop measures each frame's time with it and decides itself
how many fixed steps that is ([TimeSettings](MicroEngine.Kernel.TimeSettings.md)). Never derive it from frames or the update rate.

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

The time mode a run on this platform uses when neither the run ([TimeMode](MicroEngine.Core.RunOptions.md#MicroEngine_Core_RunOptions_TimeMode)) nor the game
([Configure](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Configure_MicroEngine_Kernel_EngineBuilder_)) chooses one: true for real time (a desktop window), false for one step per frame
(headless, or a backend whose host asked for deterministic time with [FixedStep](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_FixedStep)).
A preference only; the loop owns the clock.

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

### <a id="MicroEngine_Sdl_SdlPlatform_MoveToDisplay_System_Int32_"></a> MoveToDisplay\(int\)

Moves the window to the centre of display <code class="paramref">index</code>; a full-screen window follows to that display.

```csharp
public bool MoveToDisplay(int index)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

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

### <a id="MicroEngine_Sdl_SdlPlatform_SetVSync_System_Boolean_"></a> SetVSync\(bool\)

Turns VSync on or off; hidden and fixed-step (scripted) runs keep it off.

```csharp
public bool SetVSync(bool on)
```

#### Parameters

`on` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_SdlPlatform_SetWindowMode_MicroEngine_Core_WindowMode_"></a> SetWindowMode\(WindowMode\)

Changes the window's mode; phones keep theirs (always full screen).

```csharp
public bool SetWindowMode(WindowMode mode)
```

#### Parameters

`mode` WindowMode

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_SdlPlatform_ToString"></a> ToString\(\)

"SDL renderer" followed by the name of the render driver in use.

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

### <a id="MicroEngine_Sdl_SdlPlatform_Wait_System_Double_"></a> Wait\(double\)

Waits with SDL's precise delay (a high-resolution timer, then a short spin).

```csharp
public bool Wait(double seconds)
```

#### Parameters

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
