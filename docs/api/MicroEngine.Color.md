# Struct Color

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

An 8-bit-per-channel, non-premultiplied RGBA color.

```csharp
public readonly struct Color : IEquatable<Color>
```

#### Implements

[IEquatable<Color\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Color__ctor_System_Byte_System_Byte_System_Byte_System_Byte_"></a> Color\(byte, byte, byte, byte\)

An 8-bit-per-channel, non-premultiplied RGBA color.

```csharp
public Color(byte R, byte G, byte B, byte A = 255)
```

#### Parameters

`R` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

Red, 0..255.

`G` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

Green, 0..255.

`B` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

Blue, 0..255.

`A` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

Opacity, 0 (transparent) to 255 (opaque, the default).

## Fields

### <a id="MicroEngine_Color_Black"></a> Black

Opaque black.

```csharp
public static readonly Color Black
```

#### Field Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Color_Transparent"></a> Transparent

Fully transparent black (0, 0, 0, 0).

```csharp
public static readonly Color Transparent
```

#### Field Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Color_White"></a> White

Opaque white. As a tint it leaves a texture unchanged.

```csharp
public static readonly Color White
```

#### Field Value

 [Color](MicroEngine.Color.md)

## Properties

### <a id="MicroEngine_Color_A"></a> A

Opacity, 0 (transparent) to 255 (opaque, the default).

```csharp
public byte A { get; init; }
```

#### Property Value

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)

### <a id="MicroEngine_Color_B"></a> B

Blue, 0..255.

```csharp
public byte B { get; init; }
```

#### Property Value

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)

### <a id="MicroEngine_Color_G"></a> G

Green, 0..255.

```csharp
public byte G { get; init; }
```

#### Property Value

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)

### <a id="MicroEngine_Color_R"></a> R

Red, 0..255.

```csharp
public byte R { get; init; }
```

#### Property Value

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)

## Methods

### <a id="MicroEngine_Color_Deconstruct_System_Byte__System_Byte__System_Byte__System_Byte__"></a> Deconstruct\(out byte, out byte, out byte, out byte\)

```csharp
public void Deconstruct(out byte R, out byte G, out byte B, out byte A)
```

#### Parameters

`R` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

`G` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

`B` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

`A` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

### <a id="MicroEngine_Color_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Color_Equals_MicroEngine_Color_"></a> Equals\(Color\)

```csharp
public bool Equals(Color other)
```

#### Parameters

`other` [Color](MicroEngine.Color.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Color_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Color_Hex_System_String_"></a> Hex\(string\)

Parses "#RRGGBB" or "#RRGGBBAA" (the leading '#' is optional).

```csharp
public static Color Hex(string hex)
```

#### Parameters

`hex` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Color_Lerp_MicroEngine_Color_MicroEngine_Color_System_Single_"></a> Lerp\(Color, Color, float\)

Linear interpolation of every channel.

```csharp
public static Color Lerp(Color a, Color b, float t)
```

#### Parameters

`a` [Color](MicroEngine.Color.md)

`b` [Color](MicroEngine.Color.md)

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Color_Shade_System_Single_"></a> Shade\(float\)

Multiplies RGB by <code class="paramref">factor</code>; values above 1 brighten.

```csharp
public Color Shade(float factor)
```

#### Parameters

`factor` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Color_ToString"></a> ToString\(\)

The colour as "#RRGGBBAA", readable by [Hex](MicroEngine.Color.md#MicroEngine_Color_Hex_System_String_).

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Color_WithAlpha_System_Byte_"></a> WithAlpha\(byte\)

The same colour with opacity <code class="paramref">alpha</code> (0..255).

```csharp
public Color WithAlpha(byte alpha)
```

#### Parameters

`alpha` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

#### Returns

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Color_WithAlpha_System_Single_"></a> WithAlpha\(float\)

The same colour with opacity <code class="paramref">alpha</code> given as 0..1 (clamped; fractions of a step are truncated).

```csharp
public Color WithAlpha(float alpha)
```

#### Parameters

`alpha` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Color](MicroEngine.Color.md)

## Operators

### <a id="MicroEngine_Color_op_Equality_MicroEngine_Color_MicroEngine_Color_"></a> operator ==\(Color, Color\)

```csharp
public static bool operator ==(Color left, Color right)
```

#### Parameters

`left` [Color](MicroEngine.Color.md)

`right` [Color](MicroEngine.Color.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Color_op_Inequality_MicroEngine_Color_MicroEngine_Color_"></a> operator \!=\(Color, Color\)

```csharp
public static bool operator !=(Color left, Color right)
```

#### Parameters

`left` [Color](MicroEngine.Color.md)

`right` [Color](MicroEngine.Color.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)
