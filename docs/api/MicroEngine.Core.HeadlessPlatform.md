# Class HeadlessPlatform

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Testing your game](../examples/testing.md)  

A windowless platform: deterministic time, no devices, and a renderer that only counts
calls. Used by automated tests and CI to play whole games through scripts.

```csharp
public sealed class HeadlessPlatform : IPlatform, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HeadlessPlatform](MicroEngine.Core.HeadlessPlatform.md)

#### Implements

[IPlatform](MicroEngine.Core.IPlatform.md), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

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

The same device as [SilentAudio](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_SilentAudio).

```csharp
public IAudioDevice Audio { get; }
```

#### Property Value

 [IAudioDevice](MicroEngine.Audio.IAudioDevice.md)

### <a id="MicroEngine_Core_HeadlessPlatform_CloseRequested"></a> CloseRequested

Set it to true to stop the loop at the next frame, as if the window closed.

```csharp
public bool CloseRequested { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessPlatform_Graphics"></a> Graphics

A [HeadlessGraphicsDevice](MicroEngine.Core.HeadlessGraphicsDevice.md): textures have a size but no pixels.

```csharp
public IGraphicsDevice Graphics { get; }
```

#### Property Value

 [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

### <a id="MicroEngine_Core_HeadlessPlatform_Input"></a> Input

The same source as [Keys](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Keys).

```csharp
public IInputSource Input { get; }
```

#### Property Value

 [IInputSource](MicroEngine.Input.IInputSource.md)

### <a id="MicroEngine_Core_HeadlessPlatform_Keys"></a> Keys

The input tests set by hand (keys, gamepad, pointer).

```csharp
public HeadlessInput Keys { get; }
```

#### Property Value

 [HeadlessInput](MicroEngine.Core.HeadlessInput.md)

### <a id="MicroEngine_Core_HeadlessPlatform_Now"></a> Now

Simulated time: 1/60 second per frame polled.

```csharp
public double Now { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Core_HeadlessPlatform_Profile"></a> Profile

The headless platform as a catalog entry (<code>--platform headless</code>, <code>--headless</code>).

```csharp
public static PlatformProfile Profile { get; }
```

#### Property Value

 [PlatformProfile](MicroEngine.Core.PlatformProfile.md)

### <a id="MicroEngine_Core_HeadlessPlatform_RealTime"></a> RealTime

Always false: the loop advances exactly one update per frame.

```csharp
public bool RealTime { get; }
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

Paths of the screenshots requested, in order; no file is written.

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

 [SilentAudioDevice](MicroEngine.Audio.SilentAudioDevice.md)

### <a id="MicroEngine_Core_HeadlessPlatform_Touch"></a> Touch

The same screen as [TouchScreen](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_TouchScreen).

```csharp
public ITouchScreen Touch { get; }
```

#### Property Value

 [ITouchScreen](MicroEngine.Input.ITouchScreen.md)

### <a id="MicroEngine_Core_HeadlessPlatform_TouchScreen"></a> TouchScreen

A touch screen tests can touch; by default 4× the canvas, not touch-first, no safe-area insets.

```csharp
public HeadlessTouchScreen TouchScreen { get; }
```

#### Property Value

 [HeadlessTouchScreen](MicroEngine.Core.HeadlessTouchScreen.md)

## Methods

### <a id="MicroEngine_Core_HeadlessPlatform_BeginFrame"></a> BeginFrame\(\)

Starts a frame and returns the renderer that draws into the virtual canvas.

```csharp
public IRenderer BeginFrame()
```

#### Returns

 [IRenderer](MicroEngine.Graphics.IRenderer.md)

### <a id="MicroEngine_Core_HeadlessPlatform_Dispose"></a> Dispose\(\)

Does nothing; there is nothing to release.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Core_HeadlessPlatform_EndFrame"></a> EndFrame\(\)

Finishes the frame and presents it.

```csharp
public void EndFrame()
```

### <a id="MicroEngine_Core_HeadlessPlatform_For_MicroEngine_Core_GameSettings_"></a> For\(GameSettings\)

A headless platform with the virtual size of <code class="paramref">settings</code>.

```csharp
public static HeadlessPlatform For(GameSettings settings)
```

#### Parameters

`settings` [GameSettings](MicroEngine.Core.GameSettings.md)

#### Returns

 [HeadlessPlatform](MicroEngine.Core.HeadlessPlatform.md)

### <a id="MicroEngine_Core_HeadlessPlatform_PollEvents"></a> PollEvents\(\)

Advances simulated time by one frame.

```csharp
public void PollEvents()
```

### <a id="MicroEngine_Core_HeadlessPlatform_SaveScreenshot_System_String_"></a> SaveScreenshot\(string\)

Records <code class="paramref">path</code> in [Screenshots](MicroEngine.Core.HeadlessPlatform.md#MicroEngine_Core_HeadlessPlatform_Screenshots) without writing a file.

```csharp
public void SaveScreenshot(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_HeadlessPlatform_ToggleFullscreen"></a> ToggleFullscreen\(\)

Does nothing.

```csharp
public void ToggleFullscreen()
```

</div>
