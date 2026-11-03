# Class FrameLayout

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

Where the virtual canvas goes in a window: the shared letterbox rule of every backend.

```csharp
public static class FrameLayout
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FrameLayout](MicroEngine.Input.FrameLayout.md)

## Methods

### <a id="MicroEngine_Input_FrameLayout_Fit_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_MicroEngine_Graphics_CanvasScaling_"></a> Fit\(Vector2, RectF, int, int, CanvasScaling\)

The largest integer scale of the canvas that fits inside the safe area, centred in it, so pixels stay square and
even. When the largest integer scale would fill less than 80 % of what fits (a 480×270 game on a 412-pixel-high
phone browser is 1× of a possible 1.5×), the frame scales fractionally instead: on small screens size matters
more than even pixels. With [Smooth](MicroEngine.Graphics.CanvasScaling.md) the frame always takes the largest size that fits.
On a portrait screen the frame sits at the top of the safe area, so on-screen controls get the space below.

```csharp
public static RectF Fit(Vector2 window, RectF safeArea, int virtualWidth, int virtualHeight, CanvasScaling scaling = CanvasScaling.Pixel)
```

#### Parameters

`window` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`safeArea` RectF

`virtualWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`virtualHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`scaling` CanvasScaling

#### Returns

 RectF

### <a id="MicroEngine_Input_FrameLayout_ToCanvas_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_"></a> ToCanvas\(Vector2, RectF, int, int\)

A window position in virtual canvas pixels for the frame <code class="paramref">frame</code>.

```csharp
public static Vector2 ToCanvas(Vector2 window, RectF frame, int virtualWidth, int virtualHeight)
```

#### Parameters

`window` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`frame` RectF

`virtualWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`virtualHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

</div>
