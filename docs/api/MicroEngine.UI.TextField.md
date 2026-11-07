# Class TextField

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

One line of editable text: a name, a seed, a chat message. While it has the focus it turns the input system's text
channel on (the on-screen keyboard on a phone, the input method's candidates next to the caret), inserts what is
typed at the caret, shows what an input method is still composing, and takes the keyboard from gameplay: Backspace,
Delete, the arrows, Home and End edit, Enter submits, Escape leaves the field, Tab and the up and down keys move the
focus. Shift with the arrows, Home or End selects, as does dragging the pointer across the text; Ctrl (Command on
macOS) with A selects everything, and with C, X and V copies, cuts and pastes through the root's
[Clipboard](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Clipboard). A gamepad edits nothing but moves the caret with left and right and submits with confirm.

```csharp
public sealed class TextField : Widget
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Widget](MicroEngine.UI.Widget.md) ← 
[TextField](MicroEngine.UI.TextField.md)

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

### <a id="MicroEngine_UI_TextField__ctor"></a> TextField\(\)

```csharp
public TextField()
```

## Properties

### <a id="MicroEngine_UI_TextField_Caret"></a> Caret

Where new text goes, as an index into [Text](MicroEngine.UI.TextField.md#MicroEngine_UI_TextField_Text); setting it ends any selection.

```csharp
public int Caret { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_TextField_Columns"></a> Columns

The width, in layout units, the field measures when [Width](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Width) is not set (96 by default).

```csharp
public float Columns { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_TextField_Composition"></a> Composition

The text an input method is composing and has not committed yet (empty when none); drawn underlined at the caret.

```csharp
public string Composition { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_TextField_Focusable"></a> Focusable

Whether the widget can take the focus (false by default; buttons and text fields can).

```csharp
public override bool Focusable { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_TextField_HasSelection"></a> HasSelection

Whether any text is selected.

```csharp
public bool HasSelection { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_TextField_MaxLength"></a> MaxLength

The most characters (UTF-16 code units) the text may have (64 by default).

```csharp
public int MaxLength { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is not positive.

### <a id="MicroEngine_UI_TextField_Placeholder"></a> Placeholder

Shown in the muted colour while the field is empty and does not have the focus.

```csharp
public string Placeholder { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_TextField_SelectedText"></a> SelectedText

The selected text, or "" when none is.

```csharp
public string SelectedText { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_TextField_SelectionLength"></a> SelectionLength

How many characters (UTF-16 code units) are selected; 0 when none are.

```csharp
public int SelectionLength { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_TextField_SelectionStart"></a> SelectionStart

Where the selection starts, as an index into [Text](MicroEngine.UI.TextField.md#MicroEngine_UI_TextField_Text): the end opposite the caret.

```csharp
public int SelectionStart { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_TextField_Text"></a> Text

The text; setting it cuts it to [MaxLength](MicroEngine.UI.TextField.md#MicroEngine_UI_TextField_MaxLength), puts the caret at its end and ends any selection.

```csharp
public string Text { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_UI_TextField_Copy"></a> Copy\(\)

Puts the selected text on the root's clipboard; false when nothing is selected or there is no clipboard.

```csharp
public bool Copy()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_TextField_Cut"></a> Cut\(\)

Puts the selected text on the root's clipboard and deletes it; false (and nothing deleted) when it could not copy.

```csharp
public bool Cut()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_TextField_DrawContent_MicroEngine_Graphics_IRenderer_MicroEngine_UI_UiContext_"></a> DrawContent\(IRenderer, UiContext\)

Draws the widget in [Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds), and its children.

```csharp
protected override void DrawContent(IRenderer renderer, UiContext context)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`context` [UiContext](MicroEngine.UI.UiContext.md)

### <a id="MicroEngine_UI_TextField_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the widget's state that decides what happens next (a text field's text) to the scene's state hash; nothing by default.

```csharp
protected override void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_UI_TextField_Insert_System_String_"></a> Insert\(string\)

Inserts <code class="paramref">text</code> at the caret, in place of the selection when there is one, as far as
[MaxLength](MicroEngine.UI.TextField.md#MicroEngine_UI_TextField_MaxLength) allows, and moves the caret after it.

```csharp
public void Insert(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_TextField_MeasureContent_MicroEngine_UI_UiContext_"></a> MeasureContent\(UiContext\)

The size the widget's content needs, in UI units.

```csharp
protected override Vector2 MeasureContent(UiContext context)
```

#### Parameters

`context` [UiContext](MicroEngine.UI.UiContext.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_TextField_OnAction_MicroEngine_UI_UiAction_"></a> OnAction\(UiAction\)

Called for an action while the widget has the focus; return true when it acted, which consumes the action.

```csharp
protected override bool OnAction(UiAction action)
```

#### Parameters

`action` [UiAction](MicroEngine.UI.UiAction.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_TextField_OnFocusChanged_System_Boolean_"></a> OnFocusChanged\(bool\)

Called when the widget gains (true) or loses (false) the focus.

```csharp
protected override void OnFocusChanged(bool focused)
```

#### Parameters

`focused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_TextField_OnPointerCancel"></a> OnPointerCancel\(\)

Called when a press the widget holds is cancelled (the system took the touch, the widget left the tree, a container took it as a drag); no release or click follows.

```csharp
protected override void OnPointerCancel()
```

### <a id="MicroEngine_UI_TextField_OnPointerDown_System_Numerics_Vector2_"></a> OnPointerDown\(Vector2\)

Called when a pointer press starts on the widget (the focusable widget under the pointer, or its nearest focusable
container). <code class="paramref">position</code> is in UI units, like [Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds). The widget then holds the press:
it receives the press's moves and its release wherever the pointer goes.

```csharp
protected override void OnPointerDown(Vector2 position)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_TextField_OnPointerMove_System_Numerics_Vector2_"></a> OnPointerMove\(Vector2\)

Called when the pointer of a press the widget holds moves, inside the widget or not (dragging a slider's handle); <code class="paramref">position</code> is in UI units.

```csharp
protected override void OnPointerMove(Vector2 position)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_TextField_OnPointerUp_System_Numerics_Vector2_"></a> OnPointerUp\(Vector2\)

Called when a press the widget holds is released, inside the widget or not; <code class="paramref">position</code> is in UI units. A release on the widget then also calls [OnClick](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_OnClick).

```csharp
protected override void OnPointerUp(Vector2 position)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_TextField_Paste"></a> Paste\(\)

Inserts the root's clipboard text at the caret, in place of the selection, as one line (line breaks and tabs
become spaces) cut to [MaxLength](MicroEngine.UI.TextField.md#MicroEngine_UI_TextField_MaxLength); false when there is no clipboard or it holds no text.

```csharp
public bool Paste()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_TextField_Select_System_Int32_System_Int32_"></a> Select\(int, int\)

Selects the text between <code class="paramref">anchor</code> and <code class="paramref">caret</code> (indices into [Text](MicroEngine.UI.TextField.md#MicroEngine_UI_TextField_Text),
clamped to it); the caret goes to <code class="paramref">caret</code>, so the selection grows from there.

```csharp
public void Select(int anchor, int caret)
```

#### Parameters

`anchor` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`caret` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_TextField_SelectAll"></a> SelectAll\(\)

Selects the whole text, with the caret at its end.

```csharp
public void SelectAll()
```

### <a id="MicroEngine_UI_TextField_Update_System_Single_"></a> Update\(float\)

Called once per update of the root, before actions are read.

```csharp
protected override void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_TextField_Changed"></a> Changed

Raised with the new text whenever it changes.

```csharp
public event Action<string>? Changed
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

### <a id="MicroEngine_UI_TextField_Submitted"></a> Submitted

Raised with the text when Enter or confirm submits it.

```csharp
public event Action<string>? Submitted
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

</div>
