# Enum CanvasScaling

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

How the virtual canvas reaches the window (the game settings' <code>Scaling</code>; obsolete: the game's <code>Presentation</code> replaces it). <code>Pixel</code> prefers whole
factors (fractional only when whole ones would waste a fifth of the screen) and samples nearest, so pixels stay square
and even; <code>Smooth</code> fills the largest area of the canvas's shape and samples linearly, for high-resolution art.

```csharp
public enum CanvasScaling
```

## Fields

`Pixel = 0` 

`Smooth = 1`

</div>
