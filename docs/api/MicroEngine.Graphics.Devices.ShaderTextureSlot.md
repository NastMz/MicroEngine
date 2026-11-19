# Class ShaderTextureSlot

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A texture a shader samples: its name in the source and its slot.

```csharp
public sealed record ShaderTextureSlot : IEquatable<ShaderTextureSlot>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ShaderTextureSlot](MicroEngine.Graphics.Devices.ShaderTextureSlot.md)

#### Implements

[IEquatable<ShaderTextureSlot\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_ShaderTextureSlot__ctor_System_String_System_Int32_"></a> ShaderTextureSlot\(string, int\)

A texture a shader samples: its name in the source and its slot.

```csharp
public ShaderTextureSlot(string Name, int Slot)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The texture's name in the shader source.

`Slot` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The sampler slot it is bound to (0..n-1).

## Properties

### <a id="MicroEngine_Graphics_Devices_ShaderTextureSlot_Name"></a> Name

The texture's name in the shader source.

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_Devices_ShaderTextureSlot_Slot"></a> Slot

The sampler slot it is bound to (0..n-1).

```csharp
public int Slot { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Devices_ShaderTextureSlot__Clone__"></a> <Clone\>$\(\)

```csharp
public ShaderTextureSlot <Clone>$()
```

#### Returns

 [ShaderTextureSlot](MicroEngine.Graphics.Devices.ShaderTextureSlot.md)

### <a id="MicroEngine_Graphics_Devices_ShaderTextureSlot_Deconstruct_System_String__System_Int32__"></a> Deconstruct\(out string, out int\)

```csharp
public void Deconstruct(out string Name, out int Slot)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Slot` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_ShaderTextureSlot_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_ShaderTextureSlot_Equals_MicroEngine_Graphics_Devices_ShaderTextureSlot_"></a> Equals\(ShaderTextureSlot?\)

```csharp
public bool Equals(ShaderTextureSlot? other)
```

#### Parameters

`other` [ShaderTextureSlot](MicroEngine.Graphics.Devices.ShaderTextureSlot.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_ShaderTextureSlot_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_ShaderTextureSlot_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_ShaderTextureSlot_op_Equality_MicroEngine_Graphics_Devices_ShaderTextureSlot_MicroEngine_Graphics_Devices_ShaderTextureSlot_"></a> operator ==\(ShaderTextureSlot?, ShaderTextureSlot?\)

```csharp
public static bool operator ==(ShaderTextureSlot? left, ShaderTextureSlot? right)
```

#### Parameters

`left` [ShaderTextureSlot](MicroEngine.Graphics.Devices.ShaderTextureSlot.md)?

`right` [ShaderTextureSlot](MicroEngine.Graphics.Devices.ShaderTextureSlot.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_ShaderTextureSlot_op_Inequality_MicroEngine_Graphics_Devices_ShaderTextureSlot_MicroEngine_Graphics_Devices_ShaderTextureSlot_"></a> operator \!=\(ShaderTextureSlot?, ShaderTextureSlot?\)

```csharp
public static bool operator !=(ShaderTextureSlot? left, ShaderTextureSlot? right)
```

#### Parameters

`left` [ShaderTextureSlot](MicroEngine.Graphics.Devices.ShaderTextureSlot.md)?

`right` [ShaderTextureSlot](MicroEngine.Graphics.Devices.ShaderTextureSlot.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
