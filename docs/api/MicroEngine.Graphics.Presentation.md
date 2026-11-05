# Class Presentation

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [Presentation](../manual/rendering.md#presentation-one-choice-per-field)  

How the game's canvas reaches the window, one independent choice per field: how its scale is rounded
([Rounding](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Rounding)), what happens to the window's other shape ([Aspect](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Aspect)), how it is sampled when it is
scaled ([Upscale](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Upscale)), the sampler textures get when they choose none ([DefaultSampler](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_DefaultSampler)), the size
factor of captures ([CaptureScale](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_CaptureScale)), whether desktop windows open at whole factors
([WholeFactorWindows](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_WholeFactorWindows)) and whether 2D cameras snap to whole pixels by default
([CameraPixelSnap](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_CameraPixelSnap)). [PixelArt](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_PixelArt) and [HighResolution2D](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_HighResolution2D) are presets: start from
one and change any field with <code>with</code>.

```csharp
public sealed record Presentation : IEquatable<Presentation>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Presentation](MicroEngine.Graphics.Presentation.md)

#### Implements

[IEquatable<Presentation\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Presentation__ctor"></a> Presentation\(\)

```csharp
public Presentation()
```

## Properties

### <a id="MicroEngine_Graphics_Presentation_Aspect"></a> Aspect

Keep the canvas's shape (letterbox) or stretch it to the window.

```csharp
public AspectMode Aspect { get; init; }
```

#### Property Value

 [AspectMode](MicroEngine.Graphics.AspectMode.md)

### <a id="MicroEngine_Graphics_Presentation_CameraPixelSnap"></a> CameraPixelSnap

Whether 2D cameras created for this presentation round their translation to whole canvas pixels
(<code>Camera2D.PixelSnap</code>): crisp pixel art ([PixelArt](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_PixelArt)), smooth motion for high-resolution art
([HighResolution2D](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_HighResolution2D)). Each camera can still change it.

```csharp
public bool CameraPixelSnap { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Presentation_CaptureScale"></a> CaptureScale

Size factor of the captures a run writes (screenshots, scripted captures): 1 is the canvas's own size.

```csharp
public int CaptureScale { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is less than 1.

### <a id="MicroEngine_Graphics_Presentation_DefaultSampler"></a> DefaultSampler

The sampler of textures, atlases and font pages created without a filter of their own: the game's explicit default,
written down instead of implied by another option.

```csharp
public SamplerDescription DefaultSampler { get; init; }
```

#### Property Value

 [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

### <a id="MicroEngine_Graphics_Presentation_HighResolution2D"></a> HighResolution2D

High-resolution 2D: the largest size of the canvas's shape that fits, letterboxed, linear sampling for the canvas
and for textures, captures at the canvas's size, cameras moving smoothly (no snap).

```csharp
public static Presentation HighResolution2D { get; }
```

#### Property Value

 [Presentation](MicroEngine.Graphics.Presentation.md)

### <a id="MicroEngine_Graphics_Presentation_PixelArt"></a> PixelArt

Pixel art: whole factors unless they waste more than a fifth of the window, letterboxed, nearest sampling for the
canvas and for textures, captures three times the canvas, windows opened at whole factors, cameras snapped to whole
pixels.

```csharp
public static Presentation PixelArt { get; }
```

#### Property Value

 [Presentation](MicroEngine.Graphics.Presentation.md)

### <a id="MicroEngine_Graphics_Presentation_Rounding"></a> Rounding

How the scale is rounded.

```csharp
public ScaleRounding Rounding { get; init; }
```

#### Property Value

 [ScaleRounding](MicroEngine.Graphics.ScaleRounding.md)

### <a id="MicroEngine_Graphics_Presentation_UiScale"></a> UiScale

Window pixels per UI unit of the UI view, drawn over the world at the window's resolution. Null (the default)
follows the surface's pixel density ([PixelDensity](MicroEngine.Platform.ISurface.md#MicroEngine_Platform_ISurface_PixelDensity)): 1 on an ordinary screen, 2 on
a screen with twice the pixels per logical unit, so the interface keeps its physical size; the headless platform's
density is 1. A value overrides the density: 1 draws one UI unit per window pixel everywhere, 2 makes the UI twice
as large. Platforms resolve it with [UiScaleFor](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_UiScaleFor_System_Single_).

```csharp
public float? UiScale { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is not null and not a positive finite number.

### <a id="MicroEngine_Graphics_Presentation_Upscale"></a> Upscale

How the canvas is sampled when it is drawn to the window at another size.

```csharp
public SamplerDescription Upscale { get; init; }
```

#### Property Value

 [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

### <a id="MicroEngine_Graphics_Presentation_WastefulThreshold"></a> WastefulThreshold

With [IntegerUnlessWasteful](MicroEngine.Graphics.ScaleRounding.md): the share of the fitting size a whole factor must fill (0.8
by default, so a whole factor is kept when it fills at least 80 % of what would fit).

```csharp
public float WastefulThreshold { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is not within 0..1.

### <a id="MicroEngine_Graphics_Presentation_WholeFactorWindows"></a> WholeFactorWindows

Whether desktop windows open at a whole factor of the canvas when the requested size does not fit the display.

```csharp
public bool WholeFactorWindows { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Graphics_Presentation__Clone__"></a> <Clone\>$\(\)

```csharp
public Presentation <Clone>$()
```

#### Returns

 [Presentation](MicroEngine.Graphics.Presentation.md)

### <a id="MicroEngine_Graphics_Presentation_CanvasSize_MicroEngine_RectF_System_Int32_System_Int32_"></a> CanvasSize\(RectF, int, int\)

The canvas size for <code class="paramref">area</code>: the configured size, except with [Expand](MicroEngine.Graphics.AspectMode.md),
where the side the window has room for grows (at the scale [Rounding](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Rounding) chooses) until the scaled canvas
fills the area. Never smaller than the configured size.

```csharp
public (int Width, int Height) CanvasSize(RectF area, int canvasWidth, int canvasHeight)
```

#### Parameters

`area` [RectF](MicroEngine.RectF.md)

`canvasWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`canvasHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The configured size is not positive.

### <a id="MicroEngine_Graphics_Presentation_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Presentation_Equals_MicroEngine_Graphics_Presentation_"></a> Equals\(Presentation?\)

```csharp
public bool Equals(Presentation? other)
```

#### Parameters

`other` [Presentation](MicroEngine.Graphics.Presentation.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Presentation_Fit_MicroEngine_RectF_System_Int32_System_Int32_System_Boolean_"></a> Fit\(RectF, int, int, bool\)

Where the canvas lands in <code class="paramref">area</code> (the window, or its safe area on a phone), in window pixels: the
largest scale that fits, rounded by [Rounding](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Rounding), centred; with <code class="paramref">portrait</code> it sits at the
top of the area instead (on-screen controls get the space below). With [Stretch](MicroEngine.Graphics.AspectMode.md) it covers
the whole area.

```csharp
public RectF Fit(RectF area, int canvasWidth, int canvasHeight, bool portrait = false)
```

#### Parameters

`area` [RectF](MicroEngine.RectF.md)

The area available, in window pixels.

`canvasWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The canvas's width in pixels.

`canvasHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The canvas's height in pixels.

`portrait` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the screen is portrait (taller than wide by more than a tenth).

#### Returns

 [RectF](MicroEngine.RectF.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The canvas size is not positive.

### <a id="MicroEngine_Graphics_Presentation_Fit_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_"></a> Fit\(Vector2, RectF, int, int\)

Where the canvas lands in a window of <code class="paramref">window</code> pixels whose usable part is
<code class="paramref">safeArea</code> (an empty safe area means the whole window): [Fit](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Fit_MicroEngine_RectF_System_Int32_System_Int32_System_Boolean_)
in the safe area, at its top when the window is portrait (taller than wide by more than a tenth). Every backend
places its frame with this method, so they agree.

```csharp
public RectF Fit(Vector2 window, RectF safeArea, int canvasWidth, int canvasHeight)
```

#### Parameters

`window` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`safeArea` [RectF](MicroEngine.RectF.md)

`canvasWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`canvasHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Graphics_Presentation_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Presentation_ToCanvas_System_Numerics_Vector2_MicroEngine_RectF_System_Int32_System_Int32_"></a> ToCanvas\(Vector2, RectF, int, int\)

A window position in canvas pixels, for a canvas drawn at <code class="paramref">frame</code>.

```csharp
public static Vector2 ToCanvas(Vector2 window, RectF frame, int canvasWidth, int canvasHeight)
```

#### Parameters

`window` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`frame` [RectF](MicroEngine.RectF.md)

`canvasWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`canvasHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_Presentation_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_Presentation_UiScaleFor_System_Single_"></a> UiScaleFor\(float\)

The UI scale on a surface of <code class="paramref">pixelDensity</code>: [UiScale](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_UiScale) when it is set, else the density
(1 when the density is not a positive finite number).

```csharp
public float UiScaleFor(float pixelDensity)
```

#### Parameters

`pixelDensity` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Operators

### <a id="MicroEngine_Graphics_Presentation_op_Equality_MicroEngine_Graphics_Presentation_MicroEngine_Graphics_Presentation_"></a> operator ==\(Presentation?, Presentation?\)

```csharp
public static bool operator ==(Presentation? left, Presentation? right)
```

#### Parameters

`left` [Presentation](MicroEngine.Graphics.Presentation.md)?

`right` [Presentation](MicroEngine.Graphics.Presentation.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Presentation_op_Inequality_MicroEngine_Graphics_Presentation_MicroEngine_Graphics_Presentation_"></a> operator \!=\(Presentation?, Presentation?\)

```csharp
public static bool operator !=(Presentation? left, Presentation? right)
```

#### Parameters

`left` [Presentation](MicroEngine.Graphics.Presentation.md)?

`right` [Presentation](MicroEngine.Graphics.Presentation.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
