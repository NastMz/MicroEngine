# Class ParallaxLayer

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

A background image that scrolls slower (or faster) than the camera and repeats horizontally.
Draw it in screen space (no camera pushed). [Factor](MicroEngine.Graphics.ParallaxLayer.md#MicroEngine_Graphics_ParallaxLayer_Factor) 0 is fixed to the screen, 1 moves
with the world. [Y](MicroEngine.Graphics.ParallaxLayer.md#MicroEngine_Graphics_ParallaxLayer_Y) is the screen row of the image's top edge; the optional
[VerticalFactor](MicroEngine.Graphics.ParallaxLayer.md#MicroEngine_Graphics_ParallaxLayer_VerticalFactor) lets tall worlds shift it as the camera rises or falls.

```csharp
public sealed class ParallaxLayer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ParallaxLayer](MicroEngine.Graphics.ParallaxLayer.md)

## Constructors

### <a id="MicroEngine_Graphics_ParallaxLayer__ctor_MicroEngine_Graphics_Texture_"></a> ParallaxLayer\(Texture\)

A background image that scrolls slower (or faster) than the camera and repeats horizontally.
Draw it in screen space (no camera pushed). [Factor](MicroEngine.Graphics.ParallaxLayer.md#MicroEngine_Graphics_ParallaxLayer_Factor) 0 is fixed to the screen, 1 moves
with the world. [Y](MicroEngine.Graphics.ParallaxLayer.md#MicroEngine_Graphics_ParallaxLayer_Y) is the screen row of the image's top edge; the optional
[VerticalFactor](MicroEngine.Graphics.ParallaxLayer.md#MicroEngine_Graphics_ParallaxLayer_VerticalFactor) lets tall worlds shift it as the camera rises or falls.

```csharp
public ParallaxLayer(Texture texture)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Texture.md)

## Properties

### <a id="MicroEngine_Graphics_ParallaxLayer_Drift"></a> Drift

Extra horizontal offset in screen pixels per second (drifting clouds, traffic).

```csharp
public float Drift { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ParallaxLayer_Factor"></a> Factor

Horizontal scroll relative to the camera: 0 stays fixed, 1 moves with the world (0.5 by default).

```csharp
public float Factor { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ParallaxLayer_RepeatX"></a> RepeatX

Whether the image repeats to cover the screen width (true by default).

```csharp
public bool RepeatX { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_ParallaxLayer_Texture"></a> Texture

The image; the layer does not dispose it.

```csharp
public Texture Texture { get; }
```

#### Property Value

 [Texture](MicroEngine.Graphics.Texture.md)

### <a id="MicroEngine_Graphics_ParallaxLayer_Tint"></a> Tint

Colour multiplier (white by default).

```csharp
public Color Tint { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_ParallaxLayer_VerticalFactor"></a> VerticalFactor

Vertical scroll relative to the camera (0 by default: the layer stays at [Y](MicroEngine.Graphics.ParallaxLayer.md#MicroEngine_Graphics_ParallaxLayer_Y)).

```csharp
public float VerticalFactor { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ParallaxLayer_Y"></a> Y

Screen row of the image's top edge when the camera is at Y = 0.

```csharp
public float Y { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Graphics_ParallaxLayer_Draw_MicroEngine_Graphics_IRenderer_System_Numerics_Vector2_System_Single_"></a> Draw\(IRenderer, Vector2, float\)

Draws the layer in screen space.

```csharp
public void Draw(IRenderer r, Vector2 cameraPosition, float time = 0)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer, with no camera pushed.

`cameraPosition` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The camera's [Position](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_Position).

`time` [float](https://learn.microsoft.com/dotnet/api/system.single)

Seconds used by [Drift](MicroEngine.Graphics.ParallaxLayer.md#MicroEngine_Graphics_ParallaxLayer_Drift), for example [Time](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Time).
