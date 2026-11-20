# Class RenderTarget2D

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

A picture off screen that a renderer draws into ([PushTarget](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushTarget_MicroEngine_Graphics_RenderTarget2D_)) and that is then drawn like any
texture: [Texture](MicroEngine.Graphics.RenderTarget2D.md#MicroEngine_Graphics_RenderTarget2D_Texture) is a texture of the drawing API, so it works with every <code>Draw</code> overload, as the
source of a [Sprite](MicroEngine.Graphics.Sprite.md) and as a material's texture. Create one with
[CreateRenderTarget](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_CreateRenderTarget_System_Int32_System_Int32_MicroEngine_Graphics_TextureFilter_), or through a game's assets
(<code>Assets.RenderTarget</code>), which release it with their scope. A target keeps its pixels between frames until it
is drawn into again; they are premultiplied, like the canvas.

```csharp
public sealed class RenderTarget2D : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RenderTarget2D](MicroEngine.Graphics.RenderTarget2D.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Constructors

### <a id="MicroEngine_Graphics_RenderTarget2D__ctor_MicroEngine_Graphics_Texture_"></a> RenderTarget2D\(Texture\)

Wraps the texture a graphics device made to be drawn into. Backends call it from
[CreateRenderTarget](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_CreateRenderTarget_System_Int32_System_Int32_MicroEngine_Graphics_TextureFilter_); a game asks its graphics device.

```csharp
public RenderTarget2D(Texture texture)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Texture.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">texture</code> cannot be drawn into.

## Properties

### <a id="MicroEngine_Graphics_RenderTarget2D_Bounds"></a> Bounds

The whole target as a source rectangle.

```csharp
public RectF Bounds { get; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Graphics_RenderTarget2D_Height"></a> Height

Height in pixels.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_RenderTarget2D_IsDisposed"></a> IsDisposed

Whether the target was disposed; a disposed target cannot be drawn into or drawn.

```csharp
public bool IsDisposed { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_RenderTarget2D_SizeInBytes"></a> SizeInBytes

Memory the target takes on the device, in bytes.

```csharp
public long SizeInBytes { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_RenderTarget2D_Texture"></a> Texture

The target's picture as a texture. It is the same resource: disposing either disposes both.

```csharp
public Texture Texture { get; }
```

#### Property Value

 [Texture](MicroEngine.Graphics.Texture.md)

### <a id="MicroEngine_Graphics_RenderTarget2D_Width"></a> Width

Width in pixels.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_RenderTarget2D_Dispose"></a> Dispose\(\)

Releases the target and its texture. Safe to call more than once.

```csharp
public void Dispose()
```

</div>
