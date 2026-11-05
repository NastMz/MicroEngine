# Struct Vertex2D

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

The vertex of the built-in pipeline ([Textured](MicroEngine.Graphics.Devices.BuiltInPipeline.md)), 32 bytes: position in target pixels
relative to the viewport origin (y down), texture coordinate normalised to 0..1 over the bound texture, premultiplied
colour that multiplies the texel. The layout is fixed and identical on every tier: [Vertex2D](MicroEngine.Graphics.Devices.VertexLayout.md#MicroEngine_Graphics_Devices_VertexLayout_Vertex2D).

```csharp
public readonly struct Vertex2D : IEquatable<Vertex2D>
```

#### Implements

[IEquatable<Vertex2D\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_Vertex2D__ctor_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_ColorF_"></a> Vertex2D\(Vector2, Vector2, ColorF\)

The vertex of the built-in pipeline ([Textured](MicroEngine.Graphics.Devices.BuiltInPipeline.md)), 32 bytes: position in target pixels
relative to the viewport origin (y down), texture coordinate normalised to 0..1 over the bound texture, premultiplied
colour that multiplies the texel. The layout is fixed and identical on every tier: [Vertex2D](MicroEngine.Graphics.Devices.VertexLayout.md#MicroEngine_Graphics_Devices_VertexLayout_Vertex2D).

```csharp
public Vertex2D(Vector2 Position, Vector2 TexCoord, ColorF Color)
```

#### Parameters

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Pixels, relative to the viewport origin, y down. Pixel (x, y) has its centre at (x + 0.5, y + 0.5).

`TexCoord` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Normalised texture coordinate; (0, 0) is the top-left corner of texel (0, 0).

`Color` [ColorF](MicroEngine.ColorF.md)

Premultiplied colour multiplied with the sampled texel.

## Fields

### <a id="MicroEngine_Graphics_Devices_Vertex2D_SizeInBytes"></a> SizeInBytes

Size of one vertex in bytes (the stride of every built-in vertex buffer).

```csharp
public const int SizeInBytes = 32
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Graphics_Devices_Vertex2D_Color"></a> Color

Premultiplied colour multiplied with the sampled texel.

```csharp
public ColorF Color { get; init; }
```

#### Property Value

 [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_Graphics_Devices_Vertex2D_Position"></a> Position

Pixels, relative to the viewport origin, y down. Pixel (x, y) has its centre at (x + 0.5, y + 0.5).

```csharp
public Vector2 Position { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_Devices_Vertex2D_TexCoord"></a> TexCoord

Normalised texture coordinate; (0, 0) is the top-left corner of texel (0, 0).

```csharp
public Vector2 TexCoord { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Graphics_Devices_Vertex2D_Deconstruct_System_Numerics_Vector2__System_Numerics_Vector2__MicroEngine_ColorF__"></a> Deconstruct\(out Vector2, out Vector2, out ColorF\)

```csharp
public void Deconstruct(out Vector2 Position, out Vector2 TexCoord, out ColorF Color)
```

#### Parameters

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`TexCoord` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Color` [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_Graphics_Devices_Vertex2D_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_Vertex2D_Equals_MicroEngine_Graphics_Devices_Vertex2D_"></a> Equals\(Vertex2D\)

```csharp
public bool Equals(Vertex2D other)
```

#### Parameters

`other` [Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_Vertex2D_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_Vertex2D_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_Vertex2D_op_Equality_MicroEngine_Graphics_Devices_Vertex2D_MicroEngine_Graphics_Devices_Vertex2D_"></a> operator ==\(Vertex2D, Vertex2D\)

```csharp
public static bool operator ==(Vertex2D left, Vertex2D right)
```

#### Parameters

`left` [Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md)

`right` [Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_Vertex2D_op_Inequality_MicroEngine_Graphics_Devices_Vertex2D_MicroEngine_Graphics_Devices_Vertex2D_"></a> operator \!=\(Vertex2D, Vertex2D\)

```csharp
public static bool operator !=(Vertex2D left, Vertex2D right)
```

#### Parameters

`left` [Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md)

`right` [Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
