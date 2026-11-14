# Class CanvasPresenter

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

Draws a game's canvas into the window as its [Presentation](MicroEngine.Graphics.Presentation.md) says, prescale included
([Prescale](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Prescale)): the window cleared to the letterbox colour, the canvas into its frame, and the
UI view over both. With a prescale the canvas is first enlarged by a whole factor with nearest sampling into a target
this presenter owns, and that enlargement is what the presentation's upscale sampler samples. Platforms built on a
graphics device present their frames with it.

```csharp
public sealed class CanvasPresenter : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CanvasPresenter](MicroEngine.Graphics.CanvasPresenter.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Constructors

### <a id="MicroEngine_Graphics_CanvasPresenter__ctor_MicroEngine_Graphics_Devices_IGraphicsDevice_"></a> CanvasPresenter\(IGraphicsDevice\)

Creates a presenter drawing with <code class="paramref">device</code>.

```csharp
public CanvasPresenter(IGraphicsDevice device)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

## Properties

### <a id="MicroEngine_Graphics_CanvasPresenter_PrescaledSize"></a> PrescaledSize

The size of the enlarged canvas the last frame was sampled from, or null when it needed none.

```csharp
public (int Width, int Height)? PrescaledSize { get; }
```

#### Property Value

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)?

## Methods

### <a id="MicroEngine_Graphics_CanvasPresenter_Dispose"></a> Dispose\(\)

Releases the enlarged canvas and the batcher.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_CanvasPresenter_Present_MicroEngine_Graphics_Presentation_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_RectF_MicroEngine_Color_"></a> Present\(Presentation, RenderTarget, RenderTarget?, RectF, Color\)

Draws <code class="paramref">canvas</code> into the window's target: the window cleared to <code class="paramref">letterbox</code>
(opaque), the canvas copied (its alpha ignored) into <code class="paramref">frame</code>, then <code class="paramref">ui</code> (a
premultiplied target of the window's size, or null) over everything. Returns false, drawing nothing, when the
window has no target this frame (minimised); the caller then skips presenting.

```csharp
public bool Present(Presentation presentation, RenderTarget canvas, RenderTarget? ui, RectF frame, Color letterbox)
```

#### Parameters

`presentation` [Presentation](MicroEngine.Graphics.Presentation.md)

The game's presentation: its upscale sampler and prescale.

`canvas` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

The canvas the game drew.

`ui` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)?

The UI view of the frame, in window pixels, or null when the frame drew none.

`frame` [RectF](MicroEngine.RectF.md)

Where the canvas lands in the window, in window pixels ([Fit](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_Fit_MicroEngine_RectF_System_Int32_System_Int32_System_Boolean_)).

`letterbox` [Color](MicroEngine.Color.md)

The colour of the window around the frame.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
