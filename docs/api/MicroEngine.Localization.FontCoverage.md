# Class FontCoverage

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Localization](MicroEngine.Localization.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

The characters a TrueType or OpenType font file has glyphs for, read from its character map without rasterizing
anything: which characters of a language's texts a font would draw as <code>?</code>. The content check uses it for the
fonts a string table names; a game can ask the same of the fonts it loads
(<code>FontCoverage.Read(Assets.Bytes("fonts/ui.ttf")).Missing(Localization.Characters())</code>).

```csharp
public sealed class FontCoverage
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FontCoverage](MicroEngine.Localization.FontCoverage.md)

## Remarks

Having a glyph for each character is necessary and, for scripts that join or reorder letters (Arabic, Indic
scripts), not sufficient: those also need the shaping package to draw correctly.

## Properties

### <a id="MicroEngine_Localization_FontCoverage_Count"></a> Count

How many characters the font has.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Localization_FontCoverage_Covers_System_Int32_"></a> Covers\(int\)

Whether the font has a glyph for a character (a Unicode code point).

```csharp
public bool Covers(int codePoint)
```

#### Parameters

`codePoint` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Localization_FontCoverage_Missing_System_String_"></a> Missing\(string\)

The characters of <code class="paramref">text</code> the font lacks, each once, in the order met. Spaces, line breaks and other characters that draw nothing never count.

```csharp
public string Missing(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_FontCoverage_Missing_System_String_MicroEngine_Localization_FontCoverage___"></a> Missing\(string, params FontCoverage\[\]\)

The characters of <code class="paramref">text</code> that no font of <code class="paramref">chain</code> has (a face and those that fill in what it lacks), each once, in the order met.

```csharp
public static string Missing(string text, params FontCoverage[] chain)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`chain` [FontCoverage](MicroEngine.Localization.FontCoverage.md)\[\]

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Localization_FontCoverage_Read_System_Byte___"></a> Read\(byte\[\]\)

Reads the character map of a font file (<code>.ttf</code>, <code>.otf</code>, or the first face of a <code>.ttc</code>).

```csharp
public static FontCoverage Read(byte[] font)
```

#### Parameters

`font` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

#### Returns

 [FontCoverage](MicroEngine.Localization.FontCoverage.md)

#### Exceptions

 [InvalidDataException](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception)

The bytes are not a font, or the font has no Unicode character map.

</div>
