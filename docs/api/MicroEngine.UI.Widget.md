# Class Widget

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

One element of a user interface, laid out in UI units in the UI view (window pixels divided by the presentation's UI
scale) by its [UiRoot](MicroEngine.UI.UiRoot.md): it says how large it wants to be ([Measure](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Measure_MicroEngine_UI_UiContext_)), is given its
[Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds) ([Arrange](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Arrange_MicroEngine_RectF_MicroEngine_UI_UiContext_)) and draws itself there ([Draw](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_UI_UiContext_)). Focusable widgets take the
focus, act on the engine's actions while they have it and on the pointer. Derive from it for a widget of a game's own.

```csharp
public abstract class Widget
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Widget](MicroEngine.UI.Widget.md)

#### Derived

[Button](MicroEngine.UI.Button.md), 
[Label](MicroEngine.UI.Label.md), 
[Panel](MicroEngine.UI.Panel.md), 
[ScrollView](MicroEngine.UI.ScrollView.md), 
[Stack](MicroEngine.UI.Stack.md), 
[TextField](MicroEngine.UI.TextField.md)

## Constructors

### <a id="MicroEngine_UI_Widget__ctor"></a> Widget\(\)

```csharp
protected Widget()
```

## Properties

### <a id="MicroEngine_UI_Widget_Bounds"></a> Bounds

Where the widget was placed by the last layout, in UI units.

```csharp
public RectF Bounds { get; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_UI_Widget_Children"></a> Children

The widgets this one contains, in focus and drawing order.

```csharp
public virtual IReadOnlyList<Widget> Children { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Widget](MicroEngine.UI.Widget.md)\>

### <a id="MicroEngine_UI_Widget_ClipsChildren"></a> ClipsChildren

Whether the widget restricts its children to its [Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds) (false by default): what they draw outside
is cut, and the pointer outside reaches them neither. A scrolling container does.

```csharp
public virtual bool ClipsChildren { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Widget_Enabled"></a> Enabled

Whether the widget and its children accept the focus, the pointer and actions (true by default); disabled widgets draw muted.

```csharp
public bool Enabled { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Widget_Focusable"></a> Focusable

Whether the widget can take the focus (false by default; buttons and text fields can).

```csharp
public virtual bool Focusable { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Widget_Height"></a> Height

A fixed height in UI units; null (the default) is the height the widget measures.

```csharp
public float? Height { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_UI_Widget_InterceptsDrag"></a> InterceptsDrag

Whether the widget takes a press that started on one of its children once the pointer moves further than a few
units (false by default). The child's press is then cancelled and the widget receives [OnPointerDown](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_OnPointerDown_System_Numerics_Vector2_)
at the press's start and the moves that follow: a scrolling container dragged by a finger that landed on a button.
A press on the widget itself, where no child takes it, is the widget's from the start.

```csharp
protected virtual bool InterceptsDrag { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Widget_IsEnabledInTree"></a> IsEnabledInTree

Whether this widget and every widget containing it are enabled.

```csharp
public bool IsEnabledInTree { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Widget_IsFocused"></a> IsFocused

Whether the widget has its root's focus.

```csharp
public bool IsFocused { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Widget_IsHovered"></a> IsHovered

Whether the pointer is over the widget.

```csharp
public bool IsHovered { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Widget_IsPressed"></a> IsPressed

Whether a pointer press that started on the widget is held.

```csharp
public bool IsPressed { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Widget_IsVisibleInTree"></a> IsVisibleInTree

Whether this widget and every widget containing it are visible.

```csharp
public bool IsVisibleInTree { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Widget_Parent"></a> Parent

The widget that contains this one, or null for a root's content or a widget not placed yet.

```csharp
public Widget? Parent { get; }
```

#### Property Value

 [Widget](MicroEngine.UI.Widget.md)?

### <a id="MicroEngine_UI_Widget_Root"></a> Root

The root the widget is shown by, or null while it is not part of one.

```csharp
public UiRoot? Root { get; }
```

#### Property Value

 [UiRoot](MicroEngine.UI.UiRoot.md)?

### <a id="MicroEngine_UI_Widget_Visible"></a> Visible

Whether the widget and its children are drawn, laid out and reachable (true by default).

```csharp
public bool Visible { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Widget_Width"></a> Width

A fixed width in UI units; null (the default) is the width the widget measures.

```csharp
public float? Width { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

## Methods

### <a id="MicroEngine_UI_Widget_Adopt_MicroEngine_UI_Widget_"></a> Adopt\(Widget\)

Makes <code class="paramref">child</code> a child of this widget (containers call it when a child is added).

```csharp
protected void Adopt(Widget child)
```

#### Parameters

`child` [Widget](MicroEngine.UI.Widget.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The child already belongs to another widget or root.

### <a id="MicroEngine_UI_Widget_Arrange_MicroEngine_RectF_MicroEngine_UI_UiContext_"></a> Arrange\(RectF, UiContext\)

Places the widget at <code class="paramref">bounds</code> (UI units) and its children within it.

```csharp
public void Arrange(RectF bounds, UiContext context)
```

#### Parameters

`bounds` [RectF](MicroEngine.RectF.md)

`context` [UiContext](MicroEngine.UI.UiContext.md)

### <a id="MicroEngine_UI_Widget_ArrangeContent_MicroEngine_RectF_MicroEngine_UI_UiContext_"></a> ArrangeContent\(RectF, UiContext\)

Places the children within <code class="paramref">bounds</code>; nothing by default.

```csharp
protected virtual void ArrangeContent(RectF bounds, UiContext context)
```

#### Parameters

`bounds` [RectF](MicroEngine.RectF.md)

`context` [UiContext](MicroEngine.UI.UiContext.md)

### <a id="MicroEngine_UI_Widget_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_UI_UiContext_"></a> Draw\(IRenderer, UiContext\)

Draws the widget in its [Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds); nothing while invisible.

```csharp
public void Draw(IRenderer renderer, UiContext context)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`context` [UiContext](MicroEngine.UI.UiContext.md)

### <a id="MicroEngine_UI_Widget_DrawContent_MicroEngine_Graphics_IRenderer_MicroEngine_UI_UiContext_"></a> DrawContent\(IRenderer, UiContext\)

Draws the widget in [Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds), and its children.

```csharp
protected abstract void DrawContent(IRenderer renderer, UiContext context)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`context` [UiContext](MicroEngine.UI.UiContext.md)

### <a id="MicroEngine_UI_Widget_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the widget's state that decides what happens next (a text field's text) to the scene's state hash; nothing by default.

```csharp
protected virtual void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_UI_Widget_Measure_MicroEngine_UI_UiContext_"></a> Measure\(UiContext\)

The size the widget wants, in UI units: [Width](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Width) and [Height](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Height) when set, else what it measures; zero while invisible.

```csharp
public Vector2 Measure(UiContext context)
```

#### Parameters

`context` [UiContext](MicroEngine.UI.UiContext.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_Widget_MeasureContent_MicroEngine_UI_UiContext_"></a> MeasureContent\(UiContext\)

The size the widget's content needs, in UI units.

```csharp
protected abstract Vector2 MeasureContent(UiContext context)
```

#### Parameters

`context` [UiContext](MicroEngine.UI.UiContext.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_Widget_OnAction_MicroEngine_UI_UiAction_"></a> OnAction\(UiAction\)

Called for an action while the widget has the focus; return true when it acted, which consumes the action.

```csharp
protected virtual bool OnAction(UiAction action)
```

#### Parameters

`action` [UiAction](MicroEngine.UI.UiAction.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Widget_OnClick"></a> OnClick\(\)

Called when a pointer press that started on the widget is released on it, after [OnPointerUp](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_OnPointerUp_System_Numerics_Vector2_).

```csharp
protected virtual void OnClick()
```

### <a id="MicroEngine_UI_Widget_OnFocusChanged_System_Boolean_"></a> OnFocusChanged\(bool\)

Called when the widget gains (true) or loses (false) the focus.

```csharp
protected virtual void OnFocusChanged(bool focused)
```

#### Parameters

`focused` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Widget_OnPointerCancel"></a> OnPointerCancel\(\)

Called when a press the widget holds is cancelled (the system took the touch, the widget left the tree, a container took it as a drag); no release or click follows.

```csharp
protected virtual void OnPointerCancel()
```

### <a id="MicroEngine_UI_Widget_OnPointerDown_System_Numerics_Vector2_"></a> OnPointerDown\(Vector2\)

Called when a pointer press starts on the widget (the focusable widget under the pointer, or its nearest focusable
container). <code class="paramref">position</code> is in UI units, like [Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds). The widget then holds the press:
it receives the press's moves and its release wherever the pointer goes.

```csharp
protected virtual void OnPointerDown(Vector2 position)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_Widget_OnPointerMove_System_Numerics_Vector2_"></a> OnPointerMove\(Vector2\)

Called when the pointer of a press the widget holds moves, inside the widget or not (dragging a slider's handle); <code class="paramref">position</code> is in UI units.

```csharp
protected virtual void OnPointerMove(Vector2 position)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_Widget_OnPointerUp_System_Numerics_Vector2_"></a> OnPointerUp\(Vector2\)

Called when a press the widget holds is released, inside the widget or not; <code class="paramref">position</code> is in UI units. A release on the widget then also calls [OnClick](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_OnClick).

```csharp
protected virtual void OnPointerUp(Vector2 position)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_Widget_OnWheel_System_Numerics_Vector2_"></a> OnWheel\(Vector2\)

Called for the wheel over the widget (<code class="paramref">delta</code> in notches: y positive away from the player, x positive
to the right). Return true when the widget used it; otherwise its container is asked, up to the root.

```csharp
protected virtual bool OnWheel(Vector2 delta)
```

#### Parameters

`delta` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Widget_Release_MicroEngine_UI_Widget_"></a> Release\(Widget\)

Detaches <code class="paramref">child</code> from this widget (containers call it when a child is removed).

```csharp
protected void Release(Widget child)
```

#### Parameters

`child` [Widget](MicroEngine.UI.Widget.md)

### <a id="MicroEngine_UI_Widget_Update_System_Single_"></a> Update\(float\)

Called once per update of the root, before actions are read.

```csharp
protected virtual void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
