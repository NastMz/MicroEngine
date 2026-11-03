# Enum AspectMode

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [Presentation](../manual/rendering.md#presentation-one-choice-per-field)  

What happens when the window's shape differs from the canvas's ([Aspect](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Aspect)).

```csharp
public enum AspectMode
```

## Fields

`Expand = 2` 

Keep the scale and square pixels, and grow the canvas along the window's longer side so it fills the area: a
wider window shows more of the world, never letterbox ([CanvasSize](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_CanvasSize_MicroEngine_RectF_System_Int32_System_Int32_)). The canvas's
configured size is its minimum.



`Keep = 0` 

Keep the canvas's shape; the rest of the window is letterbox.



`Stretch = 1` 

Fill the whole area, stretching the canvas to the window's shape (pixels stop being square).

</div>
