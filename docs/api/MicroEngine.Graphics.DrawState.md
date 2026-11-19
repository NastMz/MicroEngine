# Struct DrawState

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

What a renderer draws with between [PushState](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushState_MicroEngine_Graphics_DrawState__) and [PopState](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PopState): a blend
state, a sampler and a material. A part left null keeps what the renderer was drawing with, so a state names only
what it changes.

```csharp
public readonly struct DrawState : IEquatable<DrawState>
```

#### Implements

[IEquatable<DrawState\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Examples

```csharp
renderer.PushState(DrawState.Additive);                              // one blend state
renderer.PushState(DrawState.Opaque with { Material = water });      // a material, replacing what is under it
renderer.PushState(new DrawState(Material: outline).Filtered(TextureFilter.Linear));
```

## Constructors

### <a id="MicroEngine_Graphics_DrawState__ctor_MicroEngine_Graphics_Devices_BlendState_System_Nullable_MicroEngine_Graphics_Devices_SamplerDescription__MicroEngine_Graphics_Material2D_"></a> DrawState\(BlendState?, SamplerDescription?, Material2D?\)

What a renderer draws with between [PushState](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushState_MicroEngine_Graphics_DrawState__) and [PopState](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PopState): a blend
state, a sampler and a material. A part left null keeps what the renderer was drawing with, so a state names only
what it changes.

```csharp
public DrawState(BlendState? Blend = null, SamplerDescription? Sampler = null, Material2D? Material = null)
```

#### Parameters

`Blend` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)?

How draws combine with what is there (colours are premultiplied by then). The four presets
([Opaque](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_Opaque), [AlphaPremultiplied](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_AlphaPremultiplied), [Additive](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_Additive),
[Multiply](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_Multiply)) work on every device; any other needs a device that declares
[CustomBlend](MicroEngine.Graphics.Devices.GraphicsFeatures.md). Null keeps the current blending.

`Sampler` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)?

How textures are sampled; null lets each texture's [Filter](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Filter) choose, as usual.

`Material` [Material2D](MicroEngine.Graphics.Material2D.md)?

The shader draws go through ([Material2D](MicroEngine.Graphics.Material2D.md)); null keeps the current one.

#### Examples

```csharp
renderer.PushState(DrawState.Additive);                              // one blend state
renderer.PushState(DrawState.Opaque with { Material = water });      // a material, replacing what is under it
renderer.PushState(new DrawState(Material: outline).Filtered(TextureFilter.Linear));
```

## Properties

### <a id="MicroEngine_Graphics_DrawState_Additive"></a> Additive

Draws add light ([Additive](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_Additive)), as [Additive](MicroEngine.Graphics.BlendMode.md).

```csharp
public static DrawState Additive { get; }
```

#### Property Value

 [DrawState](MicroEngine.Graphics.DrawState.md)

### <a id="MicroEngine_Graphics_DrawState_Alpha"></a> Alpha

Normal transparency ([AlphaPremultiplied](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_AlphaPremultiplied)), as [Alpha](MicroEngine.Graphics.BlendMode.md).

```csharp
public static DrawState Alpha { get; }
```

#### Property Value

 [DrawState](MicroEngine.Graphics.DrawState.md)

### <a id="MicroEngine_Graphics_DrawState_Blend"></a> Blend

How draws combine with what is there (colours are premultiplied by then). The four presets
([Opaque](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_Opaque), [AlphaPremultiplied](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_AlphaPremultiplied), [Additive](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_Additive),
[Multiply](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_Multiply)) work on every device; any other needs a device that declares
[CustomBlend](MicroEngine.Graphics.Devices.GraphicsFeatures.md). Null keeps the current blending.

```csharp
public BlendState? Blend { get; init; }
```

#### Property Value

 [BlendState](MicroEngine.Graphics.Devices.BlendState.md)?

### <a id="MicroEngine_Graphics_DrawState_Material"></a> Material

The shader draws go through ([Material2D](MicroEngine.Graphics.Material2D.md)); null keeps the current one.

```csharp
public Material2D? Material { get; init; }
```

#### Property Value

 [Material2D](MicroEngine.Graphics.Material2D.md)?

### <a id="MicroEngine_Graphics_DrawState_Multiply"></a> Multiply

Draws multiply what is there ([Multiply](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_Multiply)), as [Multiply](MicroEngine.Graphics.BlendMode.md).

```csharp
public static DrawState Multiply { get; }
```

#### Property Value

 [DrawState](MicroEngine.Graphics.DrawState.md)

### <a id="MicroEngine_Graphics_DrawState_Opaque"></a> Opaque

Draws replace what is there, alpha included ([Opaque](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_Opaque)): copies of whole pictures.

```csharp
public static DrawState Opaque { get; }
```

#### Property Value

 [DrawState](MicroEngine.Graphics.DrawState.md)

### <a id="MicroEngine_Graphics_DrawState_Sampler"></a> Sampler

How textures are sampled; null lets each texture's [Filter](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Filter) choose, as usual.

```csharp
public SamplerDescription? Sampler { get; init; }
```

#### Property Value

 [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)?

## Methods

### <a id="MicroEngine_Graphics_DrawState_Deconstruct_MicroEngine_Graphics_Devices_BlendState__System_Nullable_MicroEngine_Graphics_Devices_SamplerDescription___MicroEngine_Graphics_Material2D__"></a> Deconstruct\(out BlendState?, out SamplerDescription?, out Material2D?\)

```csharp
public void Deconstruct(out BlendState? Blend, out SamplerDescription? Sampler, out Material2D? Material)
```

#### Parameters

`Blend` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)?

`Sampler` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)?

`Material` [Material2D](MicroEngine.Graphics.Material2D.md)?

### <a id="MicroEngine_Graphics_DrawState_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_DrawState_Equals_MicroEngine_Graphics_DrawState_"></a> Equals\(DrawState\)

```csharp
public bool Equals(DrawState other)
```

#### Parameters

`other` [DrawState](MicroEngine.Graphics.DrawState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_DrawState_Filtered_MicroEngine_Graphics_TextureFilter_"></a> Filtered\(TextureFilter\)

This state sampling every texture with <code class="paramref">filter</code>, clamped at the edges, whatever the texture's own filter.

```csharp
public DrawState Filtered(TextureFilter filter)
```

#### Parameters

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

#### Returns

 [DrawState](MicroEngine.Graphics.DrawState.md)

### <a id="MicroEngine_Graphics_DrawState_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_DrawState_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_DrawState_op_Equality_MicroEngine_Graphics_DrawState_MicroEngine_Graphics_DrawState_"></a> operator ==\(DrawState, DrawState\)

```csharp
public static bool operator ==(DrawState left, DrawState right)
```

#### Parameters

`left` [DrawState](MicroEngine.Graphics.DrawState.md)

`right` [DrawState](MicroEngine.Graphics.DrawState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_DrawState_op_Inequality_MicroEngine_Graphics_DrawState_MicroEngine_Graphics_DrawState_"></a> operator \!=\(DrawState, DrawState\)

```csharp
public static bool operator !=(DrawState left, DrawState right)
```

#### Parameters

`left` [DrawState](MicroEngine.Graphics.DrawState.md)

`right` [DrawState](MicroEngine.Graphics.DrawState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
