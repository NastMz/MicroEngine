# Class LightMap

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

Darkness with point lights for caves, dungeons and night scenes. Each frame the game clears the
map, adds lights in world space and draws it over the finished scene:

<ol><li>a coarse brightness grid ([CellSize](MicroEngine.Graphics.LightMap.md#MicroEngine_Graphics_LightMap_CellSize) pixels, quantised to [Levels](MicroEngine.Graphics.LightMap.md#MicroEngine_Graphics_LightMap_Levels) bands)
is multiplied over the screen, toward [Shade](MicroEngine.Graphics.LightMap.md#MicroEngine_Graphics_LightMap_Shade) where it is dark;</li><li>lights with a glow colour add a soft radial halo on top.</li></ol>

Rows of equal cells merge into single rectangles, so a full screen costs a few hundred quads.
With [Darkness](MicroEngine.Graphics.LightMap.md#MicroEngine_Graphics_LightMap_Darkness) 1 and lights of brightness 0 it becomes a flat colour grade plus glows.

```csharp
public sealed class LightMap : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[LightMap](MicroEngine.Graphics.LightMap.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Constructors

### <a id="MicroEngine_Graphics_LightMap__ctor_MicroEngine_Graphics_IGraphicsDevice_MicroEngine_Graphics_PixelImage_"></a> LightMap\(IGraphicsDevice?, PixelImage?\)

```csharp
public LightMap(IGraphicsDevice? device, PixelImage? glowImage = null)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)?

Creates the halo texture; pass null to draw darkness only (e.g. headless tests).

`glowImage` [PixelImage](MicroEngine.Graphics.PixelImage.md)?

Optional square white halo to match a game's style (e.g. banded pixel glows);
            defaults to [CreateGlowImage](MicroEngine.Graphics.LightMap.md#MicroEngine_Graphics_LightMap_CreateGlowImage_System_Int32_). It is scaled to each light's glow radius.

## Properties

### <a id="MicroEngine_Graphics_LightMap_CellSize"></a> CellSize

Size of a brightness cell in pixels (4 by default); larger cells are cheaper and blockier.

```csharp
public int CellSize { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_LightMap_Darkness"></a> Darkness

Ambient darkness: 0 = no effect, 1 = fully [Shade](MicroEngine.Graphics.LightMap.md#MicroEngine_Graphics_LightMap_Shade) where unlit.

```csharp
public float Darkness { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_LightMap_GlowIntensity"></a> GlowIntensity

Multiplier applied to every glow's alpha (e.g. fade halos with the darkness).

```csharp
public float GlowIntensity { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_LightMap_Levels"></a> Levels

Number of brightness bands (12 by default); fewer bands give a more stylised look.

```csharp
public int Levels { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_LightMap_LightCount"></a> LightCount

Number of lights added since the last [Clear](MicroEngine.Graphics.LightMap.md#MicroEngine_Graphics_LightMap_Clear).

```csharp
public int LightCount { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_LightMap_Shade"></a> Shade

The colour full darkness multiplies the scene toward.

```csharp
public Color Shade { get; set; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

## Methods

### <a id="MicroEngine_Graphics_LightMap_Add_System_Numerics_Vector2_System_Single_System_Single_System_Nullable_MicroEngine_Color__System_Nullable_System_Single__"></a> Add\(Vector2, float, float, Color?, float?\)

Adds a light. <code class="paramref">brightness</code> lifts darkness inside <code class="paramref">radius</code> with a smooth
falloff; <code class="paramref">glow</code> (alpha respected) adds an additive halo of <code class="paramref">glowRadius</code>
(default 90% of the radius). A light may only glow (brightness 0) or only brighten (no glow).

```csharp
public void Add(Vector2 worldPosition, float radius, float brightness, Color? glow = null, float? glowRadius = null)
```

#### Parameters

`worldPosition` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`brightness` [float](https://learn.microsoft.com/dotnet/api/system.single)

`glow` [Color](MicroEngine.Color.md)?

`glowRadius` [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Graphics_LightMap_BrightnessAt_System_Numerics_Vector2_"></a> BrightnessAt\(Vector2\)

Brightness 0..1 at a world point, before banding (useful for tests and stealth/AI rules).

```csharp
public float BrightnessAt(Vector2 world)
```

#### Parameters

`world` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_LightMap_Clear"></a> Clear\(\)

Removes every light; call it at the start of each frame before adding lights.

```csharp
public void Clear()
```

### <a id="MicroEngine_Graphics_LightMap_CreateGlowImage_System_Int32_"></a> CreateGlowImage\(int\)

A white radial halo with a smooth (squared smoothstep) falloff.

```csharp
public static PixelImage CreateGlowImage(int size)
```

#### Parameters

`size` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_LightMap_Dispose"></a> Dispose\(\)

Releases the halo texture.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_LightMap_Draw_MicroEngine_Graphics_IRenderer_System_Numerics_Vector2_MicroEngine_RectF_"></a> Draw\(IRenderer, Vector2, RectF\)

Draws over <code class="paramref">screenArea</code>; <code class="paramref">cameraTopLeft</code> is the world point at screen (0,0).

```csharp
public void Draw(IRenderer r, Vector2 cameraTopLeft, RectF screenArea)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`cameraTopLeft` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`screenArea` [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Graphics_LightMap_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_Camera2D_"></a> Draw\(IRenderer, Camera2D\)

Convenience overload using a camera's rendered position (including shake).

```csharp
public void Draw(IRenderer r, Camera2D camera)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`camera` [Camera2D](MicroEngine.Graphics.Camera2D.md)
