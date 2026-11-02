# Interface IOverlayCanvas

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

Primitive drawing in screen pixels, implemented by each backend for [ITouchOverlay](MicroEngine.Input.ITouchOverlay.md).

```csharp
public interface IOverlayCanvas
```

## Methods

### <a id="MicroEngine_Input_IOverlayCanvas_FillCircle_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> FillCircle\(Vector2, float, Color\)

Fills a circle.

```csharp
void FillCircle(Vector2 center, float radius, Color color)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` Color

### <a id="MicroEngine_Input_IOverlayCanvas_FillRect_MicroEngine_RectF_MicroEngine_Color_"></a> FillRect\(RectF, Color\)

Fills a rectangle.

```csharp
void FillRect(RectF rect, Color color)
```

#### Parameters

`rect` RectF

`color` Color

### <a id="MicroEngine_Input_IOverlayCanvas_FillTriangle_System_Numerics_Vector2_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Color_"></a> FillTriangle\(Vector2, Vector2, Vector2, Color\)

Fills a triangle.

```csharp
void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
```

#### Parameters

`a` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`b` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`c` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` Color

### <a id="MicroEngine_Input_IOverlayCanvas_Image_MicroEngine_Graphics_PixelImage_MicroEngine_RectF_MicroEngine_Color_"></a> Image\(PixelImage, RectF, Color\)

Draws <code class="paramref">image</code> stretched over <code class="paramref">destination</code> without smoothing, multiplied by
<code class="paramref">tint</code> (white keeps its colours; the alpha fades it). Backends keep one texture per image, so the
same [PixelImage](MicroEngine.Graphics.PixelImage.md) instance should be passed every frame. Canvases that cannot draw images
keep the default, which draws nothing.

```csharp
void Image(PixelImage image, RectF destination, Color tint)
```

#### Parameters

`image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

`destination` RectF

`tint` Color

### <a id="MicroEngine_Input_IOverlayCanvas_Ring_System_Numerics_Vector2_System_Single_System_Single_MicroEngine_Color_"></a> Ring\(Vector2, float, float, Color\)

A circle outline of the given thickness, drawn inward from <code class="paramref">radius</code>.

```csharp
void Ring(Vector2 center, float radius, float thickness, Color color)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`thickness` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` Color

### <a id="MicroEngine_Input_IOverlayCanvas_Text_System_String_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> Text\(string, Vector2, float, Color\)

Short text centred on <code class="paramref">center</code>, about <code class="paramref">height</code> pixels tall.

```csharp
void Text(string text, Vector2 center, float height, Color color)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`height` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` Color

</div>
