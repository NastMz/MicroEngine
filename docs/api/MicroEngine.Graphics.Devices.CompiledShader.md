# Class CompiledShader

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A shader compiled ahead of time from one HLSL source into every format a tier-1 device may accept (DXIL for
Direct3D 12, SPIR-V for Vulkan, MSL for Metal), with the resource counts reflection found. It is what
<code>microengine shaders</code> writes (one <code>.shader</code> file per HLSL file) and what a game ships: nothing compiles at
run time. [DescriptionFor](MicroEngine.Graphics.Devices.CompiledShader.md#MicroEngine_Graphics_Devices_CompiledShader_DescriptionFor_MicroEngine_Graphics_Devices_GraphicsCapabilities_) picks the format a device should be given; a device without custom shaders
gets the typed [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md), as for any undeclared capability.

```csharp
public sealed class CompiledShader
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CompiledShader](MicroEngine.Graphics.Devices.CompiledShader.md)

## Remarks

The file is UTF-8 JSON: <code>{"format":"microengine-shader","version":1,"stage":"vertex"|"fragment","source":...,
"resources":{"samplers":n,"uniformBuffers":n},"code":{"dxil"|"spirv"|"msl":{"entry":...,"base64":...}}}</code>.
Binding rules are SDL_GPU's (see [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)).

## Constructors

### <a id="MicroEngine_Graphics_Devices_CompiledShader__ctor_MicroEngine_Graphics_Devices_ShaderStage_MicroEngine_Graphics_Devices_ShaderResources_System_Collections_Generic_IReadOnlyDictionary_MicroEngine_Graphics_Devices_ShaderFormat_MicroEngine_Graphics_Devices_ShaderCode__System_String_"></a> CompiledShader\(ShaderStage, ShaderResources, IReadOnlyDictionary<ShaderFormat, ShaderCode\>, string?\)

Creates a compiled shader from its code in one or more formats.

```csharp
public CompiledShader(ShaderStage stage, ShaderResources resources, IReadOnlyDictionary<ShaderFormat, ShaderCode> code, string? source = null)
```

#### Parameters

`stage` [ShaderStage](MicroEngine.Graphics.Devices.ShaderStage.md)

Stage.

`resources` [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)

Reflected resource counts (the same for every format).

`code` [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[ShaderFormat](MicroEngine.Graphics.Devices.ShaderFormat.md), [ShaderCode](MicroEngine.Graphics.Devices.ShaderCode.md)\>

Code by format; each key is exactly one format.

`source` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The source file it was compiled from, for diagnostics.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

No code, a key that is not exactly one format, or an empty entry point or code.

## Fields

### <a id="MicroEngine_Graphics_Devices_CompiledShader_FileExtension"></a> FileExtension

The file extension of a compiled shader ("sprite.frag.hlsl" compiles to "sprite.frag.shader").

```csharp
public const string FileExtension = ".shader"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_Devices_CompiledShader_FormatVersion"></a> FormatVersion

The file format version this assembly reads and writes.

```csharp
public const int FormatVersion = 1
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Graphics_Devices_CompiledShader_Formats"></a> Formats

The formats this shader has code for.

```csharp
public ShaderFormat Formats { get; }
```

#### Property Value

 [ShaderFormat](MicroEngine.Graphics.Devices.ShaderFormat.md)

### <a id="MicroEngine_Graphics_Devices_CompiledShader_Resources"></a> Resources

Reflected resource counts.

```csharp
public ShaderResources Resources { get; }
```

#### Property Value

 [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)

### <a id="MicroEngine_Graphics_Devices_CompiledShader_Source"></a> Source

The source file it was compiled from, or null.

```csharp
public string? Source { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_Devices_CompiledShader_Stage"></a> Stage

Stage.

```csharp
public ShaderStage Stage { get; }
```

#### Property Value

 [ShaderStage](MicroEngine.Graphics.Devices.ShaderStage.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_CompiledShader_Create_MicroEngine_Graphics_Devices_IGraphicsDevice_"></a> Create\(IGraphicsDevice\)

Creates the shader on <code class="paramref">device</code> in the format [DescriptionFor](MicroEngine.Graphics.Devices.CompiledShader.md#MicroEngine_Graphics_Devices_CompiledShader_DescriptionFor_MicroEngine_Graphics_Devices_GraphicsCapabilities_) picks.

```csharp
public Shader Create(IGraphicsDevice device)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

#### Returns

 [Shader](MicroEngine.Graphics.Devices.Shader.md)

#### Exceptions

 [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md)

See [DescriptionFor](MicroEngine.Graphics.Devices.CompiledShader.md#MicroEngine_Graphics_Devices_CompiledShader_DescriptionFor_MicroEngine_Graphics_Devices_GraphicsCapabilities_).

### <a id="MicroEngine_Graphics_Devices_CompiledShader_DescriptionFor_MicroEngine_Graphics_Devices_GraphicsCapabilities_"></a> DescriptionFor\(GraphicsCapabilities\)

The description to give a device with <code class="paramref">capabilities</code>: the format its API runs natively
(DXIL on Direct3D 12, MSL on Metal, SPIR-V on Vulkan), otherwise the first format both sides have.

```csharp
public ShaderDescription DescriptionFor(GraphicsCapabilities capabilities)
```

#### Parameters

`capabilities` [GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)

#### Returns

 [ShaderDescription](MicroEngine.Graphics.Devices.ShaderDescription.md)

#### Exceptions

 [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md)

The device declares no custom shaders ([Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md)), or none of the formats this shader was compiled for.

### <a id="MicroEngine_Graphics_Devices_CompiledShader_Parse_System_ReadOnlySpan_System_Byte__"></a> Parse\(ReadOnlySpan<byte\>\)

Reads a compiled shader file.

```csharp
public static CompiledShader Parse(ReadOnlySpan<byte> utf8Json)
```

#### Parameters

`utf8Json` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

#### Returns

 [CompiledShader](MicroEngine.Graphics.Devices.CompiledShader.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The data is not a compiled shader of a version this assembly reads.

### <a id="MicroEngine_Graphics_Devices_CompiledShader_ToBytes"></a> ToBytes\(\)

The file form of this shader (UTF-8 JSON, formats in a fixed order, so the same compilation gives the same bytes).

```csharp
public byte[] ToBytes()
```

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

### <a id="MicroEngine_Graphics_Devices_CompiledShader_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_Devices_CompiledShader_TryGetCode_MicroEngine_Graphics_Devices_ShaderFormat_MicroEngine_Graphics_Devices_ShaderCode__"></a> TryGetCode\(ShaderFormat, out ShaderCode\)

The code in <code class="paramref">format</code>, if compiled for it.

```csharp
public bool TryGetCode(ShaderFormat format, out ShaderCode code)
```

#### Parameters

`format` [ShaderFormat](MicroEngine.Graphics.Devices.ShaderFormat.md)

`code` [ShaderCode](MicroEngine.Graphics.Devices.ShaderCode.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
