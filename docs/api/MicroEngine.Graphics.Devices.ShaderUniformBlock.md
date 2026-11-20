# Class ShaderUniformBlock

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A uniform block of a shader (an HLSL <code>cbuffer</code>): its slot, its size and its named values.

```csharp
public sealed class ShaderUniformBlock
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ShaderUniformBlock](MicroEngine.Graphics.Devices.ShaderUniformBlock.md)

## Constructors

### <a id="MicroEngine_Graphics_Devices_ShaderUniformBlock__ctor_System_String_System_Int32_System_Int32_System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_Devices_ShaderUniform__"></a> ShaderUniformBlock\(string, int, int, IReadOnlyList<ShaderUniform\>\)

Creates the description of a block.

```csharp
public ShaderUniformBlock(string name, int slot, int size, IReadOnlyList<ShaderUniform> uniforms)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`slot` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`size` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`uniforms` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[ShaderUniform](MicroEngine.Graphics.Devices.ShaderUniform.md)\>

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A negative slot, a size that is not positive, or a value that leaves the block.

## Properties

### <a id="MicroEngine_Graphics_Devices_ShaderUniformBlock_Name"></a> Name

The block's name in the shader source.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_Devices_ShaderUniformBlock_Size"></a> Size

The size of the block's data in bytes.

```csharp
public int Size { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_ShaderUniformBlock_Slot"></a> Slot

The uniform slot the block is bound to (0..n-1).

```csharp
public int Slot { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_ShaderUniformBlock_Uniforms"></a> Uniforms

The block's values, in declaration order.

```csharp
public IReadOnlyList<ShaderUniform> Uniforms { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[ShaderUniform](MicroEngine.Graphics.Devices.ShaderUniform.md)\>

</div>
