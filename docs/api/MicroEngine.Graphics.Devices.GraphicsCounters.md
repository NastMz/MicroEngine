# Struct GraphicsCounters

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Deterministic counts of the work a device received ([Counters](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_Counters)): the same calls give
the same counts on every backend and every run, so tests can assert draw calls, passes and uploads exactly as a
performance budget. Every count is cumulative since the device was created; subtract two readings to measure a span.

```csharp
public readonly struct GraphicsCounters : IEquatable<GraphicsCounters>
```

#### Implements

[IEquatable<GraphicsCounters\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters__ctor_System_Int64_System_Int64_System_Int64_System_Int64_System_Int64_System_Int64_System_Int64_System_Int64_System_Int64_System_Int64_System_Int32_"></a> GraphicsCounters\(long, long, long, long, long, long, long, long, long, long, int\)

Deterministic counts of the work a device received ([Counters](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_Counters)): the same calls give
the same counts on every backend and every run, so tests can assert draw calls, passes and uploads exactly as a
performance budget. Every count is cumulative since the device was created; subtract two readings to measure a span.

```csharp
public GraphicsCounters(long Submissions = 0, long Passes = 0, long DrawCalls = 0, long PipelineChanges = 0, long TextureBindings = 0, long BufferUpdates = 0, long TextureUpdates = 0, long BytesUploaded = 0, long Presents = 0, long ResourcesCreated = 0, int LiveResources = 0)
```

#### Parameters

`Submissions` [long](https://learn.microsoft.com/dotnet/api/system.int64)

Command lists submitted.

`Passes` [long](https://learn.microsoft.com/dotnet/api/system.int64)

Render passes in the submitted lists.

`DrawCalls` [long](https://learn.microsoft.com/dotnet/api/system.int64)

<code>Draw</code> and <code>DrawIndexed</code> commands in the submitted lists.

`PipelineChanges` [long](https://learn.microsoft.com/dotnet/api/system.int64)

<code>SetPipeline</code> commands in the submitted lists.

`TextureBindings` [long](https://learn.microsoft.com/dotnet/api/system.int64)

<code>SetTexture</code> commands in the submitted lists.

`BufferUpdates` [long](https://learn.microsoft.com/dotnet/api/system.int64)

<code>UpdateBuffer</code> commands in the submitted lists.

`TextureUpdates` [long](https://learn.microsoft.com/dotnet/api/system.int64)

<code>UpdateTexture</code> commands in the submitted lists.

`BytesUploaded` [long](https://learn.microsoft.com/dotnet/api/system.int64)

Bytes carried by the buffer and texture updates.

`Presents` [long](https://learn.microsoft.com/dotnet/api/system.int64)

Frames presented (a [Present](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Present) with an acquired surface target).

`ResourcesCreated` [long](https://learn.microsoft.com/dotnet/api/system.int64)

Resources the device created (device-owned ones included).

`LiveResources` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Resources alive now (created and not disposed or expired).

## Properties

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_BufferUpdates"></a> BufferUpdates

<code>UpdateBuffer</code> commands in the submitted lists.

```csharp
public long BufferUpdates { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_BytesUploaded"></a> BytesUploaded

Bytes carried by the buffer and texture updates.

```csharp
public long BytesUploaded { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_DrawCalls"></a> DrawCalls

<code>Draw</code> and <code>DrawIndexed</code> commands in the submitted lists.

```csharp
public long DrawCalls { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_LiveResources"></a> LiveResources

Resources alive now (created and not disposed or expired).

```csharp
public int LiveResources { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_Passes"></a> Passes

Render passes in the submitted lists.

```csharp
public long Passes { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_PipelineChanges"></a> PipelineChanges

<code>SetPipeline</code> commands in the submitted lists.

```csharp
public long PipelineChanges { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_Presents"></a> Presents

Frames presented (a [Present](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Present) with an acquired surface target).

```csharp
public long Presents { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_ResourcesCreated"></a> ResourcesCreated

Resources the device created (device-owned ones included).

```csharp
public long ResourcesCreated { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_Submissions"></a> Submissions

Command lists submitted.

```csharp
public long Submissions { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_TextureBindings"></a> TextureBindings

<code>SetTexture</code> commands in the submitted lists.

```csharp
public long TextureBindings { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_TextureUpdates"></a> TextureUpdates

<code>UpdateTexture</code> commands in the submitted lists.

```csharp
public long TextureUpdates { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

## Methods

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_Deconstruct_System_Int64__System_Int64__System_Int64__System_Int64__System_Int64__System_Int64__System_Int64__System_Int64__System_Int64__System_Int64__System_Int32__"></a> Deconstruct\(out long, out long, out long, out long, out long, out long, out long, out long, out long, out long, out int\)

```csharp
public void Deconstruct(out long Submissions, out long Passes, out long DrawCalls, out long PipelineChanges, out long TextureBindings, out long BufferUpdates, out long TextureUpdates, out long BytesUploaded, out long Presents, out long ResourcesCreated, out int LiveResources)
```

#### Parameters

`Submissions` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`Passes` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`DrawCalls` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`PipelineChanges` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`TextureBindings` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`BufferUpdates` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`TextureUpdates` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`BytesUploaded` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`Presents` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`ResourcesCreated` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`LiveResources` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_Equals_MicroEngine_Graphics_Devices_GraphicsCounters_"></a> Equals\(GraphicsCounters\)

```csharp
public bool Equals(GraphicsCounters other)
```

#### Parameters

`other` [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_op_Equality_MicroEngine_Graphics_Devices_GraphicsCounters_MicroEngine_Graphics_Devices_GraphicsCounters_"></a> operator ==\(GraphicsCounters, GraphicsCounters\)

```csharp
public static bool operator ==(GraphicsCounters left, GraphicsCounters right)
```

#### Parameters

`left` [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

`right` [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_op_Inequality_MicroEngine_Graphics_Devices_GraphicsCounters_MicroEngine_Graphics_Devices_GraphicsCounters_"></a> operator \!=\(GraphicsCounters, GraphicsCounters\)

```csharp
public static bool operator !=(GraphicsCounters left, GraphicsCounters right)
```

#### Parameters

`left` [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

`right` [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsCounters_op_Subtraction_MicroEngine_Graphics_Devices_GraphicsCounters_MicroEngine_Graphics_Devices_GraphicsCounters_"></a> operator \-\(GraphicsCounters, GraphicsCounters\)

The counts accumulated between two readings ([LiveResources](MicroEngine.Graphics.Devices.GraphicsCounters.md#MicroEngine_Graphics_Devices_GraphicsCounters_LiveResources) is the later reading's).

```csharp
public static GraphicsCounters operator -(GraphicsCounters later, GraphicsCounters earlier)
```

#### Parameters

`later` [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

`earlier` [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

#### Returns

 [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

</div>
