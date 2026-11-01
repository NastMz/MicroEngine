# Class PngEncoder

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

Writes 8-bit RGBA PNG files (no filtering), for backends that read frames back as raw pixels.

```csharp
public static class PngEncoder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PngEncoder](MicroEngine.Graphics.PngEncoder.md)

## Methods

### <a id="MicroEngine_Graphics_PngEncoder_Encode_System_ReadOnlySpan_System_Byte__System_Int32_System_Int32_System_Int32_"></a> Encode\(ReadOnlySpan<byte\>, int, int, int\)

Encodes <code class="paramref">rgba</code> (width × height × 4 bytes, rows top to bottom), each pixel repeated <code class="paramref">scale</code> times in both directions.

```csharp
public static byte[] Encode(ReadOnlySpan<byte> rgba, int width, int height, int scale = 1)
```

#### Parameters

`rgba` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`scale` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]
