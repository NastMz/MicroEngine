# Class Button

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

A button: confirm while it has the focus, or a click (a press and release of the pointer on it), raises
[Clicked](MicroEngine.UI.Button.md#MicroEngine_UI_Button_Clicked). It takes the focus.

```csharp
public sealed class Button : Widget
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Widget](MicroEngine.UI.Widget.md) ← 
[Button](MicroEngine.UI.Button.md)

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
[Widget.FocusTime](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_FocusTime), 
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

### <a id="MicroEngine_UI_Button__ctor_System_String_System_Action_"></a> Button\(string, Action?\)

A button labelled <code class="paramref">text</code>, calling <code class="paramref">clicked</code> (optional) when clicked.

```csharp
public Button(string text, Action? clicked = null)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`clicked` [Action](https://learn.microsoft.com/dotnet/api/system.action)?

### <a id="MicroEngine_UI_Button__ctor_MicroEngine_Localization_LocalizedText_System_Action_"></a> Button\(LocalizedText, Action?\)

A button labelled with the text of a key of the game's string tables ([Localized](MicroEngine.UI.Button.md#MicroEngine_UI_Button_Localized)), calling <code class="paramref">clicked</code> (optional) when clicked.

```csharp
public Button(LocalizedText text, Action? clicked = null)
```

#### Parameters

`text` [LocalizedText](MicroEngine.Localization.LocalizedText.md)

`clicked` [Action](https://learn.microsoft.com/dotnet/api/system.action)?

## Properties

### <a id="MicroEngine_UI_Button_Focusable"></a> Focusable

Whether the widget can take the focus (false by default; buttons and text fields can).

```csharp
public override bool Focusable { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Button_Localized"></a> Localized

A key of the game's string tables: the button shows its text in the language in use and changes with it. Null (the default) is plain [Text](MicroEngine.UI.Button.md#MicroEngine_UI_Button_Text).

```csharp
public LocalizedText? Localized { get; set; }
```

#### Property Value

 [LocalizedText](MicroEngine.Localization.LocalizedText.md)?

### <a id="MicroEngine_UI_Button_Padding"></a> Padding

The room around the label in UI units, each side: X to its left and to its right, Y above and below. Null (the
default) is 6 and 4 layout units, the built-in box's. A skin without a box usually sets it: a row as tall as a
line of text is <code>new(5, 0)</code>.

```csharp
public Vector2? Padding { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

### <a id="MicroEngine_UI_Button_Skin"></a> Skin

Draws the button in the game's own look, from its state (the focus, the pointer, the press, the time it has had
the focus): a menu row that is a bar, an underline and a bobbing cursor, a tab, a card. Null (the default) is
the theme's ([ButtonSkin](MicroEngine.UI.UiTheme.md#MicroEngine_UI_UiTheme_ButtonSkin)), or the built-in box around the label. The button keeps its
behaviour and its size ([Padding](MicroEngine.UI.Button.md#MicroEngine_UI_Button_Padding)).

```csharp
public ControlSkin? Skin { get; set; }
```

#### Property Value

 [ControlSkin](MicroEngine.UI.ControlSkin.md)?

### <a id="MicroEngine_UI_Button_Text"></a> Text

The label; with [Localized](MicroEngine.UI.Button.md#MicroEngine_UI_Button_Localized) set, that key's text in the language in use as of the last layout. Setting it makes the label plain text again. Null counts as empty.

```csharp
public string Text { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_UI_Button_Click"></a> Click\(\)

Clicks the button as confirm or the pointer would: raises [Clicked](MicroEngine.UI.Button.md#MicroEngine_UI_Button_Clicked) unless it is disabled. Returns whether it did.

```csharp
public bool Click()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Button_DrawContent_MicroEngine_Graphics_IRenderer_MicroEngine_UI_UiContext_"></a> DrawContent\(IRenderer, UiContext\)

Draws the widget in [Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds), and its children.

```csharp
protected override void DrawContent(IRenderer renderer, UiContext context)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`context` [UiContext](MicroEngine.UI.UiContext.md)

### <a id="MicroEngine_UI_Button_MeasureContent_MicroEngine_UI_UiContext_"></a> MeasureContent\(UiContext\)

The size the widget's content needs, in UI units.

```csharp
protected override Vector2 MeasureContent(UiContext context)
```

#### Parameters

`context` [UiContext](MicroEngine.UI.UiContext.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_Button_OnAction_MicroEngine_UI_UiAction_"></a> OnAction\(UiAction\)

Called for an action while the widget has the focus; return true when it acted, which consumes the action.

```csharp
protected override bool OnAction(UiAction action)
```

#### Parameters

`action` [UiAction](MicroEngine.UI.UiAction.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Button_OnClick"></a> OnClick\(\)

Called when a pointer press that started on the widget is released on it, after [OnPointerUp](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_OnPointerUp_System_Numerics_Vector2_).

```csharp
protected override void OnClick()
```

### <a id="MicroEngine_UI_Button_Clicked"></a> Clicked

Raised when the button is clicked.

```csharp
public event Action? Clicked
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

</div>
