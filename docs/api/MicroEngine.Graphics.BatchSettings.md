# Struct BatchSettings

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

The settings of one batch, from [Begin](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Begin_MicroEngine_Graphics_BatchSettings__) to [End](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_End).

```csharp
public readonly struct BatchSettings : IEquatable<BatchSettings>
```

#### Implements

[IEquatable<BatchSettings\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_BatchSettings__ctor_MicroEngine_Graphics_Devices_RenderTarget_System_Nullable_System_Numerics_Matrix3x2__MicroEngine_Graphics_Devices_BlendState_System_Nullable_MicroEngine_Graphics_Devices_SamplerDescription__MicroEngine_Graphics_SortMode_System_Nullable_MicroEngine_Graphics_Devices_RectI__System_Nullable_MicroEngine_ColorF__System_Nullable_MicroEngine_Graphics_Devices_RectI__"></a> BatchSettings\(RenderTarget, Matrix3x2?, BlendState?, SamplerDescription?, SortMode, RectI?, ColorF?, RectI?\)

The settings of one batch, from [Begin](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Begin_MicroEngine_Graphics_BatchSettings__) to [End](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_End).

```csharp
public BatchSettings(RenderTarget Target, Matrix3x2? View = null, BlendState? Blend = null, SamplerDescription? Sampler = null, SortMode Sort = SortMode.Deferred, RectI? Scissor = null, ColorF? Clear = null, RectI? Viewport = null)
```

#### Parameters

`Target` RenderTarget

Where the batch renders: an offscreen target or the surface target acquired this frame.

`View` [Matrix3x2](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2)?

Applied after every draw's transform (a camera's <code>WorldToView</code>); null is the identity.

`Blend` BlendState?

The batch's blend state; null is [AlphaPremultiplied](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_AlphaPremultiplied) (premultiplied colours).

`Sampler` SamplerDescription?

The batch's sampler; null is [PointClamp](MicroEngine.Graphics.Devices.SamplerDescription.md#MicroEngine_Graphics_Devices_SamplerDescription_PointClamp).

`Sort` [SortMode](MicroEngine.Graphics.SortMode.md)

The order draws reach the device.

`Scissor` RectI?

The batch's scissor in target pixels; null draws on the whole target.

`Clear` ColorF?

A premultiplied colour the target is cleared to first; null keeps its contents.

`Viewport` RectI?

The viewport in target pixels; null is the whole target.

## Properties

### <a id="MicroEngine_Graphics_BatchSettings_Blend"></a> Blend

The batch's blend state; null is [AlphaPremultiplied](MicroEngine.Graphics.Devices.BlendState.md#MicroEngine_Graphics_Devices_BlendState_AlphaPremultiplied) (premultiplied colours).

```csharp
public BlendState? Blend { get; init; }
```

#### Property Value

 BlendState?

### <a id="MicroEngine_Graphics_BatchSettings_Clear"></a> Clear

A premultiplied colour the target is cleared to first; null keeps its contents.

```csharp
public ColorF? Clear { get; init; }
```

#### Property Value

 ColorF?

### <a id="MicroEngine_Graphics_BatchSettings_Sampler"></a> Sampler

The batch's sampler; null is [PointClamp](MicroEngine.Graphics.Devices.SamplerDescription.md#MicroEngine_Graphics_Devices_SamplerDescription_PointClamp).

```csharp
public SamplerDescription? Sampler { get; init; }
```

#### Property Value

 SamplerDescription?

### <a id="MicroEngine_Graphics_BatchSettings_Scissor"></a> Scissor

The batch's scissor in target pixels; null draws on the whole target.

```csharp
public RectI? Scissor { get; init; }
```

#### Property Value

 RectI?

### <a id="MicroEngine_Graphics_BatchSettings_Sort"></a> Sort

The order draws reach the device.

```csharp
public SortMode Sort { get; init; }
```

#### Property Value

 [SortMode](MicroEngine.Graphics.SortMode.md)

### <a id="MicroEngine_Graphics_BatchSettings_Target"></a> Target

Where the batch renders: an offscreen target or the surface target acquired this frame.

```csharp
public RenderTarget Target { get; init; }
```

#### Property Value

 RenderTarget

### <a id="MicroEngine_Graphics_BatchSettings_View"></a> View

Applied after every draw's transform (a camera's <code>WorldToView</code>); null is the identity.

```csharp
public Matrix3x2? View { get; init; }
```

#### Property Value

 [Matrix3x2](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2)?

### <a id="MicroEngine_Graphics_BatchSettings_Viewport"></a> Viewport

The viewport in target pixels; null is the whole target.

```csharp
public RectI? Viewport { get; init; }
```

#### Property Value

 RectI?

## Methods

### <a id="MicroEngine_Graphics_BatchSettings_Deconstruct_MicroEngine_Graphics_Devices_RenderTarget__System_Nullable_System_Numerics_Matrix3x2___MicroEngine_Graphics_Devices_BlendState__System_Nullable_MicroEngine_Graphics_Devices_SamplerDescription___MicroEngine_Graphics_SortMode__System_Nullable_MicroEngine_Graphics_Devices_RectI___System_Nullable_MicroEngine_ColorF___System_Nullable_MicroEngine_Graphics_Devices_RectI___"></a> Deconstruct\(out RenderTarget, out Matrix3x2?, out BlendState?, out SamplerDescription?, out SortMode, out RectI?, out ColorF?, out RectI?\)

```csharp
public void Deconstruct(out RenderTarget Target, out Matrix3x2? View, out BlendState? Blend, out SamplerDescription? Sampler, out SortMode Sort, out RectI? Scissor, out ColorF? Clear, out RectI? Viewport)
```

#### Parameters

`Target` RenderTarget

`View` [Matrix3x2](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2)?

`Blend` BlendState?

`Sampler` SamplerDescription?

`Sort` [SortMode](MicroEngine.Graphics.SortMode.md)

`Scissor` RectI?

`Clear` ColorF?

`Viewport` RectI?

### <a id="MicroEngine_Graphics_BatchSettings_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_BatchSettings_Equals_MicroEngine_Graphics_BatchSettings_"></a> Equals\(BatchSettings\)

```csharp
public bool Equals(BatchSettings other)
```

#### Parameters

`other` [BatchSettings](MicroEngine.Graphics.BatchSettings.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_BatchSettings_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_BatchSettings_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_BatchSettings_op_Equality_MicroEngine_Graphics_BatchSettings_MicroEngine_Graphics_BatchSettings_"></a> operator ==\(BatchSettings, BatchSettings\)

```csharp
public static bool operator ==(BatchSettings left, BatchSettings right)
```

#### Parameters

`left` [BatchSettings](MicroEngine.Graphics.BatchSettings.md)

`right` [BatchSettings](MicroEngine.Graphics.BatchSettings.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_BatchSettings_op_Inequality_MicroEngine_Graphics_BatchSettings_MicroEngine_Graphics_BatchSettings_"></a> operator \!=\(BatchSettings, BatchSettings\)

```csharp
public static bool operator !=(BatchSettings left, BatchSettings right)
```

#### Parameters

`left` [BatchSettings](MicroEngine.Graphics.BatchSettings.md)

`right` [BatchSettings](MicroEngine.Graphics.BatchSettings.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
