# Interface ITouchSkin

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

Draws touch controls. The engine calls it for each shown control with its state; a game may replace the whole look
([Skin](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_Skin)) and still delegate kinds it does not change to [Instance](MicroEngine.Input.DefaultTouchSkin.md#MicroEngine_Input_DefaultTouchSkin_Instance).

```csharp
public interface ITouchSkin
```

## Methods

### <a id="MicroEngine_Input_ITouchSkin_Draw_MicroEngine_Input_IOverlayCanvas_MicroEngine_Input_TouchControlView__"></a> Draw\(IOverlayCanvas, in TouchControlView\)

Draws one control.

```csharp
void Draw(IOverlayCanvas canvas, in TouchControlView view)
```

#### Parameters

`canvas` [IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md)

`view` [TouchControlView](MicroEngine.Input.TouchControlView.md)

</div>
