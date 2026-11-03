# Class VertexLayout

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Interleaved vertex layout of one vertex buffer (slot 0).

```csharp
public sealed record VertexLayout : IEquatable<VertexLayout>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)

#### Implements

[IEquatable<VertexLayout\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_VertexLayout__ctor_System_Int32_System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_Devices_VertexAttribute__"></a> VertexLayout\(int, IReadOnlyList<VertexAttribute\>\)

Interleaved vertex layout of one vertex buffer (slot 0).

```csharp
public VertexLayout(int Stride, IReadOnlyList<VertexAttribute> Attributes)
```

#### Parameters

`Stride` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Bytes per vertex.

`Attributes` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[VertexAttribute](MicroEngine.Graphics.Devices.VertexAttribute.md)\>

Attributes, at most [MaxVertexAttributes](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxVertexAttributes).

## Fields

### <a id="MicroEngine_Graphics_Devices_VertexLayout_Vertex2D"></a> Vertex2D

The layout of [Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md): position Float2 @0, texcoord Float2 @8, colour Float4 @16, stride 32.

```csharp
public static readonly VertexLayout Vertex2D
```

#### Field Value

 [VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)

## Properties

### <a id="MicroEngine_Graphics_Devices_VertexLayout_Attributes"></a> Attributes

Attributes, at most [MaxVertexAttributes](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxVertexAttributes).

```csharp
public IReadOnlyList<VertexAttribute> Attributes { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[VertexAttribute](MicroEngine.Graphics.Devices.VertexAttribute.md)\>

### <a id="MicroEngine_Graphics_Devices_VertexLayout_Stride"></a> Stride

Bytes per vertex.

```csharp
public int Stride { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Devices_VertexLayout__Clone__"></a> <Clone\>$\(\)

```csharp
public VertexLayout <Clone>$()
```

#### Returns

 [VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)

### <a id="MicroEngine_Graphics_Devices_VertexLayout_Deconstruct_System_Int32__System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_Devices_VertexAttribute___"></a> Deconstruct\(out int, out IReadOnlyList<VertexAttribute\>\)

```csharp
public void Deconstruct(out int Stride, out IReadOnlyList<VertexAttribute> Attributes)
```

#### Parameters

`Stride` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Attributes` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[VertexAttribute](MicroEngine.Graphics.Devices.VertexAttribute.md)\>

### <a id="MicroEngine_Graphics_Devices_VertexLayout_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_VertexLayout_Equals_MicroEngine_Graphics_Devices_VertexLayout_"></a> Equals\(VertexLayout?\)

```csharp
public bool Equals(VertexLayout? other)
```

#### Parameters

`other` [VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_VertexLayout_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_VertexLayout_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_VertexLayout_op_Equality_MicroEngine_Graphics_Devices_VertexLayout_MicroEngine_Graphics_Devices_VertexLayout_"></a> operator ==\(VertexLayout?, VertexLayout?\)

```csharp
public static bool operator ==(VertexLayout? left, VertexLayout? right)
```

#### Parameters

`left` [VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)?

`right` [VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_VertexLayout_op_Inequality_MicroEngine_Graphics_Devices_VertexLayout_MicroEngine_Graphics_Devices_VertexLayout_"></a> operator \!=\(VertexLayout?, VertexLayout?\)

```csharp
public static bool operator !=(VertexLayout? left, VertexLayout? right)
```

#### Parameters

`left` [VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)?

`right` [VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
