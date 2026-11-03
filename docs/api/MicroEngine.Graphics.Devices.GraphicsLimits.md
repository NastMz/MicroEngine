# Class GraphicsLimits

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Limits. Each value is a guaranteed minimum: a backend that cannot query its API reports the minimum the API
specification guarantees, so a limit may be conservative but never optimistic.

```csharp
public sealed record GraphicsLimits : IEquatable<GraphicsLimits>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsLimits](MicroEngine.Graphics.Devices.GraphicsLimits.md)

#### Implements

[IEquatable<GraphicsLimits\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits__ctor_System_Int32_System_Int32_System_Int32_System_Int32_System_Int32_"></a> GraphicsLimits\(int, int, int, int, int\)

Limits. Each value is a guaranteed minimum: a backend that cannot query its API reports the minimum the API
specification guarantees, so a limit may be conservative but never optimistic.

```csharp
public GraphicsLimits(int MaxTextureSize, int MaxRenderTargetSize, int MaxTextureSlots, int MaxUniformBytes, int MaxVertexAttributes)
```

#### Parameters

`MaxTextureSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Largest texture width or height.

`MaxRenderTargetSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Largest render target width or height.

`MaxTextureSlots` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Texture-sampler slots per fragment stage.

`MaxUniformBytes` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Bytes per uniform slot (0 on tier 0).

`MaxVertexAttributes` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Attributes per vertex layout.

## Properties

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits_MaxRenderTargetSize"></a> MaxRenderTargetSize

Largest render target width or height.

```csharp
public int MaxRenderTargetSize { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits_MaxTextureSize"></a> MaxTextureSize

Largest texture width or height.

```csharp
public int MaxTextureSize { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits_MaxTextureSlots"></a> MaxTextureSlots

Texture-sampler slots per fragment stage.

```csharp
public int MaxTextureSlots { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits_MaxUniformBytes"></a> MaxUniformBytes

Bytes per uniform slot (0 on tier 0).

```csharp
public int MaxUniformBytes { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits_MaxVertexAttributes"></a> MaxVertexAttributes

Attributes per vertex layout.

```csharp
public int MaxVertexAttributes { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits__Clone__"></a> <Clone\>$\(\)

```csharp
public GraphicsLimits <Clone>$()
```

#### Returns

 [GraphicsLimits](MicroEngine.Graphics.Devices.GraphicsLimits.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits_Deconstruct_System_Int32__System_Int32__System_Int32__System_Int32__System_Int32__"></a> Deconstruct\(out int, out int, out int, out int, out int\)

```csharp
public void Deconstruct(out int MaxTextureSize, out int MaxRenderTargetSize, out int MaxTextureSlots, out int MaxUniformBytes, out int MaxVertexAttributes)
```

#### Parameters

`MaxTextureSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`MaxRenderTargetSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`MaxTextureSlots` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`MaxUniformBytes` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`MaxVertexAttributes` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits_Equals_MicroEngine_Graphics_Devices_GraphicsLimits_"></a> Equals\(GraphicsLimits?\)

```csharp
public bool Equals(GraphicsLimits? other)
```

#### Parameters

`other` [GraphicsLimits](MicroEngine.Graphics.Devices.GraphicsLimits.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits_op_Equality_MicroEngine_Graphics_Devices_GraphicsLimits_MicroEngine_Graphics_Devices_GraphicsLimits_"></a> operator ==\(GraphicsLimits?, GraphicsLimits?\)

```csharp
public static bool operator ==(GraphicsLimits? left, GraphicsLimits? right)
```

#### Parameters

`left` [GraphicsLimits](MicroEngine.Graphics.Devices.GraphicsLimits.md)?

`right` [GraphicsLimits](MicroEngine.Graphics.Devices.GraphicsLimits.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsLimits_op_Inequality_MicroEngine_Graphics_Devices_GraphicsLimits_MicroEngine_Graphics_Devices_GraphicsLimits_"></a> operator \!=\(GraphicsLimits?, GraphicsLimits?\)

```csharp
public static bool operator !=(GraphicsLimits? left, GraphicsLimits? right)
```

#### Parameters

`left` [GraphicsLimits](MicroEngine.Graphics.Devices.GraphicsLimits.md)?

`right` [GraphicsLimits](MicroEngine.Graphics.Devices.GraphicsLimits.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
