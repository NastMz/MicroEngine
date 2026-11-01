# Class RaylibPlatform

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [Backends and platforms](../manual/backends.md)  

raylib-backed platform. Games draw into a virtual-resolution render texture which is
scaled to the window by the largest integer factor that fits (letterboxed), keeping pixel art crisp.
In a browser (raylib built for emscripten) the page paces frames: the window never asks to close and
raylib's frame limiter is left off, because both would block the only thread; the page also owns
fullscreen, and the canvas has no minimum size.

```csharp
public sealed class RaylibPlatform : IPlatform, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RaylibPlatform](MicroEngine.Raylib.RaylibPlatform.md)

#### Implements

[IPlatform](https://github.com/NastMz/MicroEngine/blob/4d9999002843ba740441ef7bb60461c13b2a816e/src/MicroEngine/Core/Platform.cs), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Constructors

### <a id="MicroEngine_Raylib_RaylibPlatform__ctor_MicroEngine_Core_GameSettings_MicroEngine_Raylib_DesktopOptions_"></a> RaylibPlatform\(GameSettings, DesktopOptions\)

Opens the raylib window (and audio device unless the options say otherwise) for <code class="paramref">settings</code>. Hosts usually go through [Profile](MicroEngine.Raylib.RaylibPlatform.md#MicroEngine_Raylib_RaylibPlatform_Profile) instead.

```csharp
public RaylibPlatform(GameSettings settings, DesktopOptions options)
```

#### Parameters

`settings` [GameSettings](https://github.com/NastMz/MicroEngine/blob/4d9999002843ba740441ef7bb60461c13b2a816e/src/MicroEngine/Core/Platform.cs)

`options` [DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)

## Properties

### <a id="MicroEngine_Raylib_RaylibPlatform_Audio"></a> Audio

Plays sound; a silent device when there is no audio.

```csharp
public IAudioDevice Audio { get; }
```

#### Property Value

 [IAudioDevice](https://github.com/NastMz/MicroEngine/blob/4d9999002843ba740441ef7bb60461c13b2a816e/src/MicroEngine/Audio/Audio.cs)

### <a id="MicroEngine_Raylib_RaylibPlatform_CloseRequested"></a> CloseRequested

Whether the user closed the window; the loop stops at the next frame.

```csharp
public bool CloseRequested { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_RaylibPlatform_Graphics"></a> Graphics

Creates textures.

```csharp
public IGraphicsDevice Graphics { get; }
```

#### Property Value

 [IGraphicsDevice](https://github.com/NastMz/MicroEngine/blob/4d9999002843ba740441ef7bb60461c13b2a816e/src/MicroEngine/Graphics/IGraphics.cs)

### <a id="MicroEngine_Raylib_RaylibPlatform_Input"></a> Input

Keyboard, gamepad and pointer state, read once per update.

```csharp
public IInputSource Input { get; }
```

#### Property Value

 [IInputSource](https://github.com/NastMz/MicroEngine/blob/4d9999002843ba740441ef7bb60461c13b2a816e/src/MicroEngine/Input/Input.cs)

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

 [PlatformProfile](https://github.com/NastMz/MicroEngine/blob/4d9999002843ba740441ef7bb60461c13b2a816e/src/MicroEngine/Core/PlatformProfiles.cs)

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

 [ITouchScreen](https://github.com/NastMz/MicroEngine/blob/4d9999002843ba740441ef7bb60461c13b2a816e/src/MicroEngine/Input/Touch.cs)

## Methods

### <a id="MicroEngine_Raylib_RaylibPlatform_BeginFrame"></a> BeginFrame\(\)

Starts a frame and returns the renderer that draws into the virtual canvas.

```csharp
public IRenderer BeginFrame()
```

#### Returns

 [IRenderer](https://github.com/NastMz/MicroEngine/blob/4d9999002843ba740441ef7bb60461c13b2a816e/src/MicroEngine/Graphics/IGraphics.cs)

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

### <a id="MicroEngine_Raylib_RaylibPlatform_ToggleFullscreen"></a> ToggleFullscreen\(\)

Switches between the window and a borderless full-screen window.

```csharp
public void ToggleFullscreen()
```
