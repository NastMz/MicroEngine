# Class Material2D

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

A pair of shaders of the game's own, with their parameters, that the 2D batcher draws with instead of the built-in
pipeline: set it on [Material](MicroEngine.Graphics.RenderState.md#MicroEngine_Graphics_RenderState_Material) (or [Material](MicroEngine.Graphics.BatchSettings.md#MicroEngine_Graphics_BatchSettings_Material)) and every draw
recorded while it is set (sprites, geometry, in any transform, with the state's blend, sampler and scissor) goes
through its shaders. It needs a device with custom shaders ([Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md)); the shaders
are created from files compiled ahead of time ([CompiledShader](MicroEngine.Graphics.Devices.CompiledShader.md)), which refuse any other device with
[GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md).

```csharp
public sealed class Material2D : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Material2D](MicroEngine.Graphics.Material2D.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Remarks

What the shaders receive:

<ul><li>Vertices in the layout of [Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md): position in target pixels relative to the viewport
(location 0), texture coordinate (location 1), premultiplied colour (location 2).</li><li>Vertex uniform slot 0, when the vertex shader declares one: the 4 × 4 matrix that takes those pixels to clip
space (a `Matrix4x4` as it is in memory; <code>mul(matrix, float4(position, 0, 1))</code> in HLSL).</li><li>Fragment texture slot 0: the draw's texture with the state's sampler. Slots from 1: the textures given to
[SetTexture](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_SetTexture_System_Int32_MicroEngine_Graphics_Devices_Texture_System_Nullable_MicroEngine_Graphics_Devices_SamplerDescription__).</li><li>Fragment uniform slots: the data given to [SetUniforms``1](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_SetUniforms__1___0__System_Int32_).</li></ul>

A draw keeps the parameters the material had when the draw was recorded, so one batch can draw with the same material
and different parameters. The material creates one pipeline per blend state and target format it is drawn with and
releases them when it is disposed; it does not own the shaders.

## Constructors

### <a id="MicroEngine_Graphics_Material2D__ctor_MicroEngine_Graphics_Devices_Shader_MicroEngine_Graphics_Devices_Shader_"></a> Material2D\(Shader, Shader\)

Creates a material from a vertex and a fragment shader of one device.

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

## Properties

### <a id="MicroEngine_Graphics_Material2D_Device"></a> Device

The device the material draws on.

```csharp
public IGraphicsDevice Device { get; }
```

#### Property Value

 [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

### <a id="MicroEngine_Graphics_Material2D_Fragment"></a> Fragment

The fragment shader.

```csharp
public Shader Fragment { get; }
```

#### Property Value

 [Shader](MicroEngine.Graphics.Devices.Shader.md)

### <a id="MicroEngine_Graphics_Material2D_IsDisposed"></a> IsDisposed

True once disposed; a disposed material cannot be drawn with.

```csharp
public bool IsDisposed { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Material2D_Vertex"></a> Vertex

The vertex shader.

```csharp
public Shader Vertex { get; }
```

#### Property Value

 [Shader](MicroEngine.Graphics.Devices.Shader.md)

## Methods

### <a id="MicroEngine_Graphics_Material2D_Dispose"></a> Dispose\(\)

Releases the pipelines the material created. The shaders stay the caller's.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_Material2D_SetTexture_System_Int32_MicroEngine_Graphics_Devices_Texture_System_Nullable_MicroEngine_Graphics_Devices_SamplerDescription__"></a> SetTexture\(int, Texture, SamplerDescription?\)

Gives the fragment shader a texture in a slot from 1 on (slot 0 is the texture of each draw), sampled with
<code class="paramref">sampler</code> (null: nearest, clamped). Draws recorded afterwards use it.

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

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

A disposed texture.

### <a id="MicroEngine_Graphics_Material2D_SetUniforms__1___0__System_Int32_"></a> SetUniforms<T\>\(in T, int\)

Sets the data of a fragment uniform slot: the bytes of <code class="paramref">data</code> as laid out in memory, which must
match the shader's block (a sequential struct of floats and vectors with the block's padding). Draws recorded
afterwards use it; draws already recorded keep what they had.

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

The fragment shader declares no such slot, or the data is empty or larger than [MaxUniformBytes](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxUniformBytes).

### <a id="MicroEngine_Graphics_Material2D_SetUniforms_System_ReadOnlySpan_System_Byte__System_Int32_"></a> SetUniforms\(ReadOnlySpan<byte\>, int\)

Sets the data of a fragment uniform slot from bytes (see [SetUniforms``1](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_SetUniforms__1___0__System_Int32_)).

```csharp
public void SetUniforms(ReadOnlySpan<byte> data, int slot = 0)
```

#### Parameters

`data` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

`slot` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The fragment shader declares no such slot, or the data is empty or larger than [MaxUniformBytes](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxUniformBytes).

</div>
