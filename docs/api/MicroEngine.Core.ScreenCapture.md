# Class ScreenCapture

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Builds the image the player sees from the two views, as the platforms compose a frame.

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

`world` PixelImage

`ui` PixelImage?

`screen` \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

`frame` RectF

`letterbox` Color

`upscale` SamplerDescription

#### Returns

 PixelImage

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The screen size is not positive.

</div>
