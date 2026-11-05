# Class OverlayShapes

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.Touch.dll  
Guide: [Input](../manual/input.md)  

Shapes built from [IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md) triangles and rectangles, which never overlap, so translucent colours stay even.

```csharp
public static class OverlayShapes
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[OverlayShapes](MicroEngine.Input.OverlayShapes.md)

## Methods

### <a id="MicroEngine_Input_OverlayShapes_FillRoundedRect_MicroEngine_Input_IOverlayCanvas_MicroEngine_RectF_System_Single_MicroEngine_Color_"></a> FillRoundedRect\(IOverlayCanvas, RectF, float, Color\)

Fills a rectangle with rounded corners of <code class="paramref">corner</code> pixels.

```csharp
public static void FillRoundedRect(IOverlayCanvas canvas, RectF rect, float corner, Color color)
```

#### Parameters

`canvas` [IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md)

`rect` [RectF](MicroEngine.RectF.md)

`corner` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Input_OverlayShapes_RoundedRectOutline_MicroEngine_Input_IOverlayCanvas_MicroEngine_RectF_System_Single_System_Single_MicroEngine_Color_"></a> RoundedRectOutline\(IOverlayCanvas, RectF, float, float, Color\)

The outline of a rounded rectangle, <code class="paramref">thickness</code> pixels wide, drawn inward.

```csharp
public static void RoundedRectOutline(IOverlayCanvas canvas, RectF rect, float corner, float thickness, Color color)
```

#### Parameters

`canvas` [IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md)

`rect` [RectF](MicroEngine.RectF.md)

`corner` [float](https://learn.microsoft.com/dotnet/api/system.single)

`thickness` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` [Color](MicroEngine.Color.md)

</div>
