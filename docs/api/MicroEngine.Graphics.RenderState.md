# Struct RenderState

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

The per-draw state of the 2D batcher ([State](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_State)): blend, sampler and scissor.

```csharp
public readonly struct RenderState : IEquatable<RenderState>
```

#### Implements

[IEquatable<RenderState\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_RenderState__ctor_MicroEngine_Graphics_Devices_BlendState_MicroEngine_Graphics_Devices_SamplerDescription_System_Nullable_MicroEngine_Graphics_Devices_RectI__"></a> RenderState\(BlendState, SamplerDescription, RectI?\)

The per-draw state of the 2D batcher ([State](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_State)): blend, sampler and scissor.

```csharp
public RenderState(BlendState Blend, SamplerDescription Sampler, RectI? Scissor)
```

#### Parameters

`Blend` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

Blend state; the built-in presets work on every device.

`Sampler` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

Sampler of the draws' textures.

`Scissor` [RectI](MicroEngine.Graphics.Devices.RectI.md)?

Scissor in target pixels; null draws on the whole viewport.

## Properties

### <a id="MicroEngine_Graphics_RenderState_Blend"></a> Blend

Blend state; the built-in presets work on every device.

```csharp
public BlendState Blend { get; init; }
```

#### Property Value

 [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

### <a id="MicroEngine_Graphics_RenderState_Default"></a> Default

Premultiplied alpha blending, nearest clamped sampling, no scissor.

```csharp
public static RenderState Default { get; }
```

#### Property Value

 [RenderState](MicroEngine.Graphics.RenderState.md)

### <a id="MicroEngine_Graphics_RenderState_Sampler"></a> Sampler

Sampler of the draws' textures.

```csharp
public SamplerDescription Sampler { get; init; }
```

#### Property Value

 [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

### <a id="MicroEngine_Graphics_RenderState_Scissor"></a> Scissor

Scissor in target pixels; null draws on the whole viewport.

```csharp
public RectI? Scissor { get; init; }
```

#### Property Value

 [RectI](MicroEngine.Graphics.Devices.RectI.md)?

## Methods

### <a id="MicroEngine_Graphics_RenderState_Deconstruct_MicroEngine_Graphics_Devices_BlendState__MicroEngine_Graphics_Devices_SamplerDescription__System_Nullable_MicroEngine_Graphics_Devices_RectI___"></a> Deconstruct\(out BlendState, out SamplerDescription, out RectI?\)

```csharp
public void Deconstruct(out BlendState Blend, out SamplerDescription Sampler, out RectI? Scissor)
```

#### Parameters

`Blend` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

`Sampler` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

`Scissor` [RectI](MicroEngine.Graphics.Devices.RectI.md)?

### <a id="MicroEngine_Graphics_RenderState_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_RenderState_Equals_MicroEngine_Graphics_RenderState_"></a> Equals\(RenderState\)

```csharp
public bool Equals(RenderState other)
```

#### Parameters

`other` [RenderState](MicroEngine.Graphics.RenderState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_RenderState_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_RenderState_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_RenderState_op_Equality_MicroEngine_Graphics_RenderState_MicroEngine_Graphics_RenderState_"></a> operator ==\(RenderState, RenderState\)

```csharp
public static bool operator ==(RenderState left, RenderState right)
```

#### Parameters

`left` [RenderState](MicroEngine.Graphics.RenderState.md)

`right` [RenderState](MicroEngine.Graphics.RenderState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_RenderState_op_Inequality_MicroEngine_Graphics_RenderState_MicroEngine_Graphics_RenderState_"></a> operator \!=\(RenderState, RenderState\)

```csharp
public static bool operator !=(RenderState left, RenderState right)
```

#### Parameters

`left` [RenderState](MicroEngine.Graphics.RenderState.md)

`right` [RenderState](MicroEngine.Graphics.RenderState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
