# Struct Command

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

One validated command, as a backend reads it from [Commands](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_Commands).

```csharp
public readonly struct Command : IEquatable<Command>
```

#### Implements

[IEquatable<Command\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_Command__ctor_MicroEngine_Graphics_Devices_CommandKind_MicroEngine_Graphics_Devices_GraphicsResource_MicroEngine_Graphics_Devices_Sampler_MicroEngine_Graphics_Devices_PassDescription_MicroEngine_Graphics_Devices_RectI_System_Int32_System_Int32_System_Int32_System_Int32_System_Int32_MicroEngine_Graphics_Devices_ShaderStage_MicroEngine_Graphics_Devices_IndexSize_System_Int32_System_Int32_"></a> Command\(CommandKind, GraphicsResource?, Sampler?, PassDescription, RectI, int, int, int, int, int, ShaderStage, IndexSize, int, int\)

One validated command, as a backend reads it from [Commands](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_Commands).

```csharp
public Command(CommandKind Kind, GraphicsResource? Resource = null, Sampler? Sampler = null, PassDescription Pass = default, RectI Rect = default, int Slot = 0, int Count = 0, int First = 0, int BaseVertex = 0, int Instances = 1, ShaderStage Stage = ShaderStage.Vertex, IndexSize IndexSize = IndexSize.Bits16, int PayloadOffset = 0, int PayloadLength = 0)
```

#### Parameters

`Kind` [CommandKind](MicroEngine.Graphics.Devices.CommandKind.md)

Kind.

`Resource` [GraphicsResource](MicroEngine.Graphics.Devices.GraphicsResource.md)?

Pipeline, texture or buffer, by kind.

`Sampler` [Sampler](MicroEngine.Graphics.Devices.Sampler.md)?

Sampler for [SetTexture](MicroEngine.Graphics.Devices.CommandKind.md).

`Pass` [PassDescription](MicroEngine.Graphics.Devices.PassDescription.md)

Pass for [BeginPass](MicroEngine.Graphics.Devices.CommandKind.md).

`Rect` [RectI](MicroEngine.Graphics.Devices.RectI.md)

Viewport, scissor or texture region.

`Slot` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Texture or uniform slot.

`Count` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Vertex or index count.

`First` [int](https://learn.microsoft.com/dotnet/api/system.int32)

First vertex, first index, or byte offset.

`BaseVertex` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Added to each index.

`Instances` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Instance count.

`Stage` [ShaderStage](MicroEngine.Graphics.Devices.ShaderStage.md)

Uniform stage.

`IndexSize` [IndexSize](MicroEngine.Graphics.Devices.IndexSize.md)

Index size.

`PayloadOffset` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Offset of the data in the list's payload.

`PayloadLength` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Length of the data.

## Properties

### <a id="MicroEngine_Graphics_Devices_Command_BaseVertex"></a> BaseVertex

Added to each index.

```csharp
public int BaseVertex { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_Command_Count"></a> Count

Vertex or index count.

```csharp
public int Count { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_Command_First"></a> First

First vertex, first index, or byte offset.

```csharp
public int First { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_Command_IndexSize"></a> IndexSize

Index size.

```csharp
public IndexSize IndexSize { get; init; }
```

#### Property Value

 [IndexSize](MicroEngine.Graphics.Devices.IndexSize.md)

### <a id="MicroEngine_Graphics_Devices_Command_Instances"></a> Instances

Instance count.

```csharp
public int Instances { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_Command_Kind"></a> Kind

Kind.

```csharp
public CommandKind Kind { get; init; }
```

#### Property Value

 [CommandKind](MicroEngine.Graphics.Devices.CommandKind.md)

### <a id="MicroEngine_Graphics_Devices_Command_Pass"></a> Pass

Pass for [BeginPass](MicroEngine.Graphics.Devices.CommandKind.md).

```csharp
public PassDescription Pass { get; init; }
```

#### Property Value

 [PassDescription](MicroEngine.Graphics.Devices.PassDescription.md)

### <a id="MicroEngine_Graphics_Devices_Command_PayloadLength"></a> PayloadLength

Length of the data.

```csharp
public int PayloadLength { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_Command_PayloadOffset"></a> PayloadOffset

Offset of the data in the list's payload.

```csharp
public int PayloadOffset { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_Command_Rect"></a> Rect

Viewport, scissor or texture region.

```csharp
public RectI Rect { get; init; }
```

#### Property Value

 [RectI](MicroEngine.Graphics.Devices.RectI.md)

### <a id="MicroEngine_Graphics_Devices_Command_Resource"></a> Resource

Pipeline, texture or buffer, by kind.

```csharp
public GraphicsResource? Resource { get; init; }
```

#### Property Value

 [GraphicsResource](MicroEngine.Graphics.Devices.GraphicsResource.md)?

### <a id="MicroEngine_Graphics_Devices_Command_Sampler"></a> Sampler

Sampler for [SetTexture](MicroEngine.Graphics.Devices.CommandKind.md).

```csharp
public Sampler? Sampler { get; init; }
```

#### Property Value

 [Sampler](MicroEngine.Graphics.Devices.Sampler.md)?

### <a id="MicroEngine_Graphics_Devices_Command_Slot"></a> Slot

Texture or uniform slot.

```csharp
public int Slot { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_Command_Stage"></a> Stage

Uniform stage.

```csharp
public ShaderStage Stage { get; init; }
```

#### Property Value

 [ShaderStage](MicroEngine.Graphics.Devices.ShaderStage.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_Command_Deconstruct_MicroEngine_Graphics_Devices_CommandKind__MicroEngine_Graphics_Devices_GraphicsResource__MicroEngine_Graphics_Devices_Sampler__MicroEngine_Graphics_Devices_PassDescription__MicroEngine_Graphics_Devices_RectI__System_Int32__System_Int32__System_Int32__System_Int32__System_Int32__MicroEngine_Graphics_Devices_ShaderStage__MicroEngine_Graphics_Devices_IndexSize__System_Int32__System_Int32__"></a> Deconstruct\(out CommandKind, out GraphicsResource?, out Sampler?, out PassDescription, out RectI, out int, out int, out int, out int, out int, out ShaderStage, out IndexSize, out int, out int\)

```csharp
public void Deconstruct(out CommandKind Kind, out GraphicsResource? Resource, out Sampler? Sampler, out PassDescription Pass, out RectI Rect, out int Slot, out int Count, out int First, out int BaseVertex, out int Instances, out ShaderStage Stage, out IndexSize IndexSize, out int PayloadOffset, out int PayloadLength)
```

#### Parameters

`Kind` [CommandKind](MicroEngine.Graphics.Devices.CommandKind.md)

`Resource` [GraphicsResource](MicroEngine.Graphics.Devices.GraphicsResource.md)?

`Sampler` [Sampler](MicroEngine.Graphics.Devices.Sampler.md)?

`Pass` [PassDescription](MicroEngine.Graphics.Devices.PassDescription.md)

`Rect` [RectI](MicroEngine.Graphics.Devices.RectI.md)

`Slot` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Count` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`First` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`BaseVertex` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Instances` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Stage` [ShaderStage](MicroEngine.Graphics.Devices.ShaderStage.md)

`IndexSize` [IndexSize](MicroEngine.Graphics.Devices.IndexSize.md)

`PayloadOffset` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`PayloadLength` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_Command_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_Command_Equals_MicroEngine_Graphics_Devices_Command_"></a> Equals\(Command\)

```csharp
public bool Equals(Command other)
```

#### Parameters

`other` [Command](MicroEngine.Graphics.Devices.Command.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_Command_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_Command_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_Command_op_Equality_MicroEngine_Graphics_Devices_Command_MicroEngine_Graphics_Devices_Command_"></a> operator ==\(Command, Command\)

```csharp
public static bool operator ==(Command left, Command right)
```

#### Parameters

`left` [Command](MicroEngine.Graphics.Devices.Command.md)

`right` [Command](MicroEngine.Graphics.Devices.Command.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_Command_op_Inequality_MicroEngine_Graphics_Devices_Command_MicroEngine_Graphics_Devices_Command_"></a> operator \!=\(Command, Command\)

```csharp
public static bool operator !=(Command left, Command right)
```

#### Parameters

`left` [Command](MicroEngine.Graphics.Devices.Command.md)

`right` [Command](MicroEngine.Graphics.Devices.Command.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
