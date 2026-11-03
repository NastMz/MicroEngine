# Class BlendState

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Fixed-function blending: <code>result = src·SrcFactor (op) dst·DstFactor</code>, separately for colour and alpha, computed
in float and quantised to the target format. The presets assume premultiplied colours.

```csharp
public sealed record BlendState : IEquatable<BlendState>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BlendState](MicroEngine.Graphics.Devices.BlendState.md)

#### Implements

[IEquatable<BlendState\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_BlendState__ctor_MicroEngine_Graphics_Devices_BlendFactor_MicroEngine_Graphics_Devices_BlendFactor_MicroEngine_Graphics_Devices_BlendOp_MicroEngine_Graphics_Devices_BlendFactor_MicroEngine_Graphics_Devices_BlendFactor_MicroEngine_Graphics_Devices_BlendOp_"></a> BlendState\(BlendFactor, BlendFactor, BlendOp, BlendFactor, BlendFactor, BlendOp\)

Fixed-function blending: <code>result = src·SrcFactor (op) dst·DstFactor</code>, separately for colour and alpha, computed
in float and quantised to the target format. The presets assume premultiplied colours.

```csharp
public BlendState(BlendFactor SrcColor, BlendFactor DstColor, BlendOp ColorOp, BlendFactor SrcAlpha, BlendFactor DstAlpha, BlendOp AlphaOp)
```

#### Parameters

`SrcColor` [BlendFactor](MicroEngine.Graphics.Devices.BlendFactor.md)

Factor applied to the source colour.

`DstColor` [BlendFactor](MicroEngine.Graphics.Devices.BlendFactor.md)

Factor applied to the destination colour.

`ColorOp` [BlendOp](MicroEngine.Graphics.Devices.BlendOp.md)

Colour operation.

`SrcAlpha` [BlendFactor](MicroEngine.Graphics.Devices.BlendFactor.md)

Factor applied to the source alpha.

`DstAlpha` [BlendFactor](MicroEngine.Graphics.Devices.BlendFactor.md)

Factor applied to the destination alpha.

`AlphaOp` [BlendOp](MicroEngine.Graphics.Devices.BlendOp.md)

Alpha operation.

## Fields

### <a id="MicroEngine_Graphics_Devices_BlendState_Additive"></a> Additive

Additive light: colour src + dst; destination alpha kept.

```csharp
public static readonly BlendState Additive
```

#### Field Value

 [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

### <a id="MicroEngine_Graphics_Devices_BlendState_AlphaPremultiplied"></a> AlphaPremultiplied

Premultiplied "over", the default: src + dst·(1 − srcA) for colour and alpha.

```csharp
public static readonly BlendState AlphaPremultiplied
```

#### Field Value

 [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

### <a id="MicroEngine_Graphics_Devices_BlendState_Multiply"></a> Multiply

Premultiplied multiply: colour src·dst + dst·(1 − srcA); destination alpha kept.

```csharp
public static readonly BlendState Multiply
```

#### Field Value

 [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

### <a id="MicroEngine_Graphics_Devices_BlendState_Opaque"></a> Opaque

Replace: src.

```csharp
public static readonly BlendState Opaque
```

#### Field Value

 [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

## Properties

### <a id="MicroEngine_Graphics_Devices_BlendState_AlphaOp"></a> AlphaOp

Alpha operation.

```csharp
public BlendOp AlphaOp { get; init; }
```

#### Property Value

 [BlendOp](MicroEngine.Graphics.Devices.BlendOp.md)

### <a id="MicroEngine_Graphics_Devices_BlendState_ColorOp"></a> ColorOp

Colour operation.

```csharp
public BlendOp ColorOp { get; init; }
```

#### Property Value

 [BlendOp](MicroEngine.Graphics.Devices.BlendOp.md)

### <a id="MicroEngine_Graphics_Devices_BlendState_DstAlpha"></a> DstAlpha

Factor applied to the destination alpha.

```csharp
public BlendFactor DstAlpha { get; init; }
```

#### Property Value

 [BlendFactor](MicroEngine.Graphics.Devices.BlendFactor.md)

### <a id="MicroEngine_Graphics_Devices_BlendState_DstColor"></a> DstColor

Factor applied to the destination colour.

```csharp
public BlendFactor DstColor { get; init; }
```

#### Property Value

 [BlendFactor](MicroEngine.Graphics.Devices.BlendFactor.md)

### <a id="MicroEngine_Graphics_Devices_BlendState_Presets"></a> Presets

The four presets. Every tier supports them with every built-in pipeline (they are the minimum of [BuiltInBlendStates](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_BuiltInBlendStates)).

```csharp
public static IReadOnlyList<BlendState> Presets { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[BlendState](MicroEngine.Graphics.Devices.BlendState.md)\>

### <a id="MicroEngine_Graphics_Devices_BlendState_SrcAlpha"></a> SrcAlpha

Factor applied to the source alpha.

```csharp
public BlendFactor SrcAlpha { get; init; }
```

#### Property Value

 [BlendFactor](MicroEngine.Graphics.Devices.BlendFactor.md)

### <a id="MicroEngine_Graphics_Devices_BlendState_SrcColor"></a> SrcColor

Factor applied to the source colour.

```csharp
public BlendFactor SrcColor { get; init; }
```

#### Property Value

 [BlendFactor](MicroEngine.Graphics.Devices.BlendFactor.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_BlendState__Clone__"></a> <Clone\>$\(\)

```csharp
public BlendState <Clone>$()
```

#### Returns

 [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

### <a id="MicroEngine_Graphics_Devices_BlendState_Deconstruct_MicroEngine_Graphics_Devices_BlendFactor__MicroEngine_Graphics_Devices_BlendFactor__MicroEngine_Graphics_Devices_BlendOp__MicroEngine_Graphics_Devices_BlendFactor__MicroEngine_Graphics_Devices_BlendFactor__MicroEngine_Graphics_Devices_BlendOp__"></a> Deconstruct\(out BlendFactor, out BlendFactor, out BlendOp, out BlendFactor, out BlendFactor, out BlendOp\)

```csharp
public void Deconstruct(out BlendFactor SrcColor, out BlendFactor DstColor, out BlendOp ColorOp, out BlendFactor SrcAlpha, out BlendFactor DstAlpha, out BlendOp AlphaOp)
```

#### Parameters

`SrcColor` [BlendFactor](MicroEngine.Graphics.Devices.BlendFactor.md)

`DstColor` [BlendFactor](MicroEngine.Graphics.Devices.BlendFactor.md)

`ColorOp` [BlendOp](MicroEngine.Graphics.Devices.BlendOp.md)

`SrcAlpha` [BlendFactor](MicroEngine.Graphics.Devices.BlendFactor.md)

`DstAlpha` [BlendFactor](MicroEngine.Graphics.Devices.BlendFactor.md)

`AlphaOp` [BlendOp](MicroEngine.Graphics.Devices.BlendOp.md)

### <a id="MicroEngine_Graphics_Devices_BlendState_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_BlendState_Equals_MicroEngine_Graphics_Devices_BlendState_"></a> Equals\(BlendState?\)

```csharp
public bool Equals(BlendState? other)
```

#### Parameters

`other` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_BlendState_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_BlendState_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_BlendState_op_Equality_MicroEngine_Graphics_Devices_BlendState_MicroEngine_Graphics_Devices_BlendState_"></a> operator ==\(BlendState?, BlendState?\)

```csharp
public static bool operator ==(BlendState? left, BlendState? right)
```

#### Parameters

`left` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)?

`right` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_BlendState_op_Inequality_MicroEngine_Graphics_Devices_BlendState_MicroEngine_Graphics_Devices_BlendState_"></a> operator \!=\(BlendState?, BlendState?\)

```csharp
public static bool operator !=(BlendState? left, BlendState? right)
```

#### Parameters

`left` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)?

`right` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
