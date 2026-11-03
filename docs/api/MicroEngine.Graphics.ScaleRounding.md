# Enum ScaleRounding

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [Presentation](../manual/rendering.md#presentation-one-choice-per-field)  

How the canvas's scale to the window is rounded ([Rounding](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Rounding)).

```csharp
public enum ScaleRounding
```

## Fields

`Fractional = 0` 

The largest scale that fits, whatever its fraction: smooth and HD art.



`Integer = 1` 

Whole factors (2×, 3×...), so every canvas pixel covers the same number of window pixels; a window smaller than the
canvas still shrinks it (never crops).



`IntegerUnlessWasteful = 2` 

Whole factors, except when the largest whole factor would fill less than [WastefulThreshold](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_WastefulThreshold)
of what fits (a small phone browser): then fractional. The pixel-art default.

</div>
