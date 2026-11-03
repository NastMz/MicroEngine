# Struct ColorF

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

A floating-point RGBA colour in linear light, for blending, lighting and anything that adds or scales colours.
Channels are not clamped (values above 1 are brighter than white). [Color](MicroEngine.Color.md) is the 8-bit sRGB form used
to store and name colours; convert with [FromSrgb](MicroEngine.ColorF.md#MicroEngine_ColorF_FromSrgb_MicroEngine_Color_) and [ToSrgb](MicroEngine.ColorF.md#MicroEngine_ColorF_ToSrgb).

```csharp
public readonly struct ColorF : IEquatable<ColorF>
```

#### Implements

[IEquatable<ColorF\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Remarks

The colour is straight (not premultiplied) unless it came from [Premultiply](MicroEngine.ColorF.md#MicroEngine_ColorF_Premultiply); nothing in the type records
which, so keep premultiplied values in variables that say so. Premultiplied colours blend and filter without dark
fringes: a red-to-transparent edge sampled halfway over white stays red.

## Constructors

### <a id="MicroEngine_ColorF__ctor_System_Single_System_Single_System_Single_System_Single_"></a> ColorF\(float, float, float, float\)

A floating-point RGBA colour in linear light, for blending, lighting and anything that adds or scales colours.
Channels are not clamped (values above 1 are brighter than white). [Color](MicroEngine.Color.md) is the 8-bit sRGB form used
to store and name colours; convert with [FromSrgb](MicroEngine.ColorF.md#MicroEngine_ColorF_FromSrgb_MicroEngine_Color_) and [ToSrgb](MicroEngine.ColorF.md#MicroEngine_ColorF_ToSrgb).

```csharp
public ColorF(float R, float G, float B, float A = 1)
```

#### Parameters

`R` [float](https://learn.microsoft.com/dotnet/api/system.single)

Red in linear light, 0..1 for displayable colours.

`G` [float](https://learn.microsoft.com/dotnet/api/system.single)

Green in linear light.

`B` [float](https://learn.microsoft.com/dotnet/api/system.single)

Blue in linear light.

`A` [float](https://learn.microsoft.com/dotnet/api/system.single)

Opacity, 0 (transparent) to 1 (opaque, the default).

#### Remarks

The colour is straight (not premultiplied) unless it came from [Premultiply](MicroEngine.ColorF.md#MicroEngine_ColorF_Premultiply); nothing in the type records
which, so keep premultiplied values in variables that say so. Premultiplied colours blend and filter without dark
fringes: a red-to-transparent edge sampled halfway over white stays red.

## Fields

### <a id="MicroEngine_ColorF_Black"></a> Black

Opaque black (0, 0, 0, 1).

```csharp
public static readonly ColorF Black
```

#### Field Value

 [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_ColorF_Transparent"></a> Transparent

Fully transparent black (0, 0, 0, 0).

```csharp
public static readonly ColorF Transparent
```

#### Field Value

 [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_ColorF_White"></a> White

Opaque white (1, 1, 1, 1).

```csharp
public static readonly ColorF White
```

#### Field Value

 [ColorF](MicroEngine.ColorF.md)

## Properties

### <a id="MicroEngine_ColorF_A"></a> A

Opacity, 0 (transparent) to 1 (opaque, the default).

```csharp
public float A { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_ColorF_B"></a> B

Blue in linear light.

```csharp
public float B { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_ColorF_G"></a> G

Green in linear light.

```csharp
public float G { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_ColorF_R"></a> R

Red in linear light, 0..1 for displayable colours.

```csharp
public float R { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_ColorF_Deconstruct_System_Single__System_Single__System_Single__System_Single__"></a> Deconstruct\(out float, out float, out float, out float\)

```csharp
public void Deconstruct(out float R, out float G, out float B, out float A)
```

#### Parameters

`R` [float](https://learn.microsoft.com/dotnet/api/system.single)

`G` [float](https://learn.microsoft.com/dotnet/api/system.single)

`B` [float](https://learn.microsoft.com/dotnet/api/system.single)

`A` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_ColorF_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_ColorF_Equals_MicroEngine_ColorF_"></a> Equals\(ColorF\)

```csharp
public bool Equals(ColorF other)
```

#### Parameters

`other` [ColorF](MicroEngine.ColorF.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_ColorF_FromSrgb_MicroEngine_Color_"></a> FromSrgb\(Color\)

The linear-light value of an 8-bit sRGB colour: each colour channel through the sRGB transfer function (IEC
61966-2-1), opacity divided by 255.

```csharp
public static ColorF FromSrgb(Color color)
```

#### Parameters

`color` [Color](MicroEngine.Color.md)

#### Returns

 [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_ColorF_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_ColorF_Lerp_MicroEngine_ColorF_MicroEngine_ColorF_System_Single_"></a> Lerp\(ColorF, ColorF, float\)

Linear interpolation of every channel; <code class="paramref">t</code> is clamped to 0..1.

```csharp
public static ColorF Lerp(ColorF a, ColorF b, float t)
```

#### Parameters

`a` [ColorF](MicroEngine.ColorF.md)

`b` [ColorF](MicroEngine.ColorF.md)

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_ColorF_LinearToSrgb_System_Single_"></a> LinearToSrgb\(float\)

The sRGB transfer function: linear light (0..1) to an sRGB-encoded value.

```csharp
public static float LinearToSrgb(float value)
```

#### Parameters

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_ColorF_Premultiply"></a> Premultiply\(\)

The colour with its colour channels multiplied by its opacity: (R·A, G·A, B·A, A).

```csharp
public ColorF Premultiply()
```

#### Returns

 [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_ColorF_SrgbToLinear_System_Single_"></a> SrgbToLinear\(float\)

The sRGB transfer function decoded: an sRGB-encoded value (0..1) to linear light.

```csharp
public static float SrgbToLinear(float value)
```

#### Parameters

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_ColorF_ToSrgb"></a> ToSrgb\(\)

The nearest 8-bit sRGB colour: each channel clamped to 0..1, encoded with the sRGB transfer function and rounded.
<code>FromSrgb(c).ToSrgb()</code> returns <code>c</code> for every 8-bit colour.

```csharp
public Color ToSrgb()
```

#### Returns

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_ColorF_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_ColorF_Unpremultiply"></a> Unpremultiply\(\)

The straight colour of a premultiplied one: colour channels divided by opacity. A fully transparent colour has no
colour left to recover and gives [Transparent](MicroEngine.ColorF.md#MicroEngine_ColorF_Transparent).

```csharp
public ColorF Unpremultiply()
```

#### Returns

 [ColorF](MicroEngine.ColorF.md)

## Operators

### <a id="MicroEngine_ColorF_op_Addition_MicroEngine_ColorF_MicroEngine_ColorF_"></a> operator \+\(ColorF, ColorF\)

Channel-by-channel sum (additive light).

```csharp
public static ColorF operator +(ColorF a, ColorF b)
```

#### Parameters

`a` [ColorF](MicroEngine.ColorF.md)

`b` [ColorF](MicroEngine.ColorF.md)

#### Returns

 [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_ColorF_op_Equality_MicroEngine_ColorF_MicroEngine_ColorF_"></a> operator ==\(ColorF, ColorF\)

```csharp
public static bool operator ==(ColorF left, ColorF right)
```

#### Parameters

`left` [ColorF](MicroEngine.ColorF.md)

`right` [ColorF](MicroEngine.ColorF.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_ColorF_op_Inequality_MicroEngine_ColorF_MicroEngine_ColorF_"></a> operator \!=\(ColorF, ColorF\)

```csharp
public static bool operator !=(ColorF left, ColorF right)
```

#### Parameters

`left` [ColorF](MicroEngine.ColorF.md)

`right` [ColorF](MicroEngine.ColorF.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_ColorF_op_Multiply_MicroEngine_ColorF_System_Single_"></a> operator \*\(ColorF, float\)

Every channel multiplied by <code class="paramref">factor</code>, opacity included.

```csharp
public static ColorF operator *(ColorF color, float factor)
```

#### Parameters

`color` [ColorF](MicroEngine.ColorF.md)

`factor` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_ColorF_op_Multiply_MicroEngine_ColorF_MicroEngine_ColorF_"></a> operator \*\(ColorF, ColorF\)

Channel-by-channel product (modulation): a tint applied to a colour.

```csharp
public static ColorF operator *(ColorF a, ColorF b)
```

#### Parameters

`a` [ColorF](MicroEngine.ColorF.md)

`b` [ColorF](MicroEngine.ColorF.md)

#### Returns

 [ColorF](MicroEngine.ColorF.md)

</div>
