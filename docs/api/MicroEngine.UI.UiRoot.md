# Class UiRoot

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

Shows one widget tree in the UI view and drives it: lays [Content](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Content) out in UI units at
[Anchor](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Anchor), keeps the focus, moves it with the engine's direction actions, gives the focused widget confirm
and cancel, and, as the highest input layer ([UI](MicroEngine.Input.InputLayerOrder.md#MicroEngine_Input_InputLayerOrder_UI)), takes pointer presses that land on
the content and, while a text field has the focus, the keyboard. What it takes never reaches gameplay: a press on a
button is not a press in the world, and the actions it acts on are consumed ([Consume](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Consume_System_String_)), so the
scene's update, which runs after the root's, does not see them. A scene has one in <code>Scene.Widgets</code>, which the engine
updates, draws and routes input to; a root can also be driven by hand ([Update](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Update_System_Single_), [Draw](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Draw_MicroEngine_Graphics_IRenderer_),
[Handle](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Handle_MicroEngine_Input_InputEvent__)).

```csharp
public sealed class UiRoot : IInputLayer, IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[UiRoot](MicroEngine.UI.UiRoot.md)

#### Implements

[IInputLayer](MicroEngine.Input.IInputLayer.md), 
[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Constructors

### <a id="MicroEngine_UI_UiRoot__ctor_MicroEngine_Input_InputMap_MicroEngine_Input_TextInput_"></a> UiRoot\(InputMap, TextInput?\)

A root reading actions from <code class="paramref">input</code> and typed text from <code class="paramref">text</code> (null: text fields cannot be typed in).

```csharp
public UiRoot(InputMap input, TextInput? text = null)
```

#### Parameters

`input` [InputMap](MicroEngine.Input.InputMap.md)

`text` [TextInput](MicroEngine.Input.TextInput.md)?

## Properties

### <a id="MicroEngine_UI_UiRoot_Actions"></a> Actions

The actions the widgets answer to ([Default](MicroEngine.UI.UiActions.md#MicroEngine_UI_UiActions_Default): the input map's names).

```csharp
public UiActions Actions { get; set; }
```

#### Property Value

 [UiActions](MicroEngine.UI.UiActions.md)

### <a id="MicroEngine_UI_UiRoot_AllWidgets"></a> AllWidgets

The visible widgets of the content in focus and drawing order (depth first).

```csharp
public IEnumerable<Widget> AllWidgets { get; }
```

#### Property Value

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[Widget](MicroEngine.UI.Widget.md)\>

### <a id="MicroEngine_UI_UiRoot_Anchor"></a> Anchor

The point of the UI view the content is placed at ([Center](MicroEngine.UI.Anchor.md) by default).

```csharp
public Anchor Anchor { get; set; }
```

#### Property Value

 [Anchor](MicroEngine.UI.Anchor.md)

### <a id="MicroEngine_UI_UiRoot_AutoFocus"></a> AutoFocus

Whether the first focusable widget takes the focus when nothing has it, so a keyboard or a gamepad can use the UI at once (true by default).

```csharp
public bool AutoFocus { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiRoot_Content"></a> Content

The widget tree shown; setting it drops the focus, the hover and any press.

```csharp
public Widget? Content { get; set; }
```

#### Property Value

 [Widget](MicroEngine.UI.Widget.md)?

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The widget already belongs to another widget or root.

### <a id="MicroEngine_UI_UiRoot_Focused"></a> Focused

The widget with the focus, or null.

```csharp
public Widget? Focused { get; }
```

#### Property Value

 [Widget](MicroEngine.UI.Widget.md)?

### <a id="MicroEngine_UI_UiRoot_Font"></a> Font

The font widgets measure and draw text with. The engine sets the game's font; it must be set before drawing.

```csharp
public IFont? Font { get; set; }
```

#### Property Value

 [IFont](MicroEngine.Graphics.IFont.md)?

### <a id="MicroEngine_UI_UiRoot_Hovered"></a> Hovered

The widget under the pointer, or null.

```csharp
public Widget? Hovered { get; }
```

#### Property Value

 [Widget](MicroEngine.UI.Widget.md)?

### <a id="MicroEngine_UI_UiRoot_Offset"></a> Offset

Moves the content from its anchor, in UI units.

```csharp
public Vector2 Offset { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_UiRoot_PointerToUi"></a> PointerToUi

Maps a pointer event's position (virtual canvas pixels, [Position](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_Position)) into UI units. The engine
sets it to the game's view mapping; the identity by default (UI units are canvas pixels).

```csharp
public Func<Vector2, Vector2> PointerToUi { get; set; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2), [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\>

### <a id="MicroEngine_UI_UiRoot_Pressed"></a> Pressed

The widget a held pointer press started on, or null.

```csharp
public Widget? Pressed { get; }
```

#### Property Value

 [Widget](MicroEngine.UI.Widget.md)?

### <a id="MicroEngine_UI_UiRoot_Size"></a> Size

The size of the UI view in UI units: set by every [Draw](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Draw_MicroEngine_Graphics_IRenderer_) from the renderer's size, and by the engine before each update.

```csharp
public Vector2 Size { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_UiRoot_TextInput"></a> TextInput

The text channel text fields type through, or null.

```csharp
public TextInput? TextInput { get; }
```

#### Property Value

 [TextInput](MicroEngine.Input.TextInput.md)?

### <a id="MicroEngine_UI_UiRoot_Theme"></a> Theme

The colours ([Default](MicroEngine.UI.UiTheme.md#MicroEngine_UI_UiTheme_Default) by default).

```csharp
public UiTheme Theme { get; set; }
```

#### Property Value

 [UiTheme](MicroEngine.UI.UiTheme.md)

### <a id="MicroEngine_UI_UiRoot_Time"></a> Time

Seconds the root has been updated.

```csharp
public float Time { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_UiRoot_UiToCanvas"></a> UiToCanvas

Maps a UI position into virtual canvas pixels, for the caret an input method shows its candidates by; the identity by default.

```csharp
public Func<Vector2, Vector2> UiToCanvas { get; set; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2), [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\>

### <a id="MicroEngine_UI_UiRoot_WrapFocus"></a> WrapFocus

Whether moving the focus past the last widget in a direction continues from the other side (true by default).

```csharp
public bool WrapFocus { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_UI_UiRoot_Contains_System_Numerics_Vector2_"></a> Contains\(Vector2\)

Whether <code class="paramref">uiPoint</code> (UI units) lies on the content, where the root takes the pointer.

```csharp
public bool Contains(Vector2 uiPoint)
```

#### Parameters

`uiPoint` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiRoot_Draw_MicroEngine_Graphics_IRenderer_"></a> Draw\(IRenderer\)

Lays the content out for <code class="paramref">ui</code>'s size (UI units) and draws it there.

```csharp
public void Draw(IRenderer ui)
```

#### Parameters

`ui` [IRenderer](MicroEngine.Graphics.IRenderer.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

[Font](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Font) is not set.

### <a id="MicroEngine_UI_UiRoot_Focus_MicroEngine_UI_Widget_"></a> Focus\(Widget?\)

Gives <code class="paramref">widget</code> the focus (null removes it); false when it cannot take it (not focusable, disabled, hidden or not in this root).

```csharp
public bool Focus(Widget? widget)
```

#### Parameters

`widget` [Widget](MicroEngine.UI.Widget.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiRoot_Handle_MicroEngine_Input_InputEvent__"></a> Handle\(in InputEvent\)

Takes an event as the UI's input layer (see [IInputLayer](MicroEngine.Input.IInputLayer.md)): a pointer press on the content, everything
of a press it took, the wheel over the content, and keys while a text field has the focus. Taken events are
applied at the next [Update](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Update_System_Single_). Pointer moves are watched (for the hover) but left to gameplay, and text
goes to the text channel.

```csharp
public bool Handle(in InputEvent e)
```

#### Parameters

`e` [InputEvent](MicroEngine.Input.InputEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiRoot_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the focus and the widgets' state (in tree order) to the scene's state hash.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_UI_UiRoot_Layout"></a> Layout\(\)

Measures the content and places it in the UI view ([Size](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Size)) at [Anchor](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Anchor).

```csharp
public void Layout()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

[Font](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Font) is not set.

### <a id="MicroEngine_UI_UiRoot_MoveFocus_MicroEngine_UI_UiAction_"></a> MoveFocus\(UiAction\)

Moves the focus to the nearest focusable widget in <code class="paramref">direction</code> (up, down, left or right) by the
widgets' last layout; past the last one it continues from the other side when [WrapFocus](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_WrapFocus) is on.
Returns whether the focus moved.

```csharp
public bool MoveFocus(UiAction direction)
```

#### Parameters

`direction` [UiAction](MicroEngine.UI.UiAction.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_UiRoot_Update_System_Single_"></a> Update\(float\)

Advances the UI by one update: lays it out, applies the pointer events and keys it took since the last update,
updates the widgets, then gives the actions to the focused widget and to focus navigation, consuming those it acts
on. The engine calls it before the scene's own update.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_UiRoot_WidgetAt_System_Numerics_Vector2_"></a> WidgetAt\(Vector2\)

The deepest visible widget under <code class="paramref">uiPoint</code>, or null; the last one drawn where several overlap.
Children of a widget that clips them ([ClipsChildren](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_ClipsChildren)) count only inside its bounds.

```csharp
public Widget? WidgetAt(Vector2 uiPoint)
```

#### Parameters

`uiPoint` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [Widget](MicroEngine.UI.Widget.md)?

### <a id="MicroEngine_UI_UiRoot_Cancelled"></a> Cancelled

Raised when cancel is pressed and no focused widget used it; the press is then consumed (for example to close a menu).

```csharp
public event Action? Cancelled
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

</div>
