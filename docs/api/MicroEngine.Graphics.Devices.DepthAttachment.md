# Struct DepthAttachment

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

The depth attachment of a pass: the depth texture of a target created with [Depth](MicroEngine.Graphics.Devices.RenderTargetDescription.md#MicroEngine_Graphics_Devices_RenderTargetDescription_Depth).

```csharp
public readonly struct DepthAttachment : IEquatable<DepthAttachment>
```

#### Implements

[IEquatable<DepthAttachment\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_DepthAttachment__ctor_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_LoadOp_MicroEngine_Graphics_Devices_StoreOp_System_Single_"></a> DepthAttachment\(RenderTarget, LoadOp, StoreOp, float\)

The depth attachment of a pass: the depth texture of a target created with [Depth](MicroEngine.Graphics.Devices.RenderTargetDescription.md#MicroEngine_Graphics_Devices_RenderTargetDescription_Depth).

```csharp
public DepthAttachment(RenderTarget Target, LoadOp Load = LoadOp.Clear, StoreOp Store = StoreOp.DontCare, float ClearDepth = 1)
```

#### Parameters

`Target` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

The target owning the depth texture (same size as the colour attachment).

`Load` [LoadOp](MicroEngine.Graphics.Devices.LoadOp.md)

Load operation.

`Store` [StoreOp](MicroEngine.Graphics.Devices.StoreOp.md)

Store operation.

`ClearDepth` [float](https://learn.microsoft.com/dotnet/api/system.single)

Clear depth when <code class="paramref">Load</code> is [Clear](MicroEngine.Graphics.Devices.LoadOp.md) (0..1).

## Properties

### <a id="MicroEngine_Graphics_Devices_DepthAttachment_ClearDepth"></a> ClearDepth

Clear depth when <code class="paramref">Load</code> is [Clear](MicroEngine.Graphics.Devices.LoadOp.md) (0..1).

```csharp
public float ClearDepth { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Devices_DepthAttachment_Load"></a> Load

Load operation.

```csharp
public LoadOp Load { get; init; }
```

#### Property Value

 [LoadOp](MicroEngine.Graphics.Devices.LoadOp.md)

### <a id="MicroEngine_Graphics_Devices_DepthAttachment_Store"></a> Store

Store operation.

```csharp
public StoreOp Store { get; init; }
```

#### Property Value

 [StoreOp](MicroEngine.Graphics.Devices.StoreOp.md)

### <a id="MicroEngine_Graphics_Devices_DepthAttachment_Target"></a> Target

The target owning the depth texture (same size as the colour attachment).

```csharp
public RenderTarget Target { get; init; }
```

#### Property Value

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_DepthAttachment_Deconstruct_MicroEngine_Graphics_Devices_RenderTarget__MicroEngine_Graphics_Devices_LoadOp__MicroEngine_Graphics_Devices_StoreOp__System_Single__"></a> Deconstruct\(out RenderTarget, out LoadOp, out StoreOp, out float\)

```csharp
public void Deconstruct(out RenderTarget Target, out LoadOp Load, out StoreOp Store, out float ClearDepth)
```

#### Parameters

`Target` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

`Load` [LoadOp](MicroEngine.Graphics.Devices.LoadOp.md)

`Store` [StoreOp](MicroEngine.Graphics.Devices.StoreOp.md)

`ClearDepth` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_Devices_DepthAttachment_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_DepthAttachment_Equals_MicroEngine_Graphics_Devices_DepthAttachment_"></a> Equals\(DepthAttachment\)

```csharp
public bool Equals(DepthAttachment other)
```

#### Parameters

`other` [DepthAttachment](MicroEngine.Graphics.Devices.DepthAttachment.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_DepthAttachment_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_DepthAttachment_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_DepthAttachment_op_Equality_MicroEngine_Graphics_Devices_DepthAttachment_MicroEngine_Graphics_Devices_DepthAttachment_"></a> operator ==\(DepthAttachment, DepthAttachment\)

```csharp
public static bool operator ==(DepthAttachment left, DepthAttachment right)
```

#### Parameters

`left` [DepthAttachment](MicroEngine.Graphics.Devices.DepthAttachment.md)

`right` [DepthAttachment](MicroEngine.Graphics.Devices.DepthAttachment.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_DepthAttachment_op_Inequality_MicroEngine_Graphics_Devices_DepthAttachment_MicroEngine_Graphics_Devices_DepthAttachment_"></a> operator \!=\(DepthAttachment, DepthAttachment\)

```csharp
public static bool operator !=(DepthAttachment left, DepthAttachment right)
```

#### Parameters

`left` [DepthAttachment](MicroEngine.Graphics.Devices.DepthAttachment.md)

`right` [DepthAttachment](MicroEngine.Graphics.Devices.DepthAttachment.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
