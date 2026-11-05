# Interface IViewCapture

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The captures of a platform that has a UI view ([IScreenView](MicroEngine.Core.IScreenView.md)), a capability of the SDL, raylib and headless
platforms ([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__)). A run's captures ([SaveScreenshot](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_SaveScreenshot_System_String_))
show what the player sees: a frame that drew a UI is captured as the window, the world scaled into its frame with the
UI view composited over it ([CaptureScreen](MicroEngine.Core.IViewCapture.md#MicroEngine_Core_IViewCapture_CaptureScreen)); a frame that drew none is captured as the canvas, as before
the UI view existed. Tests that need the world alone read [CaptureWorld](MicroEngine.Core.IViewCapture.md#MicroEngine_Core_IViewCapture_CaptureWorld) or write
[SaveWorldScreenshot](MicroEngine.Core.IViewCapture.md#MicroEngine_Core_IViewCapture_SaveWorldScreenshot_System_String_).

```csharp
public interface IViewCapture
```

## Properties

### <a id="MicroEngine_Core_IViewCapture_FrameHasUi"></a> FrameHasUi

Whether the last frame drew a UI view.

```csharp
bool FrameHasUi { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Core_IViewCapture_CaptureScreen"></a> CaptureScreen\(\)

What the player saw: with a UI, the window (letterbox, world, UI over it) at the window's size; without one, [CaptureWorld](MicroEngine.Core.IViewCapture.md#MicroEngine_Core_IViewCapture_CaptureWorld).

```csharp
PixelImage CaptureScreen()
```

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Core_IViewCapture_CaptureUi"></a> CaptureUi\(\)

The last frame's UI view at the window's size, premultiplied and transparent where nothing was drawn; null when the frame drew none.

```csharp
PixelImage? CaptureUi()
```

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)?

### <a id="MicroEngine_Core_IViewCapture_CaptureWorld"></a> CaptureWorld\(\)

The last frame's world view: the canvas, opaque, at the canvas's size.

```csharp
PixelImage CaptureWorld()
```

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Core_IViewCapture_SaveWorldScreenshot_System_String_"></a> SaveWorldScreenshot\(string\)

Writes the world view as a PNG, at the size the platform's own captures of the canvas have.

```csharp
void SaveWorldScreenshot(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
