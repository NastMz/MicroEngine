# Interface ITouchOverlay

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

Something drawn over the game on the touch screen, such as [VirtualControls](MicroEngine.Input.VirtualControls.md).

```csharp
public interface ITouchOverlay
```

## Methods

### <a id="MicroEngine_Input_ITouchOverlay_Draw_MicroEngine_Input_IOverlayCanvas_"></a> Draw\(IOverlayCanvas\)

Draws the overlay in screen pixels.

```csharp
void Draw(IOverlayCanvas canvas)
```

#### Parameters

`canvas` [IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md)

### <a id="MicroEngine_Input_ITouchOverlay_Frame_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_MicroEngine_Graphics_CanvasScaling_"></a> Frame\(Vector2, RectF, int, int, CanvasScaling\)

Where the backend should draw the game frame in this window, so the overlay does not cover it. The default is
the plain letterbox rule ([Fit](MicroEngine.Input.FrameLayout.md#MicroEngine_Input_FrameLayout_Fit_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_MicroEngine_Graphics_CanvasScaling_)); [VirtualControls](MicroEngine.Input.VirtualControls.md) fits the frame around
its controls.

```csharp
RectF Frame(Vector2 window, RectF safeArea, int virtualWidth, int virtualHeight, CanvasScaling scaling = CanvasScaling.Pixel)
```

#### Parameters

`window` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`safeArea` RectF

`virtualWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`virtualHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`scaling` [CanvasScaling](MicroEngine.Graphics.CanvasScaling.md)

#### Returns

 RectF

</div>
