# Enum CanvasScaling

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

How the virtual canvas reaches the window ([Scaling](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Scaling)). <code>Pixel</code> prefers whole
factors (fractional only when whole ones would waste a fifth of the screen) and samples nearest, so pixels stay square
and even; <code>Smooth</code> fills the largest area of the canvas's shape and samples linearly, for high-resolution art.

```csharp
public enum CanvasScaling
```

## Fields

`Pixel = 0` 

`Smooth = 1`

</div>
