# Class Material2D

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

A fragment shader of the game's own with its parameters, that draws go through instead of the built-in pipeline:
push it with a state ([PushState](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushState_MicroEngine_Graphics_DrawState__), [Material](MicroEngine.Graphics.DrawState.md#MicroEngine_Graphics_DrawState_Material)) and every draw made until
the pop (sprites, shapes, text, in any camera, with the state's blend and sampler) goes through the shader. A game
gets one from its assets (<code>Assets.Material("shaders/water.frag.shader")</code>) or from its graphics device
([CreateMaterial](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_CreateMaterial_MicroEngine_Graphics_Devices_CompiledShader_MicroEngine_Graphics_MaterialFallback_)); the engine supplies the vertex stage
([SpriteVertexShader](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_SpriteVertexShader)), so a material needs only the fragment shader, compiled ahead of time by
<code>microengine shaders</code>.

```csharp
public sealed class Material2D : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Material2D](MicroEngine.Graphics.Material2D.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Remarks

<p>
Parameters are set by the names they have in the shader's source ([Set``1](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_Set__1_System_String___0__),
[SetTexture](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_SetTexture_System_String_MicroEngine_Graphics_Texture_System_Nullable_MicroEngine_Graphics_Devices_SamplerDescription__)) and checked: a name the shader does not have, or a
value of another size than the shader declares, is an `ArgumentException` that lists the shader's
parameters. A draw keeps the parameters the material had when the draw was made, so one frame can draw with the same
material and different parameters. Values never set are zero; a texture never set is an error at the draw.
</p>
<p>
Shaders run on a device that declares [Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md) (<code>sdl-gpu</code>). On any other
([IsSupported](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_IsSupported) false) the material is created all the same, checks its parameters the same way, and
its draws are made without the shader, with its [Fallback](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_Fallback). The game's drawing code is the same.
</p>
<p>
What the shaders receive: vertices in the layout of [Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md) (position in target pixels relative to
the viewport at location 0, texture coordinate at 1, premultiplied colour at 2); in vertex uniform slot 0, when the
vertex shader declares one, the 4 × 4 matrix that takes those pixels to clip space; in fragment texture slot 0 the
draw's texture with the state's sampler, and in the slots from 1 the material's textures; in the fragment uniform
slots the material's values. The material creates one pipeline per blend state and target format it is drawn
with and releases them when it is disposed. Dispose a material between frames, not in the frame that drew with
it: the frame's draws reach the device when it ends.
</p>

## Constructors

### <a id="MicroEngine_Graphics_Material2D__ctor_MicroEngine_Graphics_Devices_Shader_MicroEngine_Graphics_Devices_Shader_"></a> Material2D\(Shader, Shader\)

Creates a material from a vertex and a fragment shader of one device. The shaders stay the caller's.

```csharp
public Material2D(Shader vertex, Shader fragment)
```

#### Parameters

`vertex` [Shader](MicroEngine.Graphics.Devices.Shader.md)

A vertex shader taking [Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md) vertices, with at most one uniform slot (the pixel-to-clip matrix).

`fragment` [Shader](MicroEngine.Graphics.Devices.Shader.md)

A fragment shader.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A shader of the wrong stage, shaders of two devices, or a vertex shader with more than one uniform slot.

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

A disposed shader.

### <a id="MicroEngine_Graphics_Material2D__ctor_MicroEngine_Graphics_Devices_Shader_MicroEngine_Graphics_Devices_Shader_MicroEngine_Graphics_Devices_ShaderReflection_"></a> Material2D\(Shader, Shader, ShaderReflection?\)

Creates a material from a vertex and a fragment shader of one device, with the names of the fragment shader's
parameters ([Reflection](MicroEngine.Graphics.Devices.CompiledShader.md#MicroEngine_Graphics_Devices_CompiledShader_Reflection)) so they can be set by name. The shaders stay the caller's.

```csharp
public Material2D(Shader vertex, Shader fragment, ShaderReflection? reflection)
```

#### Parameters

`vertex` [Shader](MicroEngine.Graphics.Devices.Shader.md)

A vertex shader taking [Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md) vertices, with at most one uniform slot (the pixel-to-clip matrix).

`fragment` [Shader](MicroEngine.Graphics.Devices.Shader.md)

A fragment shader.

`reflection` [ShaderReflection](MicroEngine.Graphics.Devices.ShaderReflection.md)?

The fragment shader's parameter names, or null to set parameters by slot only.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A shader of the wrong stage, shaders of two devices, a vertex shader with more than one uniform slot, or names for slots the fragment shader does not have.

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

A disposed shader.

## Properties

### <a id="MicroEngine_Graphics_Material2D_Device"></a> Device

The device the material draws on, or null when it is not supported.

```csharp
public IGraphicsDevice? Device { get; }
```

#### Property Value

 [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)?

### <a id="MicroEngine_Graphics_Material2D_Fallback"></a> Fallback

What the material's draws become where its shader cannot run ([IsSupported](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_IsSupported) false); plain draws by default.

```csharp
public MaterialFallback Fallback { get; set; }
```

#### Property Value

 [MaterialFallback](MicroEngine.Graphics.MaterialFallback.md)

### <a id="MicroEngine_Graphics_Material2D_Fragment"></a> Fragment

The fragment shader, or null when the material is not supported.

```csharp
public Shader? Fragment { get; }
```

#### Property Value

 [Shader](MicroEngine.Graphics.Devices.Shader.md)?

### <a id="MicroEngine_Graphics_Material2D_IsDisposed"></a> IsDisposed

True once disposed; a disposed material cannot be drawn with.

```csharp
public bool IsDisposed { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Material2D_IsSupported"></a> IsSupported

Whether the graphics device runs the material's shader. False on a device without
[Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md), where the material's draws are made with its [Fallback](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_Fallback).

```csharp
public bool IsSupported { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Material2D_Reflection"></a> Reflection

The names of the fragment shader's parameters, or null when they are not known (a version 1 <code>.shader</code> file, or shaders given without them).

```csharp
public ShaderReflection? Reflection { get; }
```

#### Property Value

 [ShaderReflection](MicroEngine.Graphics.Devices.ShaderReflection.md)?

### <a id="MicroEngine_Graphics_Material2D_SpriteVertexShader"></a> SpriteVertexShader

The vertex stage every 2D material shares, compiled for every format: it passes the batcher's vertices on
(texture coordinate in <code>TEXCOORD0</code>, premultiplied colour in <code>TEXCOORD1</code>) and takes their pixel
positions to clip space. [Create](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_Create_MicroEngine_Graphics_Devices_IGraphicsDevice_MicroEngine_Graphics_Devices_CompiledShader_MicroEngine_Graphics_MaterialFallback_) uses it; a game that builds a material from shaders of its own on
the device can use it as that material's vertex shader (<code>SpriteVertexShader.Create(device)</code>).

```csharp
public static CompiledShader SpriteVertexShader { get; }
```

#### Property Value

 [CompiledShader](MicroEngine.Graphics.Devices.CompiledShader.md)

### <a id="MicroEngine_Graphics_Material2D_Vertex"></a> Vertex

The vertex shader, or null when the material is not supported.

```csharp
public Shader? Vertex { get; }
```

#### Property Value

 [Shader](MicroEngine.Graphics.Devices.Shader.md)?

## Methods

### <a id="MicroEngine_Graphics_Material2D_Create_MicroEngine_Graphics_Devices_IGraphicsDevice_MicroEngine_Graphics_Devices_CompiledShader_MicroEngine_Graphics_MaterialFallback_"></a> Create\(IGraphicsDevice?, CompiledShader, MaterialFallback\)

Creates a material from a compiled fragment shader, with the engine's vertex stage
([SpriteVertexShader](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_SpriteVertexShader)). On a device that runs shaders the material owns the two shaders it creates
there. With no device, or one without [Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md), the material is not supported
([IsSupported](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_IsSupported)): it keeps and checks its parameters and draws with <code class="paramref">fallback</code>.

```csharp
public static Material2D Create(IGraphicsDevice? device, CompiledShader fragment, MaterialFallback fallback = default)
```

#### Parameters

`device` IGraphicsDevice?

The device to draw on, or null for a renderer that has none (the headless platform).

`fragment` [CompiledShader](MicroEngine.Graphics.Devices.CompiledShader.md)

A fragment shader compiled by <code>microengine shaders</code>.

`fallback` [MaterialFallback](MicroEngine.Graphics.MaterialFallback.md)

What the draws become where the shader cannot run.

#### Returns

 [Material2D](MicroEngine.Graphics.Material2D.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">fragment</code> is not a fragment shader.

 [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md)

The device runs shaders, but none of the formats this one was compiled to.

### <a id="MicroEngine_Graphics_Material2D_Dispose"></a> Dispose\(\)

Releases the pipelines the material created, and the shaders it created itself ([Create](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_Create_MicroEngine_Graphics_Devices_IGraphicsDevice_MicroEngine_Graphics_Devices_CompiledShader_MicroEngine_Graphics_MaterialFallback_)). Shaders given to a constructor stay the caller's.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_Material2D_Set__1_System_String___0__"></a> Set<T\>\(string, in T\)

Sets the value the shader's source names <code class="paramref">name</code> (a field of a <code>cbuffer</code>): a
`Single`, a `Vector2`, `Vector3` or
`Vector4`, a [ColorF](MicroEngine.ColorF.md), a `Matrix4x4`, or any
value whose bytes are what the shader declares. Draws made afterwards use it; draws already made keep what they had.

```csharp
public void Set<T>(string name, in T value) where T : unmanaged
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`value` T

#### Type Parameters

`T` 

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The shader has no value of that name, or the value's size is not the size the shader declares; the message lists the shader's parameters.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The shader's parameter names are not known ([Reflection](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_Reflection) is null).

### <a id="MicroEngine_Graphics_Material2D_SetTexture_System_String_MicroEngine_Graphics_Texture_System_Nullable_MicroEngine_Graphics_Devices_SamplerDescription__"></a> SetTexture\(string, Texture, SamplerDescription?\)

Gives the shader the texture its source names <code class="paramref">name</code>, sampled with <code class="paramref">sampler</code>
(null: the texture's own filter, clamped). Any texture of the drawing API works, a render target's included.
The first texture a shader declares (slot 0) is the texture of each draw and is not set here.

```csharp
public void SetTexture(string name, Texture texture, SamplerDescription? sampler = null)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`texture` [Texture](MicroEngine.Graphics.Texture.md)

`sampler` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)?

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The shader has no texture of that name, the name is the draw's own texture, or the texture belongs to another graphics device.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The shader's parameter names are not known ([Reflection](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_Reflection) is null).

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

A disposed texture.

### <a id="MicroEngine_Graphics_Material2D_SetTexture_System_Int32_MicroEngine_Graphics_Devices_Texture_System_Nullable_MicroEngine_Graphics_Devices_SamplerDescription__"></a> SetTexture\(int, Texture, SamplerDescription?\)

Gives the fragment shader a device texture in a slot from 1 on (slot 0 is the texture of each draw), sampled with
<code class="paramref">sampler</code> (null: nearest, clamped). Prefer
[SetTexture](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_SetTexture_System_String_MicroEngine_Graphics_Texture_System_Nullable_MicroEngine_Graphics_Devices_SamplerDescription__), by name. Draws made afterwards use it.

```csharp
public void SetTexture(int slot, Texture texture, SamplerDescription? sampler = null)
```

#### Parameters

`slot` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`texture` [Texture](MicroEngine.Graphics.Devices.Texture.md)

`sampler` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)?

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

Slot 0, or a slot the fragment shader does not declare.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A texture of another device.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The material is not supported: it has no device.

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

A disposed texture.

### <a id="MicroEngine_Graphics_Material2D_SetUniforms__1___0__System_Int32_"></a> SetUniforms<T\>\(in T, int\)

Sets the data of a whole fragment uniform slot: the bytes of <code class="paramref">data</code> as laid out in memory, which
must match the shader's block (a sequential struct of floats and vectors with the block's padding). Prefer
[Set``1](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_Set__1_System_String___0__), which is checked; this is for shaders whose names are not known. Draws made
afterwards use it; draws already made keep what they had.

```csharp
public void SetUniforms<T>(in T data, int slot = 0) where T : unmanaged
```

#### Parameters

`data` T

`slot` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Type Parameters

`T` 

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The fragment shader declares no such slot, the data is empty or larger than [MaxUniformBytes](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxUniformBytes), or its size is not the size of the block when that is known.

### <a id="MicroEngine_Graphics_Material2D_SetUniforms_System_ReadOnlySpan_System_Byte__System_Int32_"></a> SetUniforms\(ReadOnlySpan<byte\>, int\)

Sets the data of a whole fragment uniform slot from bytes (see [SetUniforms``1](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_SetUniforms__1___0__System_Int32_)).

```csharp
public void SetUniforms(ReadOnlySpan<byte> data, int slot = 0)
```

#### Parameters

`data` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

`slot` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The fragment shader declares no such slot, the data is empty or larger than [MaxUniformBytes](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxUniformBytes), or its size is not the size of the block when that is known.

</div>
