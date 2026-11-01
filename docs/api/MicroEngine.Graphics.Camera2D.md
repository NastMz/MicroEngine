# Class Camera2D

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

A 2D camera whose [Position](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_Position) is the world point at the top-left of the view.
Supports smooth following with a dead zone, world bounds and trauma-based shake.

```csharp
public sealed class Camera2D
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Camera2D](MicroEngine.Graphics.Camera2D.md)

## Examples

```csharp
var camera = new Camera2D(Game.Width, Game.Height) { Bounds = new RectF(0, 0, 1280, 360), DeadZone = new Vector2(24, 16) };

// Update:
camera.Follow(player.Center, dt);
camera.Update(dt);

// Draw:
renderer.PushCamera(camera);
DrawWorld(renderer);
renderer.PopCamera();
```

## Remarks

The camera has no zoom or rotation: one world pixel is one virtual pixel. Draw the world between
[PushCamera](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushCamera_MicroEngine_Graphics_Camera2D_) and [PopCamera](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PopCamera), and the interface after the pop.

## Constructors

### <a id="MicroEngine_Graphics_Camera2D__ctor_System_Single_System_Single_"></a> Camera2D\(float, float\)

Creates a camera at the world origin.

```csharp
public Camera2D(float viewWidth, float viewHeight)
```

#### Parameters

`viewWidth` [float](https://learn.microsoft.com/dotnet/api/system.single)

Width of the view in pixels.

`viewHeight` [float](https://learn.microsoft.com/dotnet/api/system.single)

Height of the view in pixels.

## Properties

### <a id="MicroEngine_Graphics_Camera2D_Bounds"></a> Bounds

Optional world limits; the view is kept inside when the world is larger than the view.

```csharp
public RectF? Bounds { get; set; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)?

### <a id="MicroEngine_Graphics_Camera2D_Center"></a> Center

The world point at the centre of the view.

```csharp
public Vector2 Center { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_Camera2D_DeadZone"></a> DeadZone

Half-size of the dead zone around the view centre in which the target can move freely.

```csharp
public Vector2 DeadZone { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_Camera2D_FollowSharpness"></a> FollowSharpness

Per-second follow sharpness; 0 snaps immediately.

```csharp
public float FollowSharpness { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Camera2D_MaxShake"></a> MaxShake

Maximum shake offset in pixels at full trauma.

```csharp
public float MaxShake { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Camera2D_PixelSnap"></a> PixelSnap

Rounds the render offset to whole pixels for crisp pixel art.

```csharp
public bool PixelSnap { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Camera2D_Position"></a> Position

The world point at the top-left of the view, in pixels. Setting it directly does not apply [Bounds](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_Bounds); call [Clamp](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_Clamp) for that.

```csharp
public Vector2 Position { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_Camera2D_RenderOffset"></a> RenderOffset

The translation the renderer should apply (negative position plus shake), snapped if requested.

```csharp
public Vector2 RenderOffset { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_Camera2D_ShakeOffset"></a> ShakeOffset

Shake offset applied on top of [Position](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_Position) when rendering.

```csharp
public Vector2 ShakeOffset { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_Camera2D_View"></a> View

The world rectangle the camera shows, without shake.

```csharp
public RectF View { get; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Graphics_Camera2D_ViewHeight"></a> ViewHeight

Height of the view in pixels, usually the virtual canvas height.

```csharp
public float ViewHeight { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Camera2D_ViewWidth"></a> ViewWidth

Width of the view in pixels, usually the virtual canvas width.

```csharp
public float ViewWidth { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Graphics_Camera2D_CenterOn_System_Numerics_Vector2_"></a> CenterOn\(Vector2\)

Moves the view so <code class="paramref">target</code> is at its centre at once, then applies [Bounds](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_Bounds).

```csharp
public void CenterOn(Vector2 target)
```

#### Parameters

`target` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_Camera2D_Clamp"></a> Clamp\(\)

Keeps the view inside [Bounds](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_Bounds); on an axis where the bounds are smaller than the view, centres the bounds instead. Does nothing without bounds.

```csharp
public void Clamp()
```

### <a id="MicroEngine_Graphics_Camera2D_Follow_System_Numerics_Vector2_System_Single_"></a> Follow\(Vector2, float\)

Moves toward keeping <code class="paramref">target</code> inside the dead zone.

```csharp
public void Follow(Vector2 target, float dt)
```

#### Parameters

`target` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Camera2D_IsVisible_MicroEngine_RectF_System_Single_"></a> IsVisible\(RectF, float\)

Whether <code class="paramref">world</code> overlaps the view, grown by <code class="paramref">margin</code> plus [MaxShake](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_MaxShake) pixels. Use it to skip drawing off-screen objects.

```csharp
public bool IsVisible(RectF world, float margin = 0)
```

#### Parameters

`world` [RectF](MicroEngine.RectF.md)

`margin` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Camera2D_ScreenToWorld_System_Numerics_Vector2_"></a> ScreenToWorld\(Vector2\)

Converts a virtual-canvas point (for example a pointer position) to world pixels, including shake.

```csharp
public Vector2 ScreenToWorld(Vector2 screen)
```

#### Parameters

`screen` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_Camera2D_Shake_System_Single_"></a> Shake\(float\)

Adds shake trauma (0..1). Shake strength is trauma squared, decaying over time.

```csharp
public void Shake(float trauma)
```

#### Parameters

`trauma` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Camera2D_Update_System_Single_"></a> Update\(float\)

Decays shake trauma (fully in about 0.6 seconds from 1) and picks a new [ShakeOffset](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_ShakeOffset). Call once per update.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

Elapsed time in seconds.

### <a id="MicroEngine_Graphics_Camera2D_WorldToScreen_System_Numerics_Vector2_"></a> WorldToScreen\(Vector2\)

Converts a world point to virtual-canvas pixels, including shake.

```csharp
public Vector2 WorldToScreen(Vector2 world)
```

#### Parameters

`world` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

</div>
