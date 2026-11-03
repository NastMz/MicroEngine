# Class PipelineDescription

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A custom pipeline: requires [Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md). The pipeline is bound to the attachment formats
it renders to: a pass accepts it only when its colour format equals <code class="paramref">ColorFormat</code> and its depth
attachment format equals <code class="paramref">DepthFormat</code> (null = the pass has no depth attachment).

```csharp
public sealed record PipelineDescription : IEquatable<PipelineDescription>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PipelineDescription](MicroEngine.Graphics.Devices.PipelineDescription.md)

#### Implements

[IEquatable<PipelineDescription\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_PipelineDescription__ctor_MicroEngine_Graphics_Devices_Shader_MicroEngine_Graphics_Devices_Shader_MicroEngine_Graphics_Devices_VertexLayout_MicroEngine_Graphics_Devices_BlendState_MicroEngine_Graphics_Devices_DepthState_MicroEngine_Graphics_Devices_RasterState_MicroEngine_Graphics_Devices_TextureFormat_System_Nullable_MicroEngine_Graphics_Devices_TextureFormat__"></a> PipelineDescription\(Shader, Shader, VertexLayout, BlendState, DepthState, RasterState, TextureFormat, TextureFormat?\)

A custom pipeline: requires [Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md). The pipeline is bound to the attachment formats
it renders to: a pass accepts it only when its colour format equals <code class="paramref">ColorFormat</code> and its depth
attachment format equals <code class="paramref">DepthFormat</code> (null = the pass has no depth attachment).

```csharp
public PipelineDescription(Shader Vertex, Shader Fragment, VertexLayout Layout, BlendState Blend, DepthState Depth, RasterState Raster, TextureFormat ColorFormat, TextureFormat? DepthFormat)
```

#### Parameters

`Vertex` [Shader](MicroEngine.Graphics.Devices.Shader.md)

Vertex shader.

`Fragment` [Shader](MicroEngine.Graphics.Devices.Shader.md)

Fragment shader.

`Layout` [VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)

Vertex layout.

`Blend` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

Blend state (any state: tier 1 declares [CustomBlend](MicroEngine.Graphics.Devices.GraphicsFeatures.md)).

`Depth` [DepthState](MicroEngine.Graphics.Devices.DepthState.md)

Depth state; anything but [None](MicroEngine.Graphics.Devices.DepthState.md#MicroEngine_Graphics_Devices_DepthState_None) requires a <code class="paramref">DepthFormat</code>.

`Raster` [RasterState](MicroEngine.Graphics.Devices.RasterState.md)

Rasterizer state.

`ColorFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

Format of the colour attachment.

`DepthFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)?

Format of the depth attachment, or null.

## Properties

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_Blend"></a> Blend

Blend state (any state: tier 1 declares [CustomBlend](MicroEngine.Graphics.Devices.GraphicsFeatures.md)).

```csharp
public BlendState Blend { get; init; }
```

#### Property Value

 [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_ColorFormat"></a> ColorFormat

Format of the colour attachment.

```csharp
public TextureFormat ColorFormat { get; init; }
```

#### Property Value

 [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_Depth"></a> Depth

Depth state; anything but [None](MicroEngine.Graphics.Devices.DepthState.md#MicroEngine_Graphics_Devices_DepthState_None) requires a <code class="paramref">DepthFormat</code>.

```csharp
public DepthState Depth { get; init; }
```

#### Property Value

 [DepthState](MicroEngine.Graphics.Devices.DepthState.md)

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_DepthFormat"></a> DepthFormat

Format of the depth attachment, or null.

```csharp
public TextureFormat? DepthFormat { get; init; }
```

#### Property Value

 [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)?

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_Fragment"></a> Fragment

Fragment shader.

```csharp
public Shader Fragment { get; init; }
```

#### Property Value

 [Shader](MicroEngine.Graphics.Devices.Shader.md)

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_Layout"></a> Layout

Vertex layout.

```csharp
public VertexLayout Layout { get; init; }
```

#### Property Value

 [VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_Raster"></a> Raster

Rasterizer state.

```csharp
public RasterState Raster { get; init; }
```

#### Property Value

 [RasterState](MicroEngine.Graphics.Devices.RasterState.md)

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_Vertex"></a> Vertex

Vertex shader.

```csharp
public Shader Vertex { get; init; }
```

#### Property Value

 [Shader](MicroEngine.Graphics.Devices.Shader.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_PipelineDescription__Clone__"></a> <Clone\>$\(\)

```csharp
public PipelineDescription <Clone>$()
```

#### Returns

 [PipelineDescription](MicroEngine.Graphics.Devices.PipelineDescription.md)

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_Deconstruct_MicroEngine_Graphics_Devices_Shader__MicroEngine_Graphics_Devices_Shader__MicroEngine_Graphics_Devices_VertexLayout__MicroEngine_Graphics_Devices_BlendState__MicroEngine_Graphics_Devices_DepthState__MicroEngine_Graphics_Devices_RasterState__MicroEngine_Graphics_Devices_TextureFormat__System_Nullable_MicroEngine_Graphics_Devices_TextureFormat___"></a> Deconstruct\(out Shader, out Shader, out VertexLayout, out BlendState, out DepthState, out RasterState, out TextureFormat, out TextureFormat?\)

```csharp
public void Deconstruct(out Shader Vertex, out Shader Fragment, out VertexLayout Layout, out BlendState Blend, out DepthState Depth, out RasterState Raster, out TextureFormat ColorFormat, out TextureFormat? DepthFormat)
```

#### Parameters

`Vertex` [Shader](MicroEngine.Graphics.Devices.Shader.md)

`Fragment` [Shader](MicroEngine.Graphics.Devices.Shader.md)

`Layout` [VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)

`Blend` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

`Depth` [DepthState](MicroEngine.Graphics.Devices.DepthState.md)

`Raster` [RasterState](MicroEngine.Graphics.Devices.RasterState.md)

`ColorFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

`DepthFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)?

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_Equals_MicroEngine_Graphics_Devices_PipelineDescription_"></a> Equals\(PipelineDescription?\)

```csharp
public bool Equals(PipelineDescription? other)
```

#### Parameters

`other` [PipelineDescription](MicroEngine.Graphics.Devices.PipelineDescription.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_op_Equality_MicroEngine_Graphics_Devices_PipelineDescription_MicroEngine_Graphics_Devices_PipelineDescription_"></a> operator ==\(PipelineDescription?, PipelineDescription?\)

```csharp
public static bool operator ==(PipelineDescription? left, PipelineDescription? right)
```

#### Parameters

`left` [PipelineDescription](MicroEngine.Graphics.Devices.PipelineDescription.md)?

`right` [PipelineDescription](MicroEngine.Graphics.Devices.PipelineDescription.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_PipelineDescription_op_Inequality_MicroEngine_Graphics_Devices_PipelineDescription_MicroEngine_Graphics_Devices_PipelineDescription_"></a> operator \!=\(PipelineDescription?, PipelineDescription?\)

```csharp
public static bool operator !=(PipelineDescription? left, PipelineDescription? right)
```

#### Parameters

`left` [PipelineDescription](MicroEngine.Graphics.Devices.PipelineDescription.md)?

`right` [PipelineDescription](MicroEngine.Graphics.Devices.PipelineDescription.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
