# Interface IPlatform

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Backends and platforms](../manual/backends.md)  

Everything a backend provides: a window (or nothing, headless), input devices, audio,
texture creation and a renderer that draws into the virtual canvas.

```csharp
public interface IPlatform : IDisposable
```

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Properties

### <a id="MicroEngine_Core_IPlatform_Audio"></a> Audio

Plays sound; a silent device when there is no audio.

```csharp
IAudioDevice Audio { get; }
```

#### Property Value

 [IAudioDevice](MicroEngine.Audio.IAudioDevice.md)

### <a id="MicroEngine_Core_IPlatform_CloseRequested"></a> CloseRequested

Whether the user closed the window; the loop stops at the next frame.

```csharp
bool CloseRequested { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_IPlatform_DisplayRate"></a> DisplayRate

The refresh rate in Hz of the display that paces the frames (VSync), or 0 when frames are not paced to a display
or its rate is unknown (the default). The loop then counts elapsed time in refresh periods, which removes the
stutter of waking up irregularly around them.

```csharp
double DisplayRate { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_IPlatform_Graphics"></a> Graphics

Creates textures.

```csharp
IGraphicsDevice Graphics { get; }
```

#### Property Value

 [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

### <a id="MicroEngine_Core_IPlatform_Input"></a> Input

Keyboard, gamepad and pointer state, read once per update.

```csharp
IInputSource Input { get; }
```

#### Property Value

 [IInputSource](MicroEngine.Input.IInputSource.md)

### <a id="MicroEngine_Core_IPlatform_Letterbox"></a> Letterbox

Colour of the window around the frame. Starts as [Letterbox](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Letterbox); the start screen sets its
own colour while it shows. Platforms without a window keep the default, which ignores changes.

```csharp
Color Letterbox { get; set; }
```

#### Property Value

 Color

### <a id="MicroEngine_Core_IPlatform_Now"></a> Now

A monotonic clock in seconds, and nothing more: the loop measures each frame's time with it and decides itself
how many fixed steps that is ([TimeSettings](MicroEngine.Kernel.TimeSettings.md)). Never derive it from frames or the update rate.

```csharp
double Now { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_IPlatform_RealTime"></a> RealTime

The time mode a run on this platform uses when neither the run ([TimeMode](MicroEngine.Core.RunOptions.md#MicroEngine_Core_RunOptions_TimeMode)) nor the game
([Configure](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Configure_MicroEngine_Kernel_EngineBuilder_)) chooses one: true for real time (a desktop window), false for one step per frame
(headless, or a backend whose host asked for deterministic time with [FixedStep](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_FixedStep)).
A preference only; the loop owns the clock.

```csharp
bool RealTime { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_IPlatform_Suspended"></a> Suspended

Whether the system took the game away (a phone app sent to the background): while true the loop neither
simulates nor renders, music is paused and the game is told through <code>OnSuspended</code>/<code>OnResumed</code>.
Platforms without such a state keep the default.

```csharp
bool Suspended { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_IPlatform_Touch"></a> Touch

The touch screen, if the platform has one: touches for [VirtualControls](MicroEngine.Input.VirtualControls.md), the safe area and a layer
drawn over the frame. Platforms without touch keep the default (null).

```csharp
ITouchScreen? Touch { get; }
```

#### Property Value

 [ITouchScreen](MicroEngine.Input.ITouchScreen.md)?

## Methods

### <a id="MicroEngine_Core_IPlatform_BeginFrame"></a> BeginFrame\(\)

Starts a frame and returns the renderer that draws into the virtual canvas.

```csharp
IRenderer BeginFrame()
```

#### Returns

 [IRenderer](MicroEngine.Graphics.IRenderer.md)

### <a id="MicroEngine_Core_IPlatform_EndFrame"></a> EndFrame\(\)

Finishes the frame and presents it.

```csharp
void EndFrame()
```

### <a id="MicroEngine_Core_IPlatform_PollEvents"></a> PollEvents\(\)

Processes pending system events (window, keyboard, gamepad, touch). Called at the start of every frame.

```csharp
void PollEvents()
```

### <a id="MicroEngine_Core_IPlatform_SaveScreenshot_System_String_"></a> SaveScreenshot\(string\)

Writes the most recently completed virtual frame as a PNG.

```csharp
void SaveScreenshot(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_IPlatform_ToggleFullscreen"></a> ToggleFullscreen\(\)

Switches between window and full screen where the platform has a window; does nothing elsewhere.

```csharp
void ToggleFullscreen()
```

### <a id="MicroEngine_Core_IPlatform_Wait_System_Double_"></a> Wait\(double\)

Blocks for about <code class="paramref">seconds</code>, precisely enough to pace frames (the frame limit,
[FrameRateLimit](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_FrameRateLimit)); returns false when the platform cannot block its thread (a browser,
whose page drives the frames), and the loop then skips the frames that come early instead. The default sleeps and
then spins for the last milliseconds; backends override it with their precise timers.

```csharp
bool Wait(double seconds)
```

#### Parameters

`seconds` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
