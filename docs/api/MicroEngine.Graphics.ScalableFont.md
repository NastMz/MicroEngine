# Class ScalableFont

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Text.dll  
Guide: [Graphics](../manual/rendering.md)  

A TrueType or OpenType font (TTF, OTF with TrueType or CFF outlines) that draws text at any size. Glyphs are
rasterized the first time they are drawn at a size and kept in shared glyph pages (textures), so later frames only
draw quads; [Prepare](MicroEngine.Graphics.ScalableFont.md#MicroEngine_Graphics_ScalableFont_Prepare_System_String_System_Single___) rasterizes ahead, so the first frames of play do no font work.

```csharp
public sealed class ScalableFont : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ScalableFont](MicroEngine.Graphics.ScalableFont.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Examples

```csharp
var font = ScalableFont.Load(Graphics, "Content/fonts/Inter-Regular.ttf");
font.Draw(renderer, "Score 120", new Vector2(24, 24), 32, Color.White);
var title = font.At(64);              // an IFont for widgets and layout
title.DrawCentered(renderer, "Paused", new Vector2(640, 360), Color.White);
```

## Remarks

Built on FontStashSharp (StbTrueTypeSharp rasterizer). One [ScalableFont](MicroEngine.Graphics.ScalableFont.md) serves every size; extra faces
added with [AddFallback](MicroEngine.Graphics.ScalableFont.md#MicroEngine_Graphics_ScalableFont_AddFallback_System_Byte___System_String_) supply characters the main face lacks (another script, symbols). Missing
characters draw as '?'. Usually loaded and owned by <code>Game.Assets</code>; a font made here is disposed by its creator.

## Fields

### <a id="MicroEngine_Graphics_ScalableFont_Ascii"></a> Ascii

Printable ASCII: letters, digits and punctuation, a good default for [Prepare](MicroEngine.Graphics.ScalableFont.md#MicroEngine_Graphics_ScalableFont_Prepare_System_String_System_Single___).

```csharp
public const string Ascii = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Graphics_ScalableFont_Filter"></a> Filter

How the glyph pages are sampled; applies to existing and future pages.

```csharp
public TextureFilter Filter { get; set; }
```

#### Property Value

 TextureFilter

### <a id="MicroEngine_Graphics_ScalableFont_GlyphsRasterized"></a> GlyphsRasterized

Glyphs rasterized so far, across every size (a measure of font work done).

```csharp
public int GlyphsRasterized { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_ScalableFont_Name"></a> Name

The file name (or the name given), for messages.

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_ScalableFont_Options"></a> Options

The options the font was opened with.

```csharp
public FontOptions Options { get; }
```

#### Property Value

 [FontOptions](MicroEngine.Graphics.FontOptions.md)

### <a id="MicroEngine_Graphics_ScalableFont_Pages"></a> Pages

Glyph pages created so far (each [PageSize](MicroEngine.Graphics.FontOptions.md#MicroEngine_Graphics_FontOptions_PageSize) squared, 4 bytes per pixel).

```csharp
public int Pages { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_ScalableFont_TextureBytes"></a> TextureBytes

Device memory of the glyph pages, in bytes.

```csharp
public long TextureBytes { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

## Methods

### <a id="MicroEngine_Graphics_ScalableFont_AddFallback_System_Byte___System_String_"></a> AddFallback\(byte\[\], string?\)

Adds another TTF/OTF face whose glyphs are used for characters this font lacks (accents, other scripts, icons).
Faces are searched in the order they were added. Glyphs already rasterized are dropped (as by [Reset](MicroEngine.Graphics.ScalableFont.md#MicroEngine_Graphics_ScalableFont_Reset)),
so add fallbacks while loading.

```csharp
public ScalableFont AddFallback(byte[] data, string? name = null)
```

#### Parameters

`data` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [ScalableFont](MicroEngine.Graphics.ScalableFont.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The data is not a font the rasterizer reads.

### <a id="MicroEngine_Graphics_ScalableFont_At_System_Single_"></a> At\(float\)

The font at <code class="paramref">size</code> (pixels per em), as an [IFont](MicroEngine.Graphics.IFont.md); the same object for the same size.

```csharp
public SizedFont At(float size)
```

#### Parameters

`size` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [SizedFont](MicroEngine.Graphics.SizedFont.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">size</code> is not positive.

### <a id="MicroEngine_Graphics_ScalableFont_Dispose"></a> Dispose\(\)

Releases the glyph pages. Text cannot be drawn afterwards.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_ScalableFont_Draw_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> Draw\(IRenderer, string, Vector2, float, Color\)

Draws <code class="paramref">text</code> at <code class="paramref">size</code> with its top-left at <code class="paramref">position</code>.

```csharp
public void Draw(IRenderer renderer, string text, Vector2 position, float size, Color color)
```

#### Parameters

`renderer` IRenderer

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`size` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` Color

### <a id="MicroEngine_Graphics_ScalableFont_FromBytes_MicroEngine_Graphics_IGraphicsDevice_System_Byte___MicroEngine_Graphics_FontOptions_System_String_"></a> FromBytes\(IGraphicsDevice, byte\[\], FontOptions?, string?\)

Opens a font from the bytes of a TTF or OTF file.

```csharp
public static ScalableFont FromBytes(IGraphicsDevice device, byte[] data, FontOptions? options = null, string? name = null)
```

#### Parameters

`device` IGraphicsDevice

Creates the glyph pages.

`data` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

The whole font file.

`options` [FontOptions](MicroEngine.Graphics.FontOptions.md)?

Rasterization and sampling; null for [Default](MicroEngine.Graphics.FontOptions.md#MicroEngine_Graphics_FontOptions_Default).

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

A name for messages and diagnostics.

#### Returns

 [ScalableFont](MicroEngine.Graphics.ScalableFont.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The data is not a font the rasterizer reads.

### <a id="MicroEngine_Graphics_ScalableFont_LineHeight_System_Single_"></a> LineHeight\(float\)

Distance between the tops of two lines at <code class="paramref">size</code>.

```csharp
public float LineHeight(float size)
```

#### Parameters

`size` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ScalableFont_Load_MicroEngine_Graphics_IGraphicsDevice_System_String_MicroEngine_Graphics_FontOptions_"></a> Load\(IGraphicsDevice, string, FontOptions?\)

Opens a TTF or OTF file.

```csharp
public static ScalableFont Load(IGraphicsDevice device, string path, FontOptions? options = null)
```

#### Parameters

`device` IGraphicsDevice

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`options` [FontOptions](MicroEngine.Graphics.FontOptions.md)?

#### Returns

 [ScalableFont](MicroEngine.Graphics.ScalableFont.md)

#### Exceptions

 [FileNotFoundException](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)

The file does not exist.

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The file is not a font the rasterizer reads; the message names it.

### <a id="MicroEngine_Graphics_ScalableFont_Measure_System_String_System_Single_"></a> Measure\(string, float\)

Size of <code class="paramref">text</code> at <code class="paramref">size</code>, in canvas pixels.

```csharp
public Vector2 Measure(string text, float size)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`size` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_ScalableFont_Prepare_System_String_System_Single___"></a> Prepare\(string, params float\[\]\)

Rasterizes <code class="paramref">characters</code> at each of <code class="paramref">sizes</code> now, so drawing them later costs no
font work. Call it while loading for the text a scene will show (digits for a score, the letters of a menu).

```csharp
public void Prepare(string characters, params float[] sizes)
```

#### Parameters

`characters` [string](https://learn.microsoft.com/dotnet/api/system.string)

`sizes` [float](https://learn.microsoft.com/dotnet/api/system.single)\[\]

### <a id="MicroEngine_Graphics_ScalableFont_Reset"></a> Reset\(\)

Drops every rasterized glyph and its pages (freeing their memory); glyphs drawn later are rasterized again.

```csharp
public void Reset()
```

</div>
