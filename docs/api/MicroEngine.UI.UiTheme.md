# Class UiTheme

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.dll  
Guide: [UI widgets](../manual/ui.md)  

Visual theme for the shared widgets so each game can keep its own identity.

```csharp
public sealed record UiTheme : IEquatable<UiTheme>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[UiTheme](MicroEngine.UI.UiTheme.md)

#### Implements

[IEquatable<UiTheme\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_UI_UiTheme__ctor_MicroEngine_Graphics_PanelStyle_MicroEngine_Color_MicroEngine_Color_MicroEngine_Color_MicroEngine_Color_"></a> UiTheme\(PanelStyle, Color, Color, Color, Color\)

Visual theme for the shared widgets so each game can keep its own identity.

```csharp
public UiTheme(PanelStyle Panel, Color Text, Color TextShadow, Color Accent, Color Muted)
```

#### Parameters

`Panel` [PanelStyle](MicroEngine.Graphics.PanelStyle.md)

Colours of panels and boxes.

`Text` [Color](MicroEngine.Color.md)

Normal text.

`TextShadow` [Color](MicroEngine.Color.md)

The 1px shadow under text.

`Accent` [Color](MicroEngine.Color.md)

Highlights: the selected option, name tags, headings.

`Muted` [Color](MicroEngine.Color.md)

Secondary or disabled text.

## Fields

### <a id="MicroEngine_UI_UiTheme_Default"></a> Default

The built-in dark blue theme with a warm yellow accent.

```csharp
public static readonly UiTheme Default
```

#### Field Value

 [UiTheme](MicroEngine.UI.UiTheme.md)

## Properties

### <a id="MicroEngine_UI_UiTheme_Accent"></a> Accent

Highlights: the selected option, name tags, headings.

```csharp
public Color Accent { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_UI_UiTheme_Muted"></a> Muted

Secondary or disabled text.

```csharp
public Color Muted { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_UI_UiTheme_Panel"></a> Panel

Colours of panels and boxes.

```csharp
public PanelStyle Panel { get; init; }
```

#### Property Value

 [PanelStyle](MicroEngine.Graphics.PanelStyle.md)

### <a id="MicroEngine_UI_UiTheme_Text"></a> Text

Normal text.

```csharp
public Color Text { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_UI_UiTheme_TextShadow"></a> TextShadow

The 1px shadow under text.

```csharp
public Color TextShadow { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

## Methods

### <a id="MicroEngine_UI_UiTheme__Clone__"></a> <Clone\>$\(\)

```csharp
public UiTheme <Clone>$()
```

#### Returns

 [UiTheme](MicroEngine.UI.UiTheme.md)

### <a id="MicroEngine_UI_UiTheme_Deconstruct_MicroEngine_Graphics_PanelStyle__MicroEngine_Color__MicroEngine_Color__MicroEngine_Color__MicroEngine_Color__"></a> Deconstruct\(out PanelStyle, out Color, out Color, out Color, out Color\)

```csharp
public void Deconstruct(out PanelStyle Panel, out Color Text, out Color TextShadow, out Color Accent, out Color Muted)
```

#### Parameters

`Panel` [PanelStyle](MicroEngine.Graphics.PanelStyle.md)

`Text` [Color](MicroEngine.Color.md)

`TextShadow` [Color](MicroEngine.Color.md)

`Accent` [Color](MicroEngine.Color.md)

`Muted` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_UI_UiTheme_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiTheme_Equals_MicroEngine_UI_UiTheme_"></a> Equals\(UiTheme?\)

```csharp
public bool Equals(UiTheme? other)
```

#### Parameters

`other` [UiTheme](MicroEngine.UI.UiTheme.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiTheme_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_UiTheme_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_UI_UiTheme_op_Equality_MicroEngine_UI_UiTheme_MicroEngine_UI_UiTheme_"></a> operator ==\(UiTheme?, UiTheme?\)

```csharp
public static bool operator ==(UiTheme? left, UiTheme? right)
```

#### Parameters

`left` [UiTheme](MicroEngine.UI.UiTheme.md)?

`right` [UiTheme](MicroEngine.UI.UiTheme.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiTheme_op_Inequality_MicroEngine_UI_UiTheme_MicroEngine_UI_UiTheme_"></a> operator \!=\(UiTheme?, UiTheme?\)

```csharp
public static bool operator !=(UiTheme? left, UiTheme? right)
```

#### Parameters

`left` [UiTheme](MicroEngine.UI.UiTheme.md)?

`right` [UiTheme](MicroEngine.UI.UiTheme.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)
