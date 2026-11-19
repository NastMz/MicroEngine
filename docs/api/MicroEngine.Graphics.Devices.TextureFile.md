# Class TextureFile

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A texture stored as the device takes it, in a KTX 2.0 file (the Khronos texture container, <code>.ktx2</code>): its size,
its format and the bytes of each mip level, ready to upload, with nothing to decode. It is what
<code>microengine textures</code> writes from image files, and what other tools that write KTX 2.0 produce; a game may
ship it beside or instead of an image, and nothing requires it. [Create](MicroEngine.Graphics.Devices.TextureFile.md#MicroEngine_Graphics_Devices_TextureFile_Create_MicroEngine_Graphics_Devices_IGraphicsDevice_System_String_) makes the texture on a
device; a device that does not sample the file's format (block-compressed formats are a capability, see
[Supports](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_Supports_MicroEngine_Graphics_Devices_TextureFormat_MicroEngine_Graphics_Devices_TextureUsage_)) gets the typed [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md), as for any
undeclared capability, so a game can keep its image file for those devices.

```csharp
public sealed class TextureFile
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TextureFile](MicroEngine.Graphics.Devices.TextureFile.md)

## Remarks

<p>Read: one 2D texture (no array, cube or 3D texture) in one of [Rgba8Unorm](MicroEngine.Graphics.Devices.TextureFormat.md),
[Bc7](MicroEngine.Graphics.Devices.TextureFormat.md), [Etc2Rgba8](MicroEngine.Graphics.Devices.TextureFormat.md) or [Astc4x4](MicroEngine.Graphics.Devices.TextureFormat.md), its
levels stored plainly or deflated (KTX's ZLIB supercompression). A format tagged sRGB is read as its plain format:
the engine uses a texture's bytes as it uses an image file's, without a transfer function. Zstandard and Basis
Universal (BasisLZ, UASTC) files are refused with a message: they need a decompressor or a transcoder the engine
does not carry.</p>
<p>Written: [Rgba8Unorm](MicroEngine.Graphics.Devices.TextureFormat.md) and [Bc7](MicroEngine.Graphics.Devices.TextureFormat.md), with the data format
descriptor of each, the premultiplied-alpha flag, and a <code>KTXwriter</code> entry.</p>

## Constructors

### <a id="MicroEngine_Graphics_Devices_TextureFile__ctor_System_Int32_System_Int32_MicroEngine_Graphics_Devices_TextureFormat_System_Collections_Generic_IReadOnlyList_System_ReadOnlyMemory_System_Byte___MicroEngine_Graphics_Devices_AlphaMode_"></a> TextureFile\(int, int, TextureFormat, IReadOnlyList<ReadOnlyMemory<byte\>\>, AlphaMode\)

Creates a texture file from the bytes of its levels.

```csharp
public TextureFile(int width, int height, TextureFormat format, IReadOnlyList<ReadOnlyMemory<byte>> levels, AlphaMode alpha = AlphaMode.Premultiplied)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Width of level 0 in texels.

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Height of level 0 in texels.

`format` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

Texel format; not a depth format.

`levels` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[ReadOnlyMemory](https://learn.microsoft.com/dotnet/api/system.readonlymemory\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>\>

Level 0 first, each half the size of the one before (at least 1), tightly packed: [ByteCount](MicroEngine.Graphics.Devices.TextureFormats.md#MicroEngine_Graphics_Devices_TextureFormats_ByteCount_MicroEngine_Graphics_Devices_TextureFormat_System_Int32_System_Int32_) bytes each.

`alpha` [AlphaMode](MicroEngine.Graphics.Devices.AlphaMode.md)

Whether colour is already multiplied by alpha (what the device draws) or straight.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A size that is not positive.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A depth format, no level, more levels than the size has, or a level whose length does not match its size.

## Fields

### <a id="MicroEngine_Graphics_Devices_TextureFile_FileExtension"></a> FileExtension

The file extension of a KTX 2.0 texture.

```csharp
public const string FileExtension = ".ktx2"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Graphics_Devices_TextureFile_Alpha"></a> Alpha

Whether the colour channels are already multiplied by alpha.

```csharp
public AlphaMode Alpha { get; }
```

#### Property Value

 [AlphaMode](MicroEngine.Graphics.Devices.AlphaMode.md)

### <a id="MicroEngine_Graphics_Devices_TextureFile_Format"></a> Format

Texel format.

```csharp
public TextureFormat Format { get; }
```

#### Property Value

 [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

### <a id="MicroEngine_Graphics_Devices_TextureFile_Height"></a> Height

Height of level 0 in texels.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_TextureFile_Levels"></a> Levels

Number of mip levels stored.

```csharp
public int Levels { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_TextureFile_Width"></a> Width

Width of level 0 in texels.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Devices_TextureFile_CanCreate_MicroEngine_Graphics_Devices_GraphicsCapabilities_"></a> CanCreate\(GraphicsCapabilities\)

Whether a device with <code class="paramref">capabilities</code> samples this file's format, so [Create](MicroEngine.Graphics.Devices.TextureFile.md#MicroEngine_Graphics_Devices_TextureFile_Create_MicroEngine_Graphics_Devices_IGraphicsDevice_System_String_) succeeds on it.

```csharp
public bool CanCreate(GraphicsCapabilities capabilities)
```

#### Parameters

`capabilities` [GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_TextureFile_Create_MicroEngine_Graphics_Devices_IGraphicsDevice_System_String_"></a> Create\(IGraphicsDevice, string?\)

Creates the texture on <code class="paramref">device</code> from level 0, which the caller owns and disposes. Further
levels of the file are not uploaded (no device declares [Mipmaps](MicroEngine.Graphics.Devices.GraphicsFeatures.md) yet). Straight
[Rgba8Unorm](MicroEngine.Graphics.Devices.TextureFormat.md) data is premultiplied on the way, as an image upload is; a
block-compressed file with straight alpha is created as it is, with [Straight](MicroEngine.Graphics.Devices.AlphaMode.md) in its
description, because blocks cannot be converted without decoding them.

```csharp
public Texture Create(IGraphicsDevice device, string? name = null)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

The device the texture lives on.

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Debug name.

#### Returns

 [Texture](MicroEngine.Graphics.Devices.Texture.md)

#### Exceptions

 [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md)

The device does not sample [Format](MicroEngine.Graphics.Devices.TextureFile.md#MicroEngine_Graphics_Devices_TextureFile_Format).

### <a id="MicroEngine_Graphics_Devices_TextureFile_Level_System_Int32_"></a> Level\(int\)

The bytes of a level as the device takes them (level 0 is the whole texture).

```csharp
public ReadOnlyMemory<byte> Level(int level)
```

#### Parameters

`level` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [ReadOnlyMemory](https://learn.microsoft.com/dotnet/api/system.readonlymemory\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">level</code> is not a stored level.

### <a id="MicroEngine_Graphics_Devices_TextureFile_Parse_System_ReadOnlySpan_System_Byte__"></a> Parse\(ReadOnlySpan<byte\>\)

Reads a KTX 2.0 file.

```csharp
public static TextureFile Parse(ReadOnlySpan<byte> data)
```

#### Parameters

`data` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

#### Returns

 [TextureFile](MicroEngine.Graphics.Devices.TextureFile.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The data is not a KTX 2.0 file, is truncated or inconsistent, or holds what this type does not read (see the remarks of [TextureFile](MicroEngine.Graphics.Devices.TextureFile.md)); the message says which.

### <a id="MicroEngine_Graphics_Devices_TextureFile_SizeOf_System_Int32_"></a> SizeOf\(int\)

The size in texels of a level: each is half the one before, never under 1.

```csharp
public (int Width, int Height) SizeOf(int level)
```

#### Parameters

`level` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

### <a id="MicroEngine_Graphics_Devices_TextureFile_ToBytes_System_Boolean_"></a> ToBytes\(bool\)

The file form of this texture: a KTX 2.0 file.

```csharp
public byte[] ToBytes(bool deflate = true)
```

#### Parameters

`deflate` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True (the default) stores each level deflated (KTX's ZLIB supercompression), which most textures shrink well under; false stores them plainly, ready to be read without inflating.

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

#### Exceptions

 [NotSupportedException](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)

[Format](MicroEngine.Graphics.Devices.TextureFile.md#MicroEngine_Graphics_Devices_TextureFile_Format) is not one this type writes ([Rgba8Unorm](MicroEngine.Graphics.Devices.TextureFormat.md), [Bc7](MicroEngine.Graphics.Devices.TextureFormat.md)).

### <a id="MicroEngine_Graphics_Devices_TextureFile_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
