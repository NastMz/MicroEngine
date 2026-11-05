# Class HeadlessPlatform

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Testing your game](../examples/testing.md)  

A windowless platform: no devices, and a renderer that counts calls (or, with [Rasterize](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Rasterize), also records
them and draws them with the CPU reference rasterizer). Used by automated tests and CI to play whole games through
scripts. Its surface has the canvas's size; tests drive its events by hand ([Resize](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Resize_System_Int32_System_Int32_), [Focus](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Focus_System_Boolean_),
[Suspend](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Suspend), [Resume](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Resume), [RequestQuit](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_RequestQuit)), which the next event pump delivers in order.
Its run preset ([Preset](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Preset)) is deterministic time: a stated default of this testing composition that a
run or a game overrides. Nothing the platform does depends on the time mode.

```csharp
public sealed class HeadlessPlatform : IPlatform, IPlatformHost, IDisposable, IScreenView, IViewCapture
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HeadlessPlatform](MicroEngine.Core.HeadlessPlatform.md)

#### Implements

[IPlatform](MicroEngine.Core.IPlatform.md), 
IPlatformHost, 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable), 
[IScreenView](MicroEngine.Core.IScreenView.md), 
[IViewCapture](MicroEngine.Core.IViewCapture.md)

## Constructors

### <a id="MicroEngine_Core_HeadlessPlatform__ctor_System_Int32_System_Int32_"></a> HeadlessPlatform\(int, int\)

Creates a headless platform whose renderer reports a <code class="paramref">width</code> × <code class="paramref">height</code> canvas.

```csharp
public HeadlessPlatform(int width = 320, int height = 180)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Core_HeadlessPlatform_Audio"></a> Audio

The device the request supplied ([Audio](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_Audio)), otherwise [SilentAudio](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_SilentAudio).

```csharp
public IAudioDevice Audio { get; }
```

#### Property Value

 IAudioDevice

### <a id="MicroEngine_Core_HeadlessPlatform_CanBlock"></a> CanBlock

True: tests and CI may block.

```csharp
public bool CanBlock { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessPlatform_Clock"></a> Clock

A monotonic clock in seconds since the platform was created. The loop owns time: in deterministic time it never
reads this clock to decide how many steps a frame runs.

```csharp
public IMonotonicClock Clock { get; }
```

#### Property Value

 IMonotonicClock

### <a id="MicroEngine_Core_HeadlessPlatform_CloseRequested"></a> CloseRequested

Set it to true to ask the game to quit at the next event pump, as if the window's close button was pressed; the
request is delivered once while it stays true. Setting it back to false allows a later request.

```csharp
public bool CloseRequested { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessPlatform_Devices"></a> Devices

The platform's input backend (also its [IInputBackend](MicroEngine.Input.IInputBackend.md), [IKeyLabels](MicroEngine.Input.IKeyLabels.md),
[IGamepadRumble](MicroEngine.Input.IGamepadRumble.md) and [ITextInputControl](MicroEngine.Input.ITextInputControl.md) capabilities): tests send device events with it,
which the next frame routes like a real backend's.

```csharp
public HeadlessInputBackend Devices { get; }
```

#### Property Value

 [HeadlessInputBackend](MicroEngine.Core.HeadlessInputBackend.md)

### <a id="MicroEngine_Core_HeadlessPlatform_FullscreenToggles"></a> FullscreenToggles

How many times the game asked to switch between a window and full screen (there is no window to switch).

```csharp
public int FullscreenToggles { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_HeadlessPlatform_Graphics"></a> Graphics

A [HeadlessGraphicsDevice](MicroEngine.Core.HeadlessGraphicsDevice.md): textures have a size but no pixels, unless [Rasterize](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Rasterize) is on.

```csharp
public IGraphicsDevice Graphics { get; }
```

#### Property Value

 IGraphicsDevice

### <a id="MicroEngine_Core_HeadlessPlatform_Input"></a> Input

[Keys](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Keys), read together with the request's extra sources ([ExtraInput](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_ExtraInput)) when there are any.

```csharp
public IInputSource Input { get; }
```

#### Property Value

 IInputSource

### <a id="MicroEngine_Core_HeadlessPlatform_Keys"></a> Keys

The input tests set by hand (keys, gamepad, pointer), read every update as one polled device.

```csharp
public HeadlessInput Keys { get; }
```

#### Property Value

 [HeadlessInput](MicroEngine.Core.HeadlessInput.md)

### <a id="MicroEngine_Core_HeadlessPlatform_Letterbox"></a> Letterbox

The colour around the world in a capture of a frame that drew a UI ([CaptureScreen](MicroEngine.Core.IViewCapture.md#MicroEngine_Core_IViewCapture_CaptureScreen)); there is no window to paint.

```csharp
public Color Letterbox { get; set; }
```

#### Property Value

 Color

### <a id="MicroEngine_Core_HeadlessPlatform_MainSurface"></a> MainSurface

The surface, of the canvas's size until [Resize](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Resize_System_Int32_System_Int32_) changes it; it has no native window.

```csharp
public ISurface MainSurface { get; }
```

#### Property Value

 ISurface

### <a id="MicroEngine_Core_HeadlessPlatform_Presentation"></a> Presentation

How the canvas is placed in the surface for the UI view's mapping ([WorldFrame](MicroEngine.Core.IScreenView.md#MicroEngine_Core_IScreenView_WorldFrame)): the
game's presentation when the platform was made for its settings, else [PixelArt](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_PixelArt).

```csharp
public Presentation Presentation { get; init; }
```

#### Property Value

 Presentation

### <a id="MicroEngine_Core_HeadlessPlatform_Preset"></a> Preset

The run defaults this testing composition states ([RunPreset](MicroEngine.Core.RunPreset.md), offered as a capability):
[Testing](MicroEngine.Core.RunPreset.md#MicroEngine_Core_RunPreset_Testing) by default, so a game run on it through [GameRunner](MicroEngine.Core.GameRunner.md),
[GameLoop](MicroEngine.Core.GameLoop.md), [GameSession](MicroEngine.Core.GameSession.md) or [InitializeForTest](MicroEngine.Core.Game.md#MicroEngine_Core_Game_InitializeForTest_MicroEngine_Core_IPlatform_System_Int32_) advances one fixed step
per frame with no option. The run's [TimeMode](MicroEngine.Core.RunOptions.md#MicroEngine_Core_RunOptions_TimeMode) and the game's [Configure](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Configure_MicroEngine_Kernel_EngineBuilder_)
win over it; <code>Preset = new RunPreset()</code> when creating the platform makes runs that choose nothing real-time.
The platform behaves the same either way.

```csharp
public RunPreset Preset { get; init; }
```

#### Property Value

 [RunPreset](MicroEngine.Core.RunPreset.md)

### <a id="MicroEngine_Core_HeadlessPlatform_Profile"></a> Profile

The headless platform as a catalog entry (<code>--platform headless</code>, <code>--headless</code>).

```csharp
public static PlatformProfile Profile { get; }
```

#### Property Value

 [PlatformProfile](MicroEngine.Core.PlatformProfile.md)

### <a id="MicroEngine_Core_HeadlessPlatform_Rasterize"></a> Rasterize

When true, textures keep their pixels and the renderer records each frame's draws
([Commands](MicroEngine.Core.HeadlessRenderer.md#MicroEngine_Core_HeadlessRenderer_Commands)) and draws them on [Frame](MicroEngine.Core.HeadlessRenderer.md#MicroEngine_Core_HeadlessRenderer_Frame) with the CPU
reference rasterizer, following the engine's rendering rules; [SaveScreenshot](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_SaveScreenshot_System_String_) then writes that frame
as a PNG (at scale 1). False by default: the renderer only counts. Set it when creating the platform.

```csharp
public bool Rasterize { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessPlatform_Renderer"></a> Renderer

The renderer, typed so tests can read its counters.

```csharp
public HeadlessRenderer Renderer { get; }
```

#### Property Value

 [HeadlessRenderer](MicroEngine.Core.HeadlessRenderer.md)

### <a id="MicroEngine_Core_HeadlessPlatform_Screenshots"></a> Screenshots

Paths of the screenshots requested, in order; a file is written only when [Rasterize](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Rasterize) is on.

```csharp
public List<string> Screenshots { get; }
```

#### Property Value

 [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Core_HeadlessPlatform_SilentAudio"></a> SilentAudio

The silent audio device, typed so tests can read [Played](MicroEngine.Audio.SilentAudioDevice.md#MicroEngine_Audio_SilentAudioDevice_Played).

```csharp
public SilentAudioDevice SilentAudio { get; }
```

#### Property Value

 SilentAudioDevice

### <a id="MicroEngine_Core_HeadlessPlatform_Suspended"></a> Suspended

Whether [Suspend](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Suspend) sent the platform to the background (until [Resume](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Resume)).

```csharp
public bool Suspended { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessPlatform_Touch"></a> Touch

The same screen as [TouchScreen](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_TouchScreen).

```csharp
public ITouchScreen Touch { get; }
```

#### Property Value

 ITouchScreen

### <a id="MicroEngine_Core_HeadlessPlatform_TouchScreen"></a> TouchScreen

A touch screen tests can touch; by default 4× the canvas, not touch-first, no safe-area insets. Also the host's [ITouchScreen](MicroEngine.Input.ITouchScreen.md) capability.

```csharp
public HeadlessTouchScreen TouchScreen { get; }
```

#### Property Value

 [HeadlessTouchScreen](MicroEngine.Core.HeadlessTouchScreen.md)

### <a id="MicroEngine_Core_HeadlessPlatform_UiRenderer"></a> UiRenderer

The UI view's renderer of the current frame ([IScreenView](MicroEngine.Core.IScreenView.md)): the surface's size in pixels, one UI unit per
pixel. It counts draw calls; with [Rasterize](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Rasterize) on it also draws them, and a frame that drew a UI is
captured with it composited over the world ([IViewCapture](MicroEngine.Core.IViewCapture.md)).

```csharp
public HeadlessRenderer UiRenderer { get; }
```

#### Property Value

 [HeadlessRenderer](MicroEngine.Core.HeadlessRenderer.md)

## Methods

### <a id="MicroEngine_Core_HeadlessPlatform_BeginFrame"></a> BeginFrame\(\)

Starts a frame and returns the renderer that draws into the virtual canvas.

```csharp
public IRenderer BeginFrame()
```

#### Returns

 IRenderer

### <a id="MicroEngine_Core_HeadlessPlatform_Dispose"></a> Dispose\(\)

Disposes the audio device the request supplied, if any; nothing else holds resources.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Core_HeadlessPlatform_EndFrame"></a> EndFrame\(\)

Finishes the frame and presents it.

```csharp
public void EndFrame()
```

### <a id="MicroEngine_Core_HeadlessPlatform_Focus_System_Boolean_"></a> Focus\(bool\)

Gives (true) or takes (false) the input focus; the next pump delivers it when the focus changes.

```csharp
public void Focus(bool focused)
```

#### Parameters

`focused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessPlatform_For_MicroEngine_Core_GameSettings_"></a> For\(GameSettings\)

A headless platform with the virtual size of <code class="paramref">settings</code>.

```csharp
public static HeadlessPlatform For(GameSettings settings)
```

#### Parameters

`settings` [GameSettings](MicroEngine.Core.GameSettings.md)

#### Returns

 [HeadlessPlatform](MicroEngine.Core.HeadlessPlatform.md)

### <a id="MicroEngine_Core_HeadlessPlatform_For_MicroEngine_Core_GameSettings_System_Boolean_"></a> For\(GameSettings, bool\)

A headless platform with the virtual size of <code class="paramref">settings</code>, rasterizing when <code class="paramref">rasterize</code> is true (see [Rasterize](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Rasterize)).

```csharp
public static HeadlessPlatform For(GameSettings settings, bool rasterize)
```

#### Parameters

`settings` [GameSettings](MicroEngine.Core.GameSettings.md)

`rasterize` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [HeadlessPlatform](MicroEngine.Core.HeadlessPlatform.md)

### <a id="MicroEngine_Core_HeadlessPlatform_For_MicroEngine_Core_PlatformRequest_"></a> For\(PlatformRequest\)

A headless platform for <code class="paramref">request</code>: the virtual size of its settings, the audio device it supplies
([Audio](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_Audio)) and its extra input sources read with [Keys](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Keys).

```csharp
public static HeadlessPlatform For(PlatformRequest request)
```

#### Parameters

`request` [PlatformRequest](MicroEngine.Core.PlatformRequest.md)

#### Returns

 [HeadlessPlatform](MicroEngine.Core.HeadlessPlatform.md)

### <a id="MicroEngine_Core_HeadlessPlatform_PumpEvents_MicroEngine_Platform_IEngineEventSink_"></a> PumpEvents\(IEngineEventSink\)

Delivers the events the test hooks queued, in order; false while suspended.

```csharp
public bool PumpEvents(IEngineEventSink sink)
```

#### Parameters

`sink` IEngineEventSink

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessPlatform_RequestQuit"></a> RequestQuit\(\)

Asks the game to quit, as a window's close button does; the next pump delivers the request.

```csharp
public void RequestQuit()
```

### <a id="MicroEngine_Core_HeadlessPlatform_Resize_System_Int32_System_Int32_"></a> Resize\(int, int\)

Changes the surface's size; the next pump delivers the resize (once, when the size changed).

```csharp
public void Resize(int width, int height)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_HeadlessPlatform_Resume"></a> Resume\(\)

Brings the platform back; the next pump delivers <code>Resumed</code> after a <code>Suspending</code>.

```csharp
public void Resume()
```

### <a id="MicroEngine_Core_HeadlessPlatform_SaveScreenshot_System_String_"></a> SaveScreenshot\(string\)

Records <code class="paramref">path</code> in [Screenshots](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Screenshots); with [Rasterize](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Rasterize) on, also writes what the
player would see there as an opaque PNG at scale 1 ([CaptureScreen](MicroEngine.Core.IViewCapture.md#MicroEngine_Core_IViewCapture_CaptureScreen)): the canvas, or, for
a frame that drew a UI, the surface with the UI over the world.

```csharp
public void SaveScreenshot(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_HeadlessPlatform_Suspend"></a> Suspend\(\)

Sends the platform to the background, as a phone does; the next pump delivers <code>Suspending</code>.

```csharp
public void Suspend()
```

### <a id="MicroEngine_Core_HeadlessPlatform_ToggleFullscreen"></a> ToggleFullscreen\(\)

Counts the request in [FullscreenToggles](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_FullscreenToggles): there is no window to switch.

```csharp
public void ToggleFullscreen()
```

### <a id="MicroEngine_Core_HeadlessPlatform_TryGetCapability__1___0__"></a> TryGetCapability<T\>\(out T?\)

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
