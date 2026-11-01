# Class Texture

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

A GPU (or headless) texture owned by the active graphics device.

```csharp
public abstract class Texture : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Texture](MicroEngine.Graphics.Texture.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Constructors

### <a id="MicroEngine_Graphics_Texture__ctor_System_Int32_System_Int32_"></a> Texture\(int, int\)

Records the size of a texture created by a graphics device.

```csharp
protected Texture(int width, int height)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Graphics_Texture_Bounds"></a> Bounds

The whole texture as a source rectangle: (0, 0, [Width](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Width), [Height](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Height)).

```csharp
public RectF Bounds { get; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Graphics_Texture_Height"></a> Height

Height in pixels.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Texture_IsDisposed"></a> IsDisposed

Whether [Dispose](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Dispose) was called. Drawing a disposed texture fails.

```csharp
public bool IsDisposed { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Texture_Width"></a> Width

Width in pixels.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Texture_Dispose"></a> Dispose\(\)

Releases the device texture. Safe to call more than once. Whoever created the texture (or the atlas or font that owns it) disposes it.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_Texture_Release"></a> Release\(\)

Frees the device resource; called once by [Dispose](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Dispose).

```csharp
protected abstract void Release()
```
