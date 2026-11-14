# Class ScreenCapture

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Builds the image the player sees from the two views, as the platforms compose a frame, prescale included.

```csharp
public static class ScreenCapture
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ScreenCapture](MicroEngine.Core.ScreenCapture.md)

## Methods

### <a id="MicroEngine_Core_ScreenCapture_Compose_MicroEngine_Graphics_PixelImage_MicroEngine_Graphics_PixelImage_System_ValueTuple_System_Int32_System_Int32__MicroEngine_RectF_MicroEngine_Color_MicroEngine_Graphics_Devices_SamplerDescription_"></a> Compose\(PixelImage, PixelImage?, \(int Width, int Height\), RectF, Color, SamplerDescription\)

The window of <code class="paramref">screen</code> pixels: <code class="paramref">letterbox</code> (opaque), the opaque
<code class="paramref">world</code> scaled into <code class="paramref">frame</code> with <code class="paramref">upscale</code>'s magnification filter,
then the premultiplied <code class="paramref">ui</code> (window-sized, or null) over everything.

```csharp
public static PixelImage Compose(PixelImage world, PixelImage? ui, (int Width, int Height) screen, RectF frame, Color letterbox, SamplerDescription upscale)
```

#### Parameters

`world` [PixelImage](MicroEngine.Graphics.PixelImage.md)

`ui` [PixelImage](MicroEngine.Graphics.PixelImage.md)?

`screen` \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

`frame` [RectF](MicroEngine.RectF.md)

`letterbox` [Color](MicroEngine.Color.md)

`upscale` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The screen size is not positive.

### <a id="MicroEngine_Core_ScreenCapture_Compose_MicroEngine_Graphics_PixelImage_MicroEngine_Graphics_PixelImage_System_ValueTuple_System_Int32_System_Int32__MicroEngine_RectF_MicroEngine_Color_MicroEngine_Graphics_Presentation_System_Int32_"></a> Compose\(PixelImage, PixelImage?, \(int Width, int Height\), RectF, Color, Presentation, int\)

The window as <code class="paramref">presentation</code> draws it: [Compose](MicroEngine.Core.ScreenCapture.md#MicroEngine_Core_ScreenCapture_Compose_MicroEngine_Graphics_PixelImage_MicroEngine_Graphics_PixelImage_System_ValueTuple_System_Int32_System_Int32__MicroEngine_RectF_MicroEngine_Color_MicroEngine_Graphics_Devices_SamplerDescription_)
with its upscale sampler, the world enlarged first by its prescale ([PrescaleFor](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_PrescaleFor_MicroEngine_RectF_System_Int32_System_Int32_System_Int32_)) with
nearest sampling, as a platform's window samples it. Without a prescale it is that composition exactly.

```csharp
public static PixelImage Compose(PixelImage world, PixelImage? ui, (int Width, int Height) screen, RectF frame, Color letterbox, Presentation presentation, int maxTargetSize = 2147483647)
```

#### Parameters

`world` [PixelImage](MicroEngine.Graphics.PixelImage.md)

The world view: the canvas, opaque.

`ui` [PixelImage](MicroEngine.Graphics.PixelImage.md)?

The UI view at the window's size, premultiplied, or null.

`screen` \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

The window's size in pixels.

`frame` [RectF](MicroEngine.RectF.md)

Where the canvas lands in the window, in window pixels.

`letterbox` [Color](MicroEngine.Color.md)

The colour of the window around the frame.

`presentation` [Presentation](MicroEngine.Graphics.Presentation.md)

The game's presentation: its upscale sampler and prescale.

`maxTargetSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The largest side the enlarged canvas may have: a device's largest render target.

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The screen size is not positive.

</div>
