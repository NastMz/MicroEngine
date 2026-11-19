# Class ShaderReflection

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

The names a compiled shader's source gives its parameters, as reflection found them when it was compiled: the
uniform blocks with each value's place, and the textures. It lets a material's parameters be set by name and
checked, the same for every format the shader was compiled to.

```csharp
public sealed class ShaderReflection
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ShaderReflection](MicroEngine.Graphics.Devices.ShaderReflection.md)

## Constructors

### <a id="MicroEngine_Graphics_Devices_ShaderReflection__ctor_System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_Devices_ShaderUniformBlock__System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_Devices_ShaderTextureSlot__"></a> ShaderReflection\(IReadOnlyList<ShaderUniformBlock\>, IReadOnlyList<ShaderTextureSlot\>\)

Creates the description of a shader's parameters.

```csharp
public ShaderReflection(IReadOnlyList<ShaderUniformBlock> blocks, IReadOnlyList<ShaderTextureSlot> textures)
```

#### Parameters

`blocks` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[ShaderUniformBlock](MicroEngine.Graphics.Devices.ShaderUniformBlock.md)\>

`textures` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[ShaderTextureSlot](MicroEngine.Graphics.Devices.ShaderTextureSlot.md)\>

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Two blocks or two textures in one slot, or one name used twice.

## Properties

### <a id="MicroEngine_Graphics_Devices_ShaderReflection_Blocks"></a> Blocks

The uniform blocks, by slot.

```csharp
public IReadOnlyList<ShaderUniformBlock> Blocks { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[ShaderUniformBlock](MicroEngine.Graphics.Devices.ShaderUniformBlock.md)\>

### <a id="MicroEngine_Graphics_Devices_ShaderReflection_Names"></a> Names

Every parameter name, values first: what a message lists when a name is not one of them.

```csharp
public IEnumerable<string> Names { get; }
```

#### Property Value

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Graphics_Devices_ShaderReflection_Textures"></a> Textures

The textures, by slot.

```csharp
public IReadOnlyList<ShaderTextureSlot> Textures { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[ShaderTextureSlot](MicroEngine.Graphics.Devices.ShaderTextureSlot.md)\>

## Methods

### <a id="MicroEngine_Graphics_Devices_ShaderReflection_TryGetUniform_System_String_MicroEngine_Graphics_Devices_ShaderUniformBlock__MicroEngine_Graphics_Devices_ShaderUniform__"></a> TryGetUniform\(string, out ShaderUniformBlock, out ShaderUniform\)

The value named <code class="paramref">name</code> and the block that holds it.

```csharp
public bool TryGetUniform(string name, out ShaderUniformBlock block, out ShaderUniform uniform)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`block` [ShaderUniformBlock](MicroEngine.Graphics.Devices.ShaderUniformBlock.md)

`uniform` [ShaderUniform](MicroEngine.Graphics.Devices.ShaderUniform.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
