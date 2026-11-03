# Interface IPlatform

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Backends and platforms](../manual/backends.md)  

Everything a backend provides: the host ([IPlatformHost](MicroEngine.Platform.IPlatformHost.md): clock, events, main surface, capabilities,
whether it may block) plus, until the graphics, input and audio contracts are split from it, the renderer that draws
into the virtual canvas and the graphics, input and audio devices.

```csharp
public interface IPlatform : IPlatformHost, IDisposable
```

#### Implements

IPlatformHost, 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Remarks

Optional features are capabilities found with [TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__), never by casting a
platform: [IDisplayControl](MicroEngine.Core.IDisplayControl.md), [IPlatformDiagnostics](MicroEngine.Core.IPlatformDiagnostics.md), [ITouchScreen](MicroEngine.Input.ITouchScreen.md) (with its
vibration), [IDisplayPacing](MicroEngine.Platform.IDisplayPacing.md) and [IPreciseDelay](MicroEngine.Platform.IPreciseDelay.md). A platform that wraps another forwards
every member, so wrapping never hides a capability.

## Properties

### <a id="MicroEngine_Core_IPlatform_Audio"></a> Audio

Plays sound; a silent device when there is no audio.

```csharp
IAudioDevice Audio { get; }
```

#### Property Value

 IAudioDevice

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

 IInputSource

### <a id="MicroEngine_Core_IPlatform_Letterbox"></a> Letterbox

Colour of the window around the frame. Starts as [Letterbox](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Letterbox); the start screen sets its
own colour while it shows. Platforms without a window keep it and ignore it.

```csharp
Color Letterbox { get; set; }
```

#### Property Value

 Color

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

</div>
