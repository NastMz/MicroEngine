# Struct SamplerDescription

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Immutable sampler state, a value: [GetSampler](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_GetSampler_MicroEngine_Graphics_Devices_SamplerDescription__) returns a cached device object for it.
Sampling is never texture state. <code class="paramref">Min</code> different from <code class="paramref">Mag</code> requires
[SeparateFilters](MicroEngine.Graphics.Devices.GraphicsFeatures.md). <code class="paramref">Mip</code> applies only to textures with more than one
mip level and is otherwise ignored (so [LinearClamp](MicroEngine.Graphics.Devices.SamplerDescription.md#MicroEngine_Graphics_Devices_SamplerDescription_LinearClamp) works on every tier).

```csharp
public readonly struct SamplerDescription : IEquatable<SamplerDescription>
```

#### Implements

[IEquatable<SamplerDescription\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_SamplerDescription__ctor_MicroEngine_Graphics_Devices_Filter_MicroEngine_Graphics_Devices_Filter_MicroEngine_Graphics_Devices_Filter_MicroEngine_Graphics_Devices_AddressMode_MicroEngine_Graphics_Devices_AddressMode_"></a> SamplerDescription\(Filter, Filter, Filter, AddressMode, AddressMode\)

Immutable sampler state, a value: [GetSampler](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_GetSampler_MicroEngine_Graphics_Devices_SamplerDescription__) returns a cached device object for it.
Sampling is never texture state. <code class="paramref">Min</code> different from <code class="paramref">Mag</code> requires
[SeparateFilters](MicroEngine.Graphics.Devices.GraphicsFeatures.md). <code class="paramref">Mip</code> applies only to textures with more than one
mip level and is otherwise ignored (so [LinearClamp](MicroEngine.Graphics.Devices.SamplerDescription.md#MicroEngine_Graphics_Devices_SamplerDescription_LinearClamp) works on every tier).

```csharp
public SamplerDescription(Filter Min = Filter.Nearest, Filter Mag = Filter.Nearest, Filter Mip = Filter.Nearest, AddressMode U = AddressMode.Clamp, AddressMode V = AddressMode.Clamp)
```

#### Parameters

`Min` [Filter](MicroEngine.Graphics.Devices.Filter.md)

Minification filter.

`Mag` [Filter](MicroEngine.Graphics.Devices.Filter.md)

Magnification filter.

`Mip` [Filter](MicroEngine.Graphics.Devices.Filter.md)

Filter between mip levels.

`U` [AddressMode](MicroEngine.Graphics.Devices.AddressMode.md)

Horizontal address mode.

`V` [AddressMode](MicroEngine.Graphics.Devices.AddressMode.md)

Vertical address mode.

## Fields

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_LinearClamp"></a> LinearClamp

Linear, clamped.

```csharp
public static readonly SamplerDescription LinearClamp
```

#### Field Value

 [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_PointClamp"></a> PointClamp

Nearest, clamped: pixel art and exact copies.

```csharp
public static readonly SamplerDescription PointClamp
```

#### Field Value

 [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

## Properties

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_Mag"></a> Mag

Magnification filter.

```csharp
public Filter Mag { get; init; }
```

#### Property Value

 [Filter](MicroEngine.Graphics.Devices.Filter.md)

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_Min"></a> Min

Minification filter.

```csharp
public Filter Min { get; init; }
```

#### Property Value

 [Filter](MicroEngine.Graphics.Devices.Filter.md)

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_Mip"></a> Mip

Filter between mip levels.

```csharp
public Filter Mip { get; init; }
```

#### Property Value

 [Filter](MicroEngine.Graphics.Devices.Filter.md)

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_U"></a> U

Horizontal address mode.

```csharp
public AddressMode U { get; init; }
```

#### Property Value

 [AddressMode](MicroEngine.Graphics.Devices.AddressMode.md)

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_V"></a> V

Vertical address mode.

```csharp
public AddressMode V { get; init; }
```

#### Property Value

 [AddressMode](MicroEngine.Graphics.Devices.AddressMode.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_Deconstruct_MicroEngine_Graphics_Devices_Filter__MicroEngine_Graphics_Devices_Filter__MicroEngine_Graphics_Devices_Filter__MicroEngine_Graphics_Devices_AddressMode__MicroEngine_Graphics_Devices_AddressMode__"></a> Deconstruct\(out Filter, out Filter, out Filter, out AddressMode, out AddressMode\)

```csharp
public void Deconstruct(out Filter Min, out Filter Mag, out Filter Mip, out AddressMode U, out AddressMode V)
```

#### Parameters

`Min` [Filter](MicroEngine.Graphics.Devices.Filter.md)

`Mag` [Filter](MicroEngine.Graphics.Devices.Filter.md)

`Mip` [Filter](MicroEngine.Graphics.Devices.Filter.md)

`U` [AddressMode](MicroEngine.Graphics.Devices.AddressMode.md)

`V` [AddressMode](MicroEngine.Graphics.Devices.AddressMode.md)

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_Equals_MicroEngine_Graphics_Devices_SamplerDescription_"></a> Equals\(SamplerDescription\)

```csharp
public bool Equals(SamplerDescription other)
```

#### Parameters

`other` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_op_Equality_MicroEngine_Graphics_Devices_SamplerDescription_MicroEngine_Graphics_Devices_SamplerDescription_"></a> operator ==\(SamplerDescription, SamplerDescription\)

```csharp
public static bool operator ==(SamplerDescription left, SamplerDescription right)
```

#### Parameters

`left` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

`right` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_SamplerDescription_op_Inequality_MicroEngine_Graphics_Devices_SamplerDescription_MicroEngine_Graphics_Devices_SamplerDescription_"></a> operator \!=\(SamplerDescription, SamplerDescription\)

```csharp
public static bool operator !=(SamplerDescription left, SamplerDescription right)
```

#### Parameters

`left` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

`right` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
