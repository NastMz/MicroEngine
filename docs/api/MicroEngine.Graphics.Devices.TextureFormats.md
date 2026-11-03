# Class TextureFormats

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Byte-size rules of texture formats, shared by validation and backends.

```csharp
public static class TextureFormats
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TextureFormats](MicroEngine.Graphics.Devices.TextureFormats.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_TextureFormats_ByteCount_MicroEngine_Graphics_Devices_TextureFormat_System_Int32_System_Int32_"></a> ByteCount\(TextureFormat, int, int\)

Bytes of tightly packed data for a <code class="paramref">width</code>×<code class="paramref">height</code> region (rows top to bottom,
no padding; compressed formats count whole 4×4 blocks). Depth formats have no CPU layout and throw.

```csharp
public static int ByteCount(TextureFormat format, int width, int height)
```

#### Parameters

`format` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_TextureFormats_IsCompressed_MicroEngine_Graphics_Devices_TextureFormat_"></a> IsCompressed\(TextureFormat\)

True for the 4×4 block-compressed formats.

```csharp
public static bool IsCompressed(TextureFormat format)
```

#### Parameters

`format` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_TextureFormats_IsDepth_MicroEngine_Graphics_Devices_TextureFormat_"></a> IsDepth\(TextureFormat\)

True for [D16Unorm](MicroEngine.Graphics.Devices.TextureFormat.md), [D24UnormS8](MicroEngine.Graphics.Devices.TextureFormat.md), [D32Float](MicroEngine.Graphics.Devices.TextureFormat.md).

```csharp
public static bool IsDepth(TextureFormat format)
```

#### Parameters

`format` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
