# Struct ColorAttachment

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

The colour attachment of a pass.

```csharp
public readonly struct ColorAttachment : IEquatable<ColorAttachment>
```

#### Implements

[IEquatable<ColorAttachment\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_ColorAttachment__ctor_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_LoadOp_MicroEngine_Graphics_Devices_StoreOp_MicroEngine_ColorF_"></a> ColorAttachment\(RenderTarget, LoadOp, StoreOp, ColorF\)

The colour attachment of a pass.

```csharp
public ColorAttachment(RenderTarget Target, LoadOp Load = LoadOp.Load, StoreOp Store = StoreOp.Store, ColorF Clear = default)
```

#### Parameters

`Target` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

The target; its [ColorFormat](MicroEngine.Graphics.Devices.RenderTarget.md#MicroEngine_Graphics_Devices_RenderTarget_ColorFormat) must match the bound pipeline.

`Load` [LoadOp](MicroEngine.Graphics.Devices.LoadOp.md)

Load operation.

`Store` [StoreOp](MicroEngine.Graphics.Devices.StoreOp.md)

Store operation.

`Clear` [ColorF](MicroEngine.ColorF.md)

Clear value when <code class="paramref">Load</code> is [Clear](MicroEngine.Graphics.Devices.LoadOp.md) (premultiplied).

## Properties

### <a id="MicroEngine_Graphics_Devices_ColorAttachment_Clear"></a> Clear

Clear value when <code class="paramref">Load</code> is [Clear](MicroEngine.Graphics.Devices.LoadOp.md) (premultiplied).

```csharp
public ColorF Clear { get; init; }
```

#### Property Value

 [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_Graphics_Devices_ColorAttachment_Load"></a> Load

Load operation.

```csharp
public LoadOp Load { get; init; }
```

#### Property Value

 [LoadOp](MicroEngine.Graphics.Devices.LoadOp.md)

### <a id="MicroEngine_Graphics_Devices_ColorAttachment_Store"></a> Store

Store operation.

```csharp
public StoreOp Store { get; init; }
```

#### Property Value

 [StoreOp](MicroEngine.Graphics.Devices.StoreOp.md)

### <a id="MicroEngine_Graphics_Devices_ColorAttachment_Target"></a> Target

The target; its [ColorFormat](MicroEngine.Graphics.Devices.RenderTarget.md#MicroEngine_Graphics_Devices_RenderTarget_ColorFormat) must match the bound pipeline.

```csharp
public RenderTarget Target { get; init; }
```

#### Property Value

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_ColorAttachment_Deconstruct_MicroEngine_Graphics_Devices_RenderTarget__MicroEngine_Graphics_Devices_LoadOp__MicroEngine_Graphics_Devices_StoreOp__MicroEngine_ColorF__"></a> Deconstruct\(out RenderTarget, out LoadOp, out StoreOp, out ColorF\)

```csharp
public void Deconstruct(out RenderTarget Target, out LoadOp Load, out StoreOp Store, out ColorF Clear)
```

#### Parameters

`Target` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

`Load` [LoadOp](MicroEngine.Graphics.Devices.LoadOp.md)

`Store` [StoreOp](MicroEngine.Graphics.Devices.StoreOp.md)

`Clear` [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_Graphics_Devices_ColorAttachment_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_ColorAttachment_Equals_MicroEngine_Graphics_Devices_ColorAttachment_"></a> Equals\(ColorAttachment\)

```csharp
public bool Equals(ColorAttachment other)
```

#### Parameters

`other` [ColorAttachment](MicroEngine.Graphics.Devices.ColorAttachment.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_ColorAttachment_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_ColorAttachment_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_ColorAttachment_op_Equality_MicroEngine_Graphics_Devices_ColorAttachment_MicroEngine_Graphics_Devices_ColorAttachment_"></a> operator ==\(ColorAttachment, ColorAttachment\)

```csharp
public static bool operator ==(ColorAttachment left, ColorAttachment right)
```

#### Parameters

`left` [ColorAttachment](MicroEngine.Graphics.Devices.ColorAttachment.md)

`right` [ColorAttachment](MicroEngine.Graphics.Devices.ColorAttachment.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_ColorAttachment_op_Inequality_MicroEngine_Graphics_Devices_ColorAttachment_MicroEngine_Graphics_Devices_ColorAttachment_"></a> operator \!=\(ColorAttachment, ColorAttachment\)

```csharp
public static bool operator !=(ColorAttachment left, ColorAttachment right)
```

#### Parameters

`left` [ColorAttachment](MicroEngine.Graphics.Devices.ColorAttachment.md)

`right` [ColorAttachment](MicroEngine.Graphics.Devices.ColorAttachment.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
