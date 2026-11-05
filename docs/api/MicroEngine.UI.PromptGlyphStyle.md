# Class PromptGlyphStyle

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.dll  
Guide: [UI widgets](../manual/ui.md)  

Colours of prompt glyphs ([PromptText](MicroEngine.UI.PromptText.md)): key caps, gamepad buttons and touch controls.

```csharp
public sealed record PromptGlyphStyle : IEquatable<PromptGlyphStyle>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)

#### Implements

[IEquatable<PromptGlyphStyle\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_UI_PromptGlyphStyle__ctor_MicroEngine_Color_MicroEngine_Color_MicroEngine_Color_"></a> PromptGlyphStyle\(Color, Color, Color\)

Colours of prompt glyphs ([PromptText](MicroEngine.UI.PromptText.md)): key caps, gamepad buttons and touch controls.

```csharp
public PromptGlyphStyle(Color Cap, Color CapBorder, Color CapText)
```

#### Parameters

`Cap` Color

Fill of key caps and of gamepad buttons without a colour of their own.

`CapBorder` Color

Outline of caps, pills and touch rings.

`CapText` Color

Text on caps and buttons.

## Fields

### <a id="MicroEngine_UI_PromptGlyphStyle_Default"></a> Default

Light caps with dark text, readable on most backgrounds.

```csharp
public static readonly PromptGlyphStyle Default
```

#### Field Value

 [PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)

## Properties

### <a id="MicroEngine_UI_PromptGlyphStyle_Cap"></a> Cap

Fill of key caps and of gamepad buttons without a colour of their own.

```csharp
public Color Cap { get; init; }
```

#### Property Value

 Color

### <a id="MicroEngine_UI_PromptGlyphStyle_CapBorder"></a> CapBorder

Outline of caps, pills and touch rings.

```csharp
public Color CapBorder { get; init; }
```

#### Property Value

 Color

### <a id="MicroEngine_UI_PromptGlyphStyle_CapText"></a> CapText

Text on caps and buttons.

```csharp
public Color CapText { get; init; }
```

#### Property Value

 Color

### <a id="MicroEngine_UI_PromptGlyphStyle_ColoredFaceButtons"></a> ColoredFaceButtons

Whether Xbox and Nintendo face buttons are filled with their usual colours (A green, B red, X blue, Y yellow).

```csharp
public bool ColoredFaceButtons { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_UI_PromptGlyphStyle__Clone__"></a> <Clone\>$\(\)

```csharp
public PromptGlyphStyle <Clone>$()
```

#### Returns

 [PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)

### <a id="MicroEngine_UI_PromptGlyphStyle_Deconstruct_MicroEngine_Color__MicroEngine_Color__MicroEngine_Color__"></a> Deconstruct\(out Color, out Color, out Color\)

```csharp
public void Deconstruct(out Color Cap, out Color CapBorder, out Color CapText)
```

#### Parameters

`Cap` Color

`CapBorder` Color

`CapText` Color

### <a id="MicroEngine_UI_PromptGlyphStyle_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_PromptGlyphStyle_Equals_MicroEngine_UI_PromptGlyphStyle_"></a> Equals\(PromptGlyphStyle?\)

```csharp
public bool Equals(PromptGlyphStyle? other)
```

#### Parameters

`other` [PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_PromptGlyphStyle_From_MicroEngine_UI_UiTheme_"></a> From\(UiTheme\)

Caps in a theme's text colour, outlined with its shadow, labelled in its panel colour.

```csharp
public static PromptGlyphStyle From(UiTheme theme)
```

#### Parameters

`theme` UiTheme

#### Returns

 [PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)

### <a id="MicroEngine_UI_PromptGlyphStyle_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_PromptGlyphStyle_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_UI_PromptGlyphStyle_op_Equality_MicroEngine_UI_PromptGlyphStyle_MicroEngine_UI_PromptGlyphStyle_"></a> operator ==\(PromptGlyphStyle?, PromptGlyphStyle?\)

```csharp
public static bool operator ==(PromptGlyphStyle? left, PromptGlyphStyle? right)
```

#### Parameters

`left` [PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)?

`right` [PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_PromptGlyphStyle_op_Inequality_MicroEngine_UI_PromptGlyphStyle_MicroEngine_UI_PromptGlyphStyle_"></a> operator \!=\(PromptGlyphStyle?, PromptGlyphStyle?\)

```csharp
public static bool operator !=(PromptGlyphStyle? left, PromptGlyphStyle? right)
```

#### Parameters

`left` [PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)?

`right` [PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
