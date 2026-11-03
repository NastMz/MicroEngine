# Struct DepthState

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Depth test and write. Depth is 0 (near) .. 1 (far); anything but [None](MicroEngine.Graphics.Devices.DepthState.md#MicroEngine_Graphics_Devices_DepthState_None) requires [DepthBuffer](MicroEngine.Graphics.Devices.GraphicsFeatures.md).

```csharp
public readonly struct DepthState : IEquatable<DepthState>
```

#### Implements

[IEquatable<DepthState\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_DepthState__ctor_System_Boolean_System_Boolean_MicroEngine_Graphics_Devices_CompareOp_"></a> DepthState\(bool, bool, CompareOp\)

Depth test and write. Depth is 0 (near) .. 1 (far); anything but [None](MicroEngine.Graphics.Devices.DepthState.md#MicroEngine_Graphics_Devices_DepthState_None) requires [DepthBuffer](MicroEngine.Graphics.Devices.GraphicsFeatures.md).

```csharp
public DepthState(bool Test, bool Write, CompareOp Compare)
```

#### Parameters

`Test` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Compare against the depth attachment.

`Write` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Write passing depths.

`Compare` [CompareOp](MicroEngine.Graphics.Devices.CompareOp.md)

The comparison when <code class="paramref">Test</code> is set.

## Fields

### <a id="MicroEngine_Graphics_Devices_DepthState_LessWrite"></a> LessWrite

Test less, write: the usual opaque 3D state.

```csharp
public static readonly DepthState LessWrite
```

#### Field Value

 [DepthState](MicroEngine.Graphics.Devices.DepthState.md)

### <a id="MicroEngine_Graphics_Devices_DepthState_None"></a> None

No depth test, no depth write.

```csharp
public static readonly DepthState None
```

#### Field Value

 [DepthState](MicroEngine.Graphics.Devices.DepthState.md)

## Properties

### <a id="MicroEngine_Graphics_Devices_DepthState_Compare"></a> Compare

The comparison when <code class="paramref">Test</code> is set.

```csharp
public CompareOp Compare { get; init; }
```

#### Property Value

 [CompareOp](MicroEngine.Graphics.Devices.CompareOp.md)

### <a id="MicroEngine_Graphics_Devices_DepthState_Test"></a> Test

Compare against the depth attachment.

```csharp
public bool Test { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_DepthState_Write"></a> Write

Write passing depths.

```csharp
public bool Write { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Graphics_Devices_DepthState_Deconstruct_System_Boolean__System_Boolean__MicroEngine_Graphics_Devices_CompareOp__"></a> Deconstruct\(out bool, out bool, out CompareOp\)

```csharp
public void Deconstruct(out bool Test, out bool Write, out CompareOp Compare)
```

#### Parameters

`Test` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Write` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Compare` [CompareOp](MicroEngine.Graphics.Devices.CompareOp.md)

### <a id="MicroEngine_Graphics_Devices_DepthState_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_DepthState_Equals_MicroEngine_Graphics_Devices_DepthState_"></a> Equals\(DepthState\)

```csharp
public bool Equals(DepthState other)
```

#### Parameters

`other` [DepthState](MicroEngine.Graphics.Devices.DepthState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_DepthState_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_DepthState_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_DepthState_op_Equality_MicroEngine_Graphics_Devices_DepthState_MicroEngine_Graphics_Devices_DepthState_"></a> operator ==\(DepthState, DepthState\)

```csharp
public static bool operator ==(DepthState left, DepthState right)
```

#### Parameters

`left` [DepthState](MicroEngine.Graphics.Devices.DepthState.md)

`right` [DepthState](MicroEngine.Graphics.Devices.DepthState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_DepthState_op_Inequality_MicroEngine_Graphics_Devices_DepthState_MicroEngine_Graphics_Devices_DepthState_"></a> operator \!=\(DepthState, DepthState\)

```csharp
public static bool operator !=(DepthState left, DepthState right)
```

#### Parameters

`left` [DepthState](MicroEngine.Graphics.Devices.DepthState.md)

`right` [DepthState](MicroEngine.Graphics.Devices.DepthState.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
