# Class Pipeline

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A graphics pipeline: built-in (device-owned) or custom.

```csharp
public abstract class Pipeline : GraphicsResource, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsResource](MicroEngine.Graphics.Devices.GraphicsResource.md) ← 
[Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Inherited Members

[GraphicsResource.Dispose\(\)](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Dispose), 
[GraphicsResource.Release\(\)](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Release), 
[GraphicsResource.Expire\(\)](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Expire), 
[GraphicsResource.MarkLost\(\)](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_MarkLost), 
[GraphicsResource.Device](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Device), 
[GraphicsResource.Name](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Name), 
[GraphicsResource.Handle](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Handle), 
[GraphicsResource.IsDisposed](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_IsDisposed), 
[GraphicsResource.IsLost](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_IsLost), 
[GraphicsResource.OwnedBy](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_OwnedBy), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_Graphics_Devices_Pipeline__ctor_MicroEngine_Graphics_Devices_IGraphicsDevice_System_Nullable_MicroEngine_Graphics_Devices_BuiltInPipeline__MicroEngine_Graphics_Devices_VertexLayout_MicroEngine_Graphics_Devices_BlendState_MicroEngine_Graphics_Devices_TextureFormat_System_Nullable_MicroEngine_Graphics_Devices_TextureFormat__MicroEngine_Graphics_Devices_ShaderResources_MicroEngine_Graphics_Devices_ShaderResources_"></a> Pipeline\(IGraphicsDevice, BuiltInPipeline?, VertexLayout, BlendState, TextureFormat, TextureFormat?, ShaderResources, ShaderResources\)

Creates the pipeline object.

```csharp
protected Pipeline(IGraphicsDevice device, BuiltInPipeline? builtIn, VertexLayout layout, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat, ShaderResources vertexResources, ShaderResources fragmentResources)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

`builtIn` [BuiltInPipeline](MicroEngine.Graphics.Devices.BuiltInPipeline.md)?

`layout` [VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)

`blend` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

`colorFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

`depthFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)?

`vertexResources` [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)

`fragmentResources` [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)

## Properties

### <a id="MicroEngine_Graphics_Devices_Pipeline_Blend"></a> Blend

Blend state.

```csharp
public BlendState Blend { get; }
```

#### Property Value

 [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

### <a id="MicroEngine_Graphics_Devices_Pipeline_BuiltIn"></a> BuiltIn

The built-in kind, or null for a custom pipeline.

```csharp
public BuiltInPipeline? BuiltIn { get; }
```

#### Property Value

 [BuiltInPipeline](MicroEngine.Graphics.Devices.BuiltInPipeline.md)?

### <a id="MicroEngine_Graphics_Devices_Pipeline_ColorFormat"></a> ColorFormat

Colour attachment format the pipeline renders to.

```csharp
public TextureFormat ColorFormat { get; }
```

#### Property Value

 [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

### <a id="MicroEngine_Graphics_Devices_Pipeline_DepthFormat"></a> DepthFormat

Depth attachment format, or null for passes without depth.

```csharp
public TextureFormat? DepthFormat { get; }
```

#### Property Value

 [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)?

### <a id="MicroEngine_Graphics_Devices_Pipeline_FragmentResources"></a> FragmentResources

Fragment-stage resources (built-ins: one sampler).

```csharp
public ShaderResources FragmentResources { get; }
```

#### Property Value

 [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)

### <a id="MicroEngine_Graphics_Devices_Pipeline_Layout"></a> Layout

Vertex layout ([Vertex2D](MicroEngine.Graphics.Devices.VertexLayout.md#MicroEngine_Graphics_Devices_VertexLayout_Vertex2D) for built-ins).

```csharp
public VertexLayout Layout { get; }
```

#### Property Value

 [VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)

### <a id="MicroEngine_Graphics_Devices_Pipeline_OwnedBy"></a> OwnedBy

When not null, [Dispose](MicroEngine.Graphics.Devices.GraphicsResource.md#MicroEngine_Graphics_Devices_GraphicsResource_Dispose) throws `InvalidOperationException` with this text: device-owned
objects (built-in pipelines, a render target's colour texture, the surface target) are not disposed by callers.

```csharp
protected override string? OwnedBy { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_Devices_Pipeline_VertexResources"></a> VertexResources

Vertex-stage resources (built-ins: none visible to callers).

```csharp
public ShaderResources VertexResources { get; }
```

#### Property Value

 [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)

</div>
