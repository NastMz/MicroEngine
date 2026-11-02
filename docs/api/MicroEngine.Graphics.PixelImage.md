# Class PixelImage

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

A CPU-side RGBA image. Games author procedural art with it and hand it to
[CreateTexture](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_MicroEngine_Graphics_TextureFilter_); it also backs the built-in font.

```csharp
public sealed class PixelImage
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PixelImage](MicroEngine.Graphics.PixelImage.md)

## Remarks

Drawing methods change this image and return it, so calls chain; methods that build a new image ([Clone](MicroEngine.Graphics.PixelImage.md#MicroEngine_Graphics_PixelImage_Clone),
[FlipX](MicroEngine.Graphics.PixelImage.md#MicroEngine_Graphics_PixelImage_FlipX), [Crop](MicroEngine.Graphics.PixelImage.md#MicroEngine_Graphics_PixelImage_Crop_System_Int32_System_Int32_System_Int32_System_Int32_)...) leave it unchanged. Coordinates outside the image are ignored when
writing and read as transparent. Drawing methods alpha-blend unless they say otherwise.

## Constructors

### <a id="MicroEngine_Graphics_PixelImage__ctor_System_Int32_System_Int32_"></a> PixelImage\(int, int\)

Creates a fully transparent image.

```csharp
public PixelImage(int width, int height)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">width</code> or <code class="paramref">height</code> is not positive.

## Properties

### <a id="MicroEngine_Graphics_PixelImage_Height"></a> Height

Height in pixels.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_PixelImage_Pixels"></a> Pixels

The pixels, row by row from the top-left.

```csharp
public ReadOnlySpan<Color> Pixels { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<Color\>

### <a id="MicroEngine_Graphics_PixelImage_Width"></a> Width

Width in pixels.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_PixelImage_Item_System_Int32_System_Int32_"></a> this\[int, int\]

The pixel at (<code class="paramref">x</code>, <code class="paramref">y</code>). Reading outside the image returns [Transparent](MicroEngine.Color.md#MicroEngine_Color_Transparent); writing outside does nothing. Writing replaces the pixel without blending.

```csharp
public Color this[int x, int y] { get; set; }
```

#### Property Value

 Color

## Methods

### <a id="MicroEngine_Graphics_PixelImage_Blend_System_Int32_System_Int32_MicroEngine_Color_"></a> Blend\(int, int, Color\)

Alpha-blends <code class="paramref">color</code> over the existing pixel.

```csharp
public void Blend(int x, int y, Color color)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`color` Color

### <a id="MicroEngine_Graphics_PixelImage_Blit_MicroEngine_Graphics_PixelImage_System_Int32_System_Int32_System_Boolean_"></a> Blit\(PixelImage, int, int, bool\)

Copies another image (alpha-blended) with optional horizontal flip.

```csharp
public PixelImage Blit(PixelImage source, int dx, int dy, bool flipX = false)
```

#### Parameters

`source` [PixelImage](MicroEngine.Graphics.PixelImage.md)

`dx` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`dy` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`flipX` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_Blit_MicroEngine_Graphics_PixelImage_System_Int32_System_Int32_System_Int32_System_Int32_System_Int32_System_Int32_"></a> Blit\(PixelImage, int, int, int, int, int, int\)

Blends the <code class="paramref">w</code> × <code class="paramref">h</code> region of <code class="paramref">source</code> at (<code class="paramref">sx</code>, <code class="paramref">sy</code>) onto this image at (<code class="paramref">dx</code>, <code class="paramref">dy</code>).

```csharp
public PixelImage Blit(PixelImage source, int sx, int sy, int w, int h, int dx, int dy)
```

#### Parameters

`source` [PixelImage](MicroEngine.Graphics.PixelImage.md)

`sx` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`sy` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`w` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`h` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`dx` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`dy` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_Clear_MicroEngine_Color_"></a> Clear\(Color\)

Sets every pixel to <code class="paramref">color</code>, without blending.

```csharp
public PixelImage Clear(Color color)
```

#### Parameters

`color` Color

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_ClearRect_System_Int32_System_Int32_System_Int32_System_Int32_"></a> ClearRect\(int, int, int, int\)

Makes a rectangle fully transparent (unlike [FillRect](MicroEngine.Graphics.PixelImage.md#MicroEngine_Graphics_PixelImage_FillRect_System_Int32_System_Int32_System_Int32_System_Int32_MicroEngine_Color_), which blends).

```csharp
public PixelImage ClearRect(int x, int y, int w, int h)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`w` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`h` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_Clone"></a> Clone\(\)

Returns an independent copy.

```csharp
public PixelImage Clone()
```

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_Crop_System_Int32_System_Int32_System_Int32_System_Int32_"></a> Crop\(int, int, int, int\)

Copies a rectangle into a new image; areas outside the source become transparent.

```csharp
public PixelImage Crop(int x, int y, int width, int height)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_Decode_System_ReadOnlySpan_System_Byte__"></a> Decode\(ReadOnlySpan<byte\>\)

Decodes an image file held in memory: PNG (any variant), JPEG (baseline and progressive), BMP, TGA, GIF (its first
frame) or PSD (the composited image), recognised by its content rather than a name.

```csharp
public static PixelImage Decode(ReadOnlySpan<byte> data)
```

#### Parameters

`data` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The data is not an image in one of those formats.

### <a id="MicroEngine_Graphics_PixelImage_DrawRect_System_Int32_System_Int32_System_Int32_System_Int32_MicroEngine_Color_"></a> DrawRect\(int, int, int, int, Color\)

Draws the one-pixel outline of a rectangle.

```csharp
public PixelImage DrawRect(int x, int y, int w, int h, Color color)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`w` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`h` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`color` Color

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_FillCircle_System_Single_System_Single_System_Single_MicroEngine_Color_"></a> FillCircle\(float, float, float, Color\)

Filled circle centred on a pixel-space coordinate (half-pixel centres allowed).

```csharp
public PixelImage FillCircle(float cx, float cy, float radius, Color color)
```

#### Parameters

`cx` [float](https://learn.microsoft.com/dotnet/api/system.single)

`cy` [float](https://learn.microsoft.com/dotnet/api/system.single)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` Color

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_FillEllipse_System_Single_System_Single_System_Single_System_Single_MicroEngine_Color_"></a> FillEllipse\(float, float, float, float, Color\)

Fills an ellipse centred on (<code class="paramref">cx</code>, <code class="paramref">cy</code>) with radii <code class="paramref">rx</code> and <code class="paramref">ry</code>.

```csharp
public PixelImage FillEllipse(float cx, float cy, float rx, float ry, Color color)
```

#### Parameters

`cx` [float](https://learn.microsoft.com/dotnet/api/system.single)

`cy` [float](https://learn.microsoft.com/dotnet/api/system.single)

`rx` [float](https://learn.microsoft.com/dotnet/api/system.single)

`ry` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` Color

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_FillRect_System_Int32_System_Int32_System_Int32_System_Int32_MicroEngine_Color_"></a> FillRect\(int, int, int, int, Color\)

Blends <code class="paramref">color</code> over a <code class="paramref">w</code> × <code class="paramref">h</code> rectangle whose top-left is (<code class="paramref">x</code>, <code class="paramref">y</code>).

```csharp
public PixelImage FillRect(int x, int y, int w, int h, Color color)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`w` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`h` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`color` Color

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_FitInside_System_Int32_System_Int32_"></a> FitInside\(int, int\)

The largest size of this image that fits inside <code class="paramref">maxWidth</code> by <code class="paramref">maxHeight</code>, keeping its shape (at least 1 by 1).

```csharp
public (int Width, int Height) FitInside(int maxWidth, int maxHeight)
```

#### Parameters

`maxWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`maxHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

### <a id="MicroEngine_Graphics_PixelImage_FlipX"></a> FlipX\(\)

Returns a copy mirrored horizontally.

```csharp
public PixelImage FlipX()
```

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_FlipY"></a> FlipY\(\)

Returns a copy mirrored vertically.

```csharp
public PixelImage FlipY()
```

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_FromMap_System_Collections_Generic_IReadOnlyDictionary_System_Char_MicroEngine_Color__System_String___"></a> FromMap\(IReadOnlyDictionary<char, Color\>, params string\[\]\)

Creates an image from a character map; the image size is the map size.

```csharp
public static PixelImage FromMap(IReadOnlyDictionary<char, Color> palette, params string[] rows)
```

#### Parameters

`palette` [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[char](https://learn.microsoft.com/dotnet/api/system.char), Color\>

`rows` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_FromPng_System_ReadOnlySpan_System_Byte__"></a> FromPng\(ReadOnlySpan<byte\>\)

Decodes a PNG: any colour type and bit depth, interlaced or not.

```csharp
public static PixelImage FromPng(ReadOnlySpan<byte> data)
```

#### Parameters

`data` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The data is not a PNG the engine can read.

### <a id="MicroEngine_Graphics_PixelImage_InBounds_System_Int32_System_Int32_"></a> InBounds\(int, int\)

Whether (<code class="paramref">x</code>, <code class="paramref">y</code>) is inside the image.

```csharp
public bool InBounds(int x, int y)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_PixelImage_Line_System_Int32_System_Int32_System_Int32_System_Int32_MicroEngine_Color_"></a> Line\(int, int, int, int, Color\)

Draws a one-pixel line between two pixels, both included.

```csharp
public PixelImage Line(int x0, int y0, int x1, int y1, Color color)
```

#### Parameters

`x0` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y0` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`x1` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y1` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`color` Color

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_Load_System_String_"></a> Load\(string\)

Loads an image file in any format [Decode](MicroEngine.Graphics.PixelImage.md#MicroEngine_Graphics_PixelImage_Decode_System_ReadOnlySpan_System_Byte__) reads; the message names the path when decoding fails.

```csharp
public static PixelImage Load(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

#### Exceptions

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

The file does not exist.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The file is not an image the engine can read.

### <a id="MicroEngine_Graphics_PixelImage_Map_System_Func_System_Int32_System_Int32_MicroEngine_Color_MicroEngine_Color__"></a> Map\(Func<int, int, Color, Color\>\)

Replaces each pixel with <code>transform(x, y, pixel)</code>.

```csharp
public PixelImage Map(Func<int, int, Color, Color> transform)
```

#### Parameters

`transform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[int](https://learn.microsoft.com/dotnet/api/system.int32), [int](https://learn.microsoft.com/dotnet/api/system.int32), Color, Color\>

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_OpaqueBounds"></a> OpaqueBounds\(\)

Smallest rectangle containing every pixel with alpha above zero; null when fully transparent.

```csharp
public (int X, int Y, int Width, int Height)? OpaqueBounds()
```

#### Returns

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [X](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32,system.int32,system.int32\-.x), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Y](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32,system.int32,system.int32\-.y), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32,system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32,system.int32,system.int32\-.height)\)?

### <a id="MicroEngine_Graphics_PixelImage_Outline_MicroEngine_Color_System_Boolean_"></a> Outline\(Color, bool\)

Adds a 1px outline of <code class="paramref">color</code> around every opaque region (4-neighbourhood).

```csharp
public PixelImage Outline(Color color, bool diagonals = false)
```

#### Parameters

`color` Color

`diagonals` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_Paint_System_Int32_System_Int32_System_Collections_Generic_IReadOnlyDictionary_System_Char_MicroEngine_Color__System_String___"></a> Paint\(int, int, IReadOnlyDictionary<char, Color\>, params string\[\]\)

Paints a character map. Each row is a string; each char is looked up in <code class="paramref">palette</code>.
Characters missing from the palette (conventionally '.' or ' ') are left untouched.

```csharp
public PixelImage Paint(int dx, int dy, IReadOnlyDictionary<char, Color> palette, params string[] rows)
```

#### Parameters

`dx` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`dy` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`palette` [IReadOnlyDictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary\-2)<[char](https://learn.microsoft.com/dotnet/api/system.char), Color\>

`rows` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_Recolor_MicroEngine_Color_MicroEngine_Color_"></a> Recolor\(Color, Color\)

Replaces every exact <code class="paramref">from</code> pixel with <code class="paramref">to</code>.

```csharp
public PixelImage Recolor(Color from, Color to)
```

#### Parameters

`from` Color

`to` Color

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_Resized_System_Int32_System_Int32_"></a> Resized\(int, int\)

A copy at <code class="paramref">width</code> by <code class="paramref">height</code> pixels. Whole-number enlargements repeat pixels,
so pixel art stays sharp; other sizes average the source area under each pixel (weighted by opacity, so
transparent pixels do not darken edges), so a 1024-pixel icon reduced for a launcher or a start screen keeps its
detail instead of dropping rows.

```csharp
public PixelImage Resized(int width, int height)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A size is not positive.

### <a id="MicroEngine_Graphics_PixelImage_Rotate90"></a> Rotate90\(\)

Returns a copy rotated 90 degrees clockwise (width and height swap).

```csharp
public PixelImage Rotate90()
```

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_Rotated_System_Single_System_Single_System_Single_"></a> Rotated\(float, float, float\)

Returns a same-size copy rotated by <code class="paramref">radians</code> (clockwise on screen) around a pivot,
sampling nearest neighbours so the palette stays intact. Content leaving the canvas is clipped.
Useful for deriving extra poses (e.g. aiming arms) from authored frames.

```csharp
public PixelImage Rotated(float radians, float pivotX, float pivotY)
```

#### Parameters

`radians` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pivotX` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pivotY` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_Scale2x"></a> Scale2x\(\)

Doubles the image with the Scale2x (EPX) rule: flat areas stay flat while one-pixel stair steps
become slopes, so small sprites can be shown at twice the size without looking blocky.
Pixels beyond the edge count as copies of the edge pixel.

```csharp
public PixelImage Scale2x()
```

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_Silhouette_MicroEngine_Color_"></a> Silhouette\(Color\)

Returns a copy where every visible pixel becomes <code class="paramref">color</code> (hit flashes, shadows).

```csharp
public PixelImage Silhouette(Color color)
```

#### Parameters

`color` Color

#### Returns

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PixelImage_ToRgba"></a> ToRgba\(\)

Raw RGBA8 bytes, row-major, for backends.

```csharp
public byte[] ToRgba()
```

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

</div>
