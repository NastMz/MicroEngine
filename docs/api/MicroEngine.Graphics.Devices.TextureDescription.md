# Struct TextureDescription

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A sampled texture. Texture data uploads go through [CreateTexture](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_Devices_TextureDescription__System_ReadOnlySpan_System_Byte__) and [UpdateTexture](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_UpdateTexture_MicroEngine_Graphics_Devices_Texture_MicroEngine_Graphics_Devices_RectI__System_ReadOnlySpan_System_Byte__).

```csharp
public readonly struct TextureDescription : IEquatable<TextureDescription>
```

#### Implements

[IEquatable<TextureDescription\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_Devices_TextureDescription__ctor_System_Int32_System_Int32_MicroEngine_Graphics_Devices_TextureFormat_System_Int32_MicroEngine_Graphics_Devices_AlphaMode_System_String_"></a> TextureDescription\(int, int, TextureFormat, int, AlphaMode, string?\)

A sampled texture. Texture data uploads go through [CreateTexture](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_Devices_TextureDescription__System_ReadOnlySpan_System_Byte__) and [UpdateTexture](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_UpdateTexture_MicroEngine_Graphics_Devices_Texture_MicroEngine_Graphics_Devices_RectI__System_ReadOnlySpan_System_Byte__).

```csharp
public TextureDescription(int Width, int Height, TextureFormat Format = TextureFormat.Rgba8Unorm, int MipLevels = 1, AlphaMode Alpha = AlphaMode.Premultiplied, string? Name = null)
```

#### Parameters

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Width in texels, 1..[MaxTextureSize](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxTextureSize).

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Height in texels, 1..[MaxTextureSize](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxTextureSize).

`Format` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

Texel format; must support [Sampled](MicroEngine.Graphics.Devices.TextureUsage.md).

`MipLevels` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Mip levels; more than 1 requires [Mipmaps](MicroEngine.Graphics.Devices.GraphicsFeatures.md).

`Alpha` [AlphaMode](MicroEngine.Graphics.Devices.AlphaMode.md)

Whether the data is premultiplied (default) or straight; informational for the device.

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Debug name.

## Properties

### <a id="MicroEngine_Graphics_Devices_TextureDescription_Alpha"></a> Alpha

Whether the data is premultiplied (default) or straight; informational for the device.

```csharp
public AlphaMode Alpha { get; init; }
```

#### Property Value

 [AlphaMode](MicroEngine.Graphics.Devices.AlphaMode.md)

### <a id="MicroEngine_Graphics_Devices_TextureDescription_Format"></a> Format

Texel format; must support [Sampled](MicroEngine.Graphics.Devices.TextureUsage.md).

```csharp
public TextureFormat Format { get; init; }
```

#### Property Value

 [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

### <a id="MicroEngine_Graphics_Devices_TextureDescription_Height"></a> Height

Height in texels, 1..[MaxTextureSize](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxTextureSize).

```csharp
public int Height { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_TextureDescription_MipLevels"></a> MipLevels

Mip levels; more than 1 requires [Mipmaps](MicroEngine.Graphics.Devices.GraphicsFeatures.md).

```csharp
public int MipLevels { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_TextureDescription_Name"></a> Name

Debug name.

```csharp
public string? Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_Devices_TextureDescription_Width"></a> Width

Width in texels, 1..[MaxTextureSize](MicroEngine.Graphics.Devices.GraphicsLimits.md#MicroEngine_Graphics_Devices_GraphicsLimits_MaxTextureSize).

```csharp
public int Width { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Devices_TextureDescription_Deconstruct_System_Int32__System_Int32__MicroEngine_Graphics_Devices_TextureFormat__System_Int32__MicroEngine_Graphics_Devices_AlphaMode__System_String__"></a> Deconstruct\(out int, out int, out TextureFormat, out int, out AlphaMode, out string?\)

```csharp
public void Deconstruct(out int Width, out int Height, out TextureFormat Format, out int MipLevels, out AlphaMode Alpha, out string? Name)
```

#### Parameters

`Width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Format` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

`MipLevels` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Alpha` [AlphaMode](MicroEngine.Graphics.Devices.AlphaMode.md)

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_Devices_TextureDescription_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_TextureDescription_Equals_MicroEngine_Graphics_Devices_TextureDescription_"></a> Equals\(TextureDescription\)

```csharp
public bool Equals(TextureDescription other)
```

#### Parameters

`other` [TextureDescription](MicroEngine.Graphics.Devices.TextureDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_TextureDescription_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_TextureDescription_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_Devices_TextureDescription_op_Equality_MicroEngine_Graphics_Devices_TextureDescription_MicroEngine_Graphics_Devices_TextureDescription_"></a> operator ==\(TextureDescription, TextureDescription\)

```csharp
public static bool operator ==(TextureDescription left, TextureDescription right)
```

#### Parameters

`left` [TextureDescription](MicroEngine.Graphics.Devices.TextureDescription.md)

`right` [TextureDescription](MicroEngine.Graphics.Devices.TextureDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_TextureDescription_op_Inequality_MicroEngine_Graphics_Devices_TextureDescription_MicroEngine_Graphics_Devices_TextureDescription_"></a> operator \!=\(TextureDescription, TextureDescription\)

```csharp
public static bool operator !=(TextureDescription left, TextureDescription right)
```

#### Parameters

`left` [TextureDescription](MicroEngine.Graphics.Devices.TextureDescription.md)

`right` [TextureDescription](MicroEngine.Graphics.Devices.TextureDescription.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
