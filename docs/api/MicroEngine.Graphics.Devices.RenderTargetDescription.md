# Struct RenderTargetDescription

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

An offscreen target: one colour texture that can also be sampled, and an optional depth(-stencil) texture.

```csharp
public readonly struct RenderTargetDescription : IEquatable<RenderTargetDescription>
```

#### Implements

[IEquatable<RenderTargetDescription\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_RenderTargetDescription__ctor_System_Int32_System_Int32_MicroEngine_Graphics_Devices_TextureFormat_System_Nullable_MicroEngine_Graphics_Devices_TextureFormat__System_String_"></a> RenderTargetDescription\(int, int, TextureFormat, TextureFormat?, string?\)

An offscreen target: one colour texture that can also be sampled, and an optional depth(-stencil) texture.

```csharp
public RenderTargetDescription(int Width, int Height, TextureFormat Format = TextureFormat.Rgba8Unorm, TextureFormat? Depth = null, string? Name = null)
```

#### Parameters

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Width in pixels, 1..[MaxRenderTargetSize](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxRenderTargetSize).

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Height in pixels, 1..[MaxRenderTargetSize](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxRenderTargetSize).

`Format` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

Colour format; must support [RenderTarget](MicroEngine.Graphics.Devices.TextureUsage.md) and [Sampled](MicroEngine.Graphics.Devices.TextureUsage.md).

`Depth` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)?

Depth format, or null; requires [DepthBuffer](MicroEngine.Graphics.Devices.GraphicsFeatures.md) and [DepthStencil](MicroEngine.Graphics.Devices.TextureUsage.md) support for the format.

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Debug name.

## Properties

### <a id="MicroEngine_Graphics_Devices_RenderTargetDescription_Depth"></a> Depth

Depth format, or null; requires [DepthBuffer](MicroEngine.Graphics.Devices.GraphicsFeatures.md) and [DepthStencil](MicroEngine.Graphics.Devices.TextureUsage.md) support for the format.

```csharp
public TextureFormat? Depth { get; init; }
```

#### Property Value

 [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)?

### <a id="MicroEngine_Graphics_Devices_RenderTargetDescription_Format"></a> Format

Colour format; must support [RenderTarget](MicroEngine.Graphics.Devices.TextureUsage.md) and [Sampled](MicroEngine.Graphics.Devices.TextureUsage.md).

```csharp
public TextureFormat Format { get; init; }
```

#### Property Value

 [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

### <a id="MicroEngine_Graphics_Devices_RenderTargetDescription_Height"></a> Height

Height in pixels, 1..[MaxRenderTargetSize](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxRenderTargetSize).

```csharp
public int Height { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_RenderTargetDescription_Name"></a> Name

Debug name.

```csharp
public string? Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_Devices_RenderTargetDescription_Width"></a> Width

Width in pixels, 1..[MaxRenderTargetSize](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxRenderTargetSize).

```csharp
public int Width { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Devices_RenderTargetDescription_Deconstruct_System_Int32__System_Int32__MicroEngine_Graphics_Devices_TextureFormat__System_Nullable_MicroEngine_Graphics_Devices_TextureFormat___System_String__"></a> Deconstruct\(out int, out int, out TextureFormat, out TextureFormat?, out string?\)

```csharp
public void Deconstruct(out int Width, out int Height, out TextureFormat Format, out TextureFormat? Depth, out string? Name)
```

#### Parameters

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Format` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

`Depth` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)?

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_Devices_RenderTargetDescription_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_RenderTargetDescription_Equals_MicroEngine_Graphics_Devices_RenderTargetDescription_"></a> Equals\(RenderTargetDescription\)

```csharp
public bool Equals(RenderTargetDescription other)
```

#### Parameters

`other` [RenderTargetDescription](MicroEngine.Graphics.Devices.RenderTargetDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_RenderTargetDescription_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_RenderTargetDescription_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_RenderTargetDescription_op_Equality_MicroEngine_Graphics_Devices_RenderTargetDescription_MicroEngine_Graphics_Devices_RenderTargetDescription_"></a> operator ==\(RenderTargetDescription, RenderTargetDescription\)

```csharp
public static bool operator ==(RenderTargetDescription left, RenderTargetDescription right)
```

#### Parameters

`left` [RenderTargetDescription](MicroEngine.Graphics.Devices.RenderTargetDescription.md)

`right` [RenderTargetDescription](MicroEngine.Graphics.Devices.RenderTargetDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_RenderTargetDescription_op_Inequality_MicroEngine_Graphics_Devices_RenderTargetDescription_MicroEngine_Graphics_Devices_RenderTargetDescription_"></a> operator \!=\(RenderTargetDescription, RenderTargetDescription\)

```csharp
public static bool operator !=(RenderTargetDescription left, RenderTargetDescription right)
```

#### Parameters

`left` [RenderTargetDescription](MicroEngine.Graphics.Devices.RenderTargetDescription.md)

`right` [RenderTargetDescription](MicroEngine.Graphics.Devices.RenderTargetDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
