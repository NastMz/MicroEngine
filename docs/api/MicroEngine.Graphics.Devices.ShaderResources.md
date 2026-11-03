# Struct ShaderResources

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Resource counts of a shader, produced by the offline tool's reflection and validated at creation. Bindings follow
the SDL_GPU register-space rules (vertex uniforms space1; fragment samplers space2, fragment uniforms space3).

```csharp
public readonly struct ShaderResources : IEquatable<ShaderResources>
```

#### Implements

[IEquatable<ShaderResources\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_ShaderResources__ctor_System_Int32_System_Int32_System_Int32_System_Int32_"></a> ShaderResources\(int, int, int, int\)

Resource counts of a shader, produced by the offline tool's reflection and validated at creation. Bindings follow
the SDL_GPU register-space rules (vertex uniforms space1; fragment samplers space2, fragment uniforms space3).

```csharp
public ShaderResources(int Samplers = 0, int UniformBuffers = 0, int StorageTextures = 0, int StorageBuffers = 0)
```

#### Parameters

`Samplers` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Texture-sampler pairs, slots 0..n-1.

`UniformBuffers` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Uniform slots, 0..n-1.

`StorageTextures` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Storage textures (reserved; must be 0 in this contract version).

`StorageBuffers` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Storage buffers (reserved; must be 0 in this contract version).

## Properties

### <a id="MicroEngine_Graphics_Devices_ShaderResources_Samplers"></a> Samplers

Texture-sampler pairs, slots 0..n-1.

```csharp
public int Samplers { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_ShaderResources_StorageBuffers"></a> StorageBuffers

Storage buffers (reserved; must be 0 in this contract version).

```csharp
public int StorageBuffers { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_ShaderResources_StorageTextures"></a> StorageTextures

Storage textures (reserved; must be 0 in this contract version).

```csharp
public int StorageTextures { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_ShaderResources_UniformBuffers"></a> UniformBuffers

Uniform slots, 0..n-1.

```csharp
public int UniformBuffers { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Devices_ShaderResources_Deconstruct_System_Int32__System_Int32__System_Int32__System_Int32__"></a> Deconstruct\(out int, out int, out int, out int\)

```csharp
public void Deconstruct(out int Samplers, out int UniformBuffers, out int StorageTextures, out int StorageBuffers)
```

#### Parameters

`Samplers` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`UniformBuffers` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`StorageTextures` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`StorageBuffers` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_ShaderResources_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_ShaderResources_Equals_MicroEngine_Graphics_Devices_ShaderResources_"></a> Equals\(ShaderResources\)

```csharp
public bool Equals(ShaderResources other)
```

#### Parameters

`other` [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_ShaderResources_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_ShaderResources_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_ShaderResources_op_Equality_MicroEngine_Graphics_Devices_ShaderResources_MicroEngine_Graphics_Devices_ShaderResources_"></a> operator ==\(ShaderResources, ShaderResources\)

```csharp
public static bool operator ==(ShaderResources left, ShaderResources right)
```

#### Parameters

`left` [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)

`right` [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_ShaderResources_op_Inequality_MicroEngine_Graphics_Devices_ShaderResources_MicroEngine_Graphics_Devices_ShaderResources_"></a> operator \!=\(ShaderResources, ShaderResources\)

```csharp
public static bool operator !=(ShaderResources left, ShaderResources right)
```

#### Parameters

`left` [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)

`right` [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
