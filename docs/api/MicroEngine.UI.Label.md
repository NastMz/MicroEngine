# Class Label

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

Text that does nothing: a heading, a value, a hint, a line of dialogue. Lines are separated by <code>\n</code>; with
[Wrap](MicroEngine.UI.Label.md#MicroEngine_UI_Label_Wrap) and a fixed [Width](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Width) the text also breaks between words to fit. Set
[VisibleCharacters](MicroEngine.UI.Label.md#MicroEngine_UI_Label_VisibleCharacters) to reveal it a little at a time: the lines stay where the whole text puts them.

```csharp
public sealed class Label : Widget
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Widget](MicroEngine.UI.Widget.md) ← 
[Label](MicroEngine.UI.Label.md)

#### Inherited Members

[Widget.Measure\(UiContext\)](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Measure\_MicroEngine\_UI\_UiContext\_), 
[Widget.Arrange\(RectF, UiContext\)](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Arrange\_MicroEngine\_RectF\_MicroEngine\_UI\_UiContext\_), 
[Widget.Draw\(IRenderer, UiContext\)](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Draw\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_UI\_UiContext\_), 
[Widget.Bounds](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Bounds), 
[Widget.Visible](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Visible), 
[Widget.Enabled](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Enabled), 
[Widget.Width](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Width), 
[Widget.Height](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Height), 
[Widget.Offset](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Offset), 
[Widget.Opacity](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Opacity), 
[Widget.BlocksPointer](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_BlocksPointer), 
[Widget.Parent](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Parent), 
[Widget.Root](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Root), 
[Widget.Children](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Children), 
[Widget.Focusable](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Focusable), 
[Widget.ClipsChildren](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_ClipsChildren), 
[Widget.IsFocused](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_IsFocused), 
[Widget.IsHovered](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_IsHovered), 
[Widget.IsPressed](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_IsPressed), 
[Widget.IsEnabledInTree](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_IsEnabledInTree), 
[Widget.IsVisibleInTree](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_IsVisibleInTree), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_UI_Label__ctor_System_String_"></a> Label\(string\)

A label showing <code class="paramref">text</code>.

```csharp
public Label(string text = "")
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_UI_Label_Color"></a> Color

The text's colour; null (the default) is the theme's text colour, or its muted colour while disabled.

```csharp
public Color? Color { get; set; }
```

#### Property Value

 [Color](MicroEngine.Color.md)?

### <a id="MicroEngine_UI_Label_Lines"></a> Lines

The lines the text is drawn in, after wrapping, as of the last layout.

```csharp
public IReadOnlyList<string> Lines { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_UI_Label_Shadow"></a> Shadow

Whether the text has the theme's shadow under it (true by default).

```csharp
public bool Shadow { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Label_Text"></a> Text

The text; null counts as empty.

```csharp
public string Text { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_Label_VisibleCharacters"></a> VisibleCharacters

How many characters of [Text](MicroEngine.UI.Label.md#MicroEngine_UI_Label_Text) are drawn, counted in the text as written (spaces and <code>\n</code>
included); null (the default) draws all of it. The label keeps the size and the lines of its whole text, so a
text revealed a little at a time never moves. Raise it over time for a typewriter effect.

```csharp
public int? VisibleCharacters { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_UI_Label_VisibleLines"></a> VisibleLines

The part of each of [Lines](MicroEngine.UI.Label.md#MicroEngine_UI_Label_Lines) that [VisibleCharacters](MicroEngine.UI.Label.md#MicroEngine_UI_Label_VisibleCharacters) shows.

```csharp
public IReadOnlyList<string> VisibleLines { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_UI_Label_Wrap"></a> Wrap

Whether the text breaks between words so no line is wider than the label's fixed [Width](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Width)
(false by default); a word wider than that on its own is broken between characters. Without a fixed width the
text keeps its lines. The label grows in height to hold the lines.

```csharp
public bool Wrap { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_UI_Label_DrawContent_MicroEngine_Graphics_IRenderer_MicroEngine_UI_UiContext_"></a> DrawContent\(IRenderer, UiContext\)

Draws the widget in [Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds), and its children.

```csharp
protected override void DrawContent(IRenderer renderer, UiContext context)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`context` [UiContext](MicroEngine.UI.UiContext.md)

### <a id="MicroEngine_UI_Label_MeasureContent_MicroEngine_UI_UiContext_"></a> MeasureContent\(UiContext\)

The size the widget's content needs, in UI units.

```csharp
protected override Vector2 MeasureContent(UiContext context)
```

#### Parameters

`context` [UiContext](MicroEngine.UI.UiContext.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

</div>
