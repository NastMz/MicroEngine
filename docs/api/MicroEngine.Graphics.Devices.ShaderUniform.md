# Class ShaderUniform

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

One named value of a uniform block (a field of an HLSL <code>cbuffer</code>).

```csharp
public sealed record ShaderUniform : IEquatable<ShaderUniform>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ShaderUniform](MicroEngine.Graphics.Devices.ShaderUniform.md)

#### Implements

[IEquatable<ShaderUniform\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_ShaderUniform__ctor_System_String_System_Int32_System_Int32_"></a> ShaderUniform\(string, int, int\)

One named value of a uniform block (a field of an HLSL <code>cbuffer</code>).

```csharp
public ShaderUniform(string Name, int Offset, int Size)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The field's name in the shader source.

`Offset` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Where it starts in the block's data, in bytes.

`Size` [int](https://learn.microsoft.com/dotnet/api/system.int32)

How many bytes it takes (a <code>float</code> 4, a <code>float2</code> 8, a <code>float3</code> 12, a <code>float4</code> 16, a <code>float4x4</code> 64).

## Properties

### <a id="MicroEngine_Graphics_Devices_ShaderUniform_Name"></a> Name

The field's name in the shader source.

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_Devices_ShaderUniform_Offset"></a> Offset

Where it starts in the block's data, in bytes.

```csharp
public int Offset { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_ShaderUniform_Size"></a> Size

How many bytes it takes (a <code>float</code> 4, a <code>float2</code> 8, a <code>float3</code> 12, a <code>float4</code> 16, a <code>float4x4</code> 64).

```csharp
public int Size { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Devices_ShaderUniform__Clone__"></a> <Clone\>$\(\)

```csharp
public ShaderUniform <Clone>$()
```

#### Returns

 [ShaderUniform](MicroEngine.Graphics.Devices.ShaderUniform.md)

### <a id="MicroEngine_Graphics_Devices_ShaderUniform_Deconstruct_System_String__System_Int32__System_Int32__"></a> Deconstruct\(out string, out int, out int\)

```csharp
public void Deconstruct(out string Name, out int Offset, out int Size)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Offset` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Size` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_ShaderUniform_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_ShaderUniform_Equals_MicroEngine_Graphics_Devices_ShaderUniform_"></a> Equals\(ShaderUniform?\)

```csharp
public bool Equals(ShaderUniform? other)
```

#### Parameters

`other` [ShaderUniform](MicroEngine.Graphics.Devices.ShaderUniform.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_ShaderUniform_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_ShaderUniform_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_ShaderUniform_op_Equality_MicroEngine_Graphics_Devices_ShaderUniform_MicroEngine_Graphics_Devices_ShaderUniform_"></a> operator ==\(ShaderUniform?, ShaderUniform?\)

```csharp
public static bool operator ==(ShaderUniform? left, ShaderUniform? right)
```

#### Parameters

`left` [ShaderUniform](MicroEngine.Graphics.Devices.ShaderUniform.md)?

`right` [ShaderUniform](MicroEngine.Graphics.Devices.ShaderUniform.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_ShaderUniform_op_Inequality_MicroEngine_Graphics_Devices_ShaderUniform_MicroEngine_Graphics_Devices_ShaderUniform_"></a> operator \!=\(ShaderUniform?, ShaderUniform?\)

```csharp
public static bool operator !=(ShaderUniform? left, ShaderUniform? right)
```

#### Parameters

`left` [ShaderUniform](MicroEngine.Graphics.Devices.ShaderUniform.md)?

`right` [ShaderUniform](MicroEngine.Graphics.Devices.ShaderUniform.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
