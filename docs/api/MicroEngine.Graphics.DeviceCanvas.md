# Class DeviceCanvas

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [The 2D batcher and the camera](../manual/rendering.md#the-2d-batcher-and-the-camera)  

A game's canvas on a graphics device, as backends built on the device contract use it: the canvas render target, the
textures ([Graphics](MicroEngine.Graphics.DeviceCanvas.md#MicroEngine_Graphics_DeviceCanvas_Graphics)) and the drawing API ([Renderer](MicroEngine.Graphics.DeviceCanvas.md#MicroEngine_Graphics_DeviceCanvas_Renderer)) over the 2D batcher, the composition of
the canvas into the window as the presentation says, and captures read back from the canvas.

```csharp
public sealed class DeviceCanvas : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DeviceCanvas](MicroEngine.Graphics.DeviceCanvas.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Constructors

### <a id="MicroEngine_Graphics_DeviceCanvas__ctor_MicroEngine_Graphics_Devices_IGraphicsDevice_System_Int32_System_Int32_"></a> DeviceCanvas\(IGraphicsDevice, int, int\)

Creates a canvas of <code class="paramref">width</code> × <code class="paramref">height</code> pixels on <code class="paramref">device</code>.

```csharp
public DeviceCanvas(IGraphicsDevice device, int width, int height)
```

#### Parameters

`device` IGraphicsDevice

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Graphics_DeviceCanvas_Device"></a> Device

The device everything is drawn with.

```csharp
public IGraphicsDevice Device { get; }
```

#### Property Value

 IGraphicsDevice

### <a id="MicroEngine_Graphics_DeviceCanvas_Graphics"></a> Graphics

The textures of the drawing API, on [Device](MicroEngine.Graphics.DeviceCanvas.md#MicroEngine_Graphics_DeviceCanvas_Device).

```csharp
public DeviceGraphics Graphics { get; }
```

#### Property Value

 [DeviceGraphics](MicroEngine.Graphics.DeviceGraphics.md)

### <a id="MicroEngine_Graphics_DeviceCanvas_Renderer"></a> Renderer

The drawing API over the batcher, drawing into [Target](MicroEngine.Graphics.DeviceCanvas.md#MicroEngine_Graphics_DeviceCanvas_Target).

```csharp
public DeviceRenderer Renderer { get; }
```

#### Property Value

 [DeviceRenderer](MicroEngine.Graphics.DeviceRenderer.md)

### <a id="MicroEngine_Graphics_DeviceCanvas_Target"></a> Target

The canvas: the render target the game draws into.

```csharp
public RenderTarget Target { get; }
```

#### Property Value

 RenderTarget

## Methods

### <a id="MicroEngine_Graphics_DeviceCanvas_BeginFrame"></a> BeginFrame\(\)

Starts drawing a frame into the canvas.

```csharp
public IRenderer BeginFrame()
```

#### Returns

 [IRenderer](MicroEngine.Graphics.IRenderer.md)

### <a id="MicroEngine_Graphics_DeviceCanvas_Capture"></a> Capture\(\)

The canvas as a capture stores it: every pixel opaque (alpha 255), the colour channels as stored.

```csharp
public PixelImage Capture()
```

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_DeviceCanvas_Compose_MicroEngine_Graphics_Presentation_MicroEngine_RectF_MicroEngine_Color_"></a> Compose\(Presentation, RectF, Color\)

Draws the canvas into the window's target: the window cleared to <code class="paramref">letterbox</code> (opaque), then the
canvas copied (its alpha ignored, so a translucent canvas never darkens) into <code class="paramref">frame</code> with the
presentation's upscale sampler. Returns false, drawing nothing, when the window has no target this frame
(minimised); the caller then skips presenting.

```csharp
public bool Compose(Presentation presentation, RectF frame, Color letterbox)
```

#### Parameters

`presentation` Presentation

`frame` RectF

`letterbox` Color

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_DeviceCanvas_Dispose"></a> Dispose\(\)

Releases the canvas, the batchers and every texture made with [Graphics](MicroEngine.Graphics.DeviceCanvas.md#MicroEngine_Graphics_DeviceCanvas_Graphics).

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_DeviceCanvas_EndFrame"></a> EndFrame\(\)

Submits the frame drawn into the canvas.

```csharp
public void EndFrame()
```

### <a id="MicroEngine_Graphics_DeviceCanvas_SaveCapture_System_String_System_Int32_"></a> SaveCapture\(string, int\)

Writes [Capture](MicroEngine.Graphics.DeviceCanvas.md#MicroEngine_Graphics_DeviceCanvas_Capture) as a PNG, each canvas pixel repeated <code class="paramref">scale</code> times per axis.

```csharp
public void SaveCapture(string path, int scale)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`scale` [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>
