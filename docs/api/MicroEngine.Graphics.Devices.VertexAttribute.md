# Struct VertexAttribute

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

One vertex attribute.

```csharp
public readonly struct VertexAttribute : IEquatable<VertexAttribute>
```

#### Implements

[IEquatable<VertexAttribute\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_VertexAttribute__ctor_System_Int32_MicroEngine_Graphics_Devices_VertexFormat_System_Int32_"></a> VertexAttribute\(int, VertexFormat, int\)

One vertex attribute.

```csharp
public VertexAttribute(int Location, VertexFormat Format, int Offset)
```

#### Parameters

`Location` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Shader input location (HLSL <code>TEXCOORD&lt;location&gt;</code>).

`Format` [VertexFormat](MicroEngine.Graphics.Devices.VertexFormat.md)

Element format.

`Offset` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Byte offset in the vertex.

## Properties

### <a id="MicroEngine_Graphics_Devices_VertexAttribute_Format"></a> Format

Element format.

```csharp
public VertexFormat Format { get; init; }
```

#### Property Value

 [VertexFormat](MicroEngine.Graphics.Devices.VertexFormat.md)

### <a id="MicroEngine_Graphics_Devices_VertexAttribute_Location"></a> Location

Shader input location (HLSL <code>TEXCOORD&lt;location&gt;</code>).

```csharp
public int Location { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_VertexAttribute_Offset"></a> Offset

Byte offset in the vertex.

```csharp
public int Offset { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Devices_VertexAttribute_Deconstruct_System_Int32__MicroEngine_Graphics_Devices_VertexFormat__System_Int32__"></a> Deconstruct\(out int, out VertexFormat, out int\)

```csharp
public void Deconstruct(out int Location, out VertexFormat Format, out int Offset)
```

#### Parameters

`Location` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Format` [VertexFormat](MicroEngine.Graphics.Devices.VertexFormat.md)

`Offset` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_VertexAttribute_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_VertexAttribute_Equals_MicroEngine_Graphics_Devices_VertexAttribute_"></a> Equals\(VertexAttribute\)

```csharp
public bool Equals(VertexAttribute other)
```

#### Parameters

`other` [VertexAttribute](MicroEngine.Graphics.Devices.VertexAttribute.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_VertexAttribute_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_VertexAttribute_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_VertexAttribute_op_Equality_MicroEngine_Graphics_Devices_VertexAttribute_MicroEngine_Graphics_Devices_VertexAttribute_"></a> operator ==\(VertexAttribute, VertexAttribute\)

```csharp
public static bool operator ==(VertexAttribute left, VertexAttribute right)
```

#### Parameters

`left` [VertexAttribute](MicroEngine.Graphics.Devices.VertexAttribute.md)

`right` [VertexAttribute](MicroEngine.Graphics.Devices.VertexAttribute.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_VertexAttribute_op_Inequality_MicroEngine_Graphics_Devices_VertexAttribute_MicroEngine_Graphics_Devices_VertexAttribute_"></a> operator \!=\(VertexAttribute, VertexAttribute\)

```csharp
public static bool operator !=(VertexAttribute left, VertexAttribute right)
```

#### Parameters

`left` [VertexAttribute](MicroEngine.Graphics.Devices.VertexAttribute.md)

`right` [VertexAttribute](MicroEngine.Graphics.Devices.VertexAttribute.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
