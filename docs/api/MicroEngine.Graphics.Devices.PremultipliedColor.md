# Class PremultipliedColor

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Conversions between 8-bit colours and the premultiplied float colours of the device contract.

```csharp
public static class PremultipliedColor
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PremultipliedColor](MicroEngine.Graphics.Devices.PremultipliedColor.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_PremultipliedColor_FromBytes_System_Byte_System_Byte_System_Byte_System_Byte_"></a> FromBytes\(byte, byte, byte, byte\)

A colour from 8-bit channels that are already premultiplied: each channel is <code>byte / 255</code>.

```csharp
public static ColorF FromBytes(byte r, byte g, byte b, byte a = 255)
```

#### Parameters

`r` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

`g` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

`b` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

`a` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

#### Returns

 [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_Graphics_Devices_PremultipliedColor_FromStraight_MicroEngine_Color_"></a> FromStraight\(Color\)

The premultiplied form of a straight (not premultiplied) 8-bit colour, as the 2D layer passes tints to the device:
each colour channel <code>c / 255 × a / 255</code>.

```csharp
public static ColorF FromStraight(Color color)
```

#### Parameters

`color` [Color](MicroEngine.Color.md)

#### Returns

 [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_Graphics_Devices_PremultipliedColor_PremultiplyInPlace_System_Span_System_Byte__"></a> PremultiplyInPlace\(Span<byte\>\)

Premultiplies straight 8-bit RGBA texels in place, as an upload of straight-alpha image data does (each colour
channel becomes <code>round(c × a / 255)</code>). Opaque and fully transparent texels are exact.

```csharp
public static void PremultiplyInPlace(Span<byte> rgba)
```

#### Parameters

`rgba` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

Tightly packed R, G, B, A bytes.

</div>
