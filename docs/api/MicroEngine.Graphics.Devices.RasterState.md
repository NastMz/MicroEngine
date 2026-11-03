# Struct RasterState

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Rasterizer state. Scissor is not here: it is dynamic pass state, always on ([SetScissor](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_SetScissor_System_Nullable_MicroEngine_Graphics_Devices_RectI__)).

```csharp
public readonly struct RasterState : IEquatable<RasterState>
```

#### Implements

[IEquatable<RasterState\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_RasterState__ctor_MicroEngine_Graphics_Devices_CullMode_MicroEngine_Graphics_Devices_FrontFace_"></a> RasterState\(CullMode, FrontFace\)

Rasterizer state. Scissor is not here: it is dynamic pass state, always on ([SetScissor](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_SetScissor_System_Nullable_MicroEngine_Graphics_Devices_RectI__)).

```csharp
public RasterState(CullMode Cull = CullMode.None, FrontFace Front = FrontFace.CounterClockwise)
```

#### Parameters

`Cull` [CullMode](MicroEngine.Graphics.Devices.CullMode.md)

Culling.

`Front` [FrontFace](MicroEngine.Graphics.Devices.FrontFace.md)

Front-face winding.

## Properties

### <a id="MicroEngine_Graphics_Devices_RasterState_Cull"></a> Cull

Culling.

```csharp
public CullMode Cull { get; init; }
```

#### Property Value

 [CullMode](MicroEngine.Graphics.Devices.CullMode.md)

### <a id="MicroEngine_Graphics_Devices_RasterState_Front"></a> Front

Front-face winding.

```csharp
public FrontFace Front { get; init; }
```

#### Property Value

 [FrontFace](MicroEngine.Graphics.Devices.FrontFace.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_RasterState_Deconstruct_MicroEngine_Graphics_Devices_CullMode__MicroEngine_Graphics_Devices_FrontFace__"></a> Deconstruct\(out CullMode, out FrontFace\)

```csharp
public void Deconstruct(out CullMode Cull, out FrontFace Front)
```

#### Parameters

`Cull` [CullMode](MicroEngine.Graphics.Devices.CullMode.md)

`Front` [FrontFace](MicroEngine.Graphics.Devices.FrontFace.md)

### <a id="MicroEngine_Graphics_Devices_RasterState_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_RasterState_Equals_MicroEngine_Graphics_Devices_RasterState_"></a> Equals\(RasterState\)

```csharp
public bool Equals(RasterState other)
```

#### Parameters

`other` [RasterState](MicroEngine.Graphics.Devices.RasterState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_RasterState_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_RasterState_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_RasterState_op_Equality_MicroEngine_Graphics_Devices_RasterState_MicroEngine_Graphics_Devices_RasterState_"></a> operator ==\(RasterState, RasterState\)

```csharp
public static bool operator ==(RasterState left, RasterState right)
```

#### Parameters

`left` [RasterState](MicroEngine.Graphics.Devices.RasterState.md)

`right` [RasterState](MicroEngine.Graphics.Devices.RasterState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_RasterState_op_Inequality_MicroEngine_Graphics_Devices_RasterState_MicroEngine_Graphics_Devices_RasterState_"></a> operator \!=\(RasterState, RasterState\)

```csharp
public static bool operator !=(RasterState left, RasterState right)
```

#### Parameters

`left` [RasterState](MicroEngine.Graphics.Devices.RasterState.md)

`right` [RasterState](MicroEngine.Graphics.Devices.RasterState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
