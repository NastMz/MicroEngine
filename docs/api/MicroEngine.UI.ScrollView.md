# Class ScrollView

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

A window onto content longer than the space it has: a long menu, a list of saves, an achievement list, the credits.
Give it a fixed [Height](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Height) (or [Width](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Width) when [Orientation](MicroEngine.UI.ScrollView.md#MicroEngine_UI_ScrollView_Orientation) is
horizontal); its content keeps the size it measures and is cut at the view's edges. It scrolls to show a widget
inside it when that widget takes the focus, with the wheel over it, and with a drag, also one that starts on a button
inside it (a finger on a phone). A thin bar on its edge shows where the window is.

```csharp
public sealed class ScrollView : Widget
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Widget](MicroEngine.UI.Widget.md) ← 
[ScrollView](MicroEngine.UI.ScrollView.md)

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

### <a id="MicroEngine_UI_ScrollView__ctor_MicroEngine_UI_Widget_"></a> ScrollView\(Widget?\)

A view of <code class="paramref">content</code> (optional).

```csharp
public ScrollView(Widget? content = null)
```

#### Parameters

`content` [Widget](MicroEngine.UI.Widget.md)?

## Properties

### <a id="MicroEngine_UI_ScrollView_Children"></a> Children

The widgets this one contains, in focus and drawing order.

```csharp
public override IReadOnlyList<Widget> Children { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Widget](MicroEngine.UI.Widget.md)\>

### <a id="MicroEngine_UI_ScrollView_ClipsChildren"></a> ClipsChildren

Whether the widget restricts its children to its [Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds) (false by default): what they draw outside
is cut, and the pointer outside reaches them neither. A scrolling container does.

```csharp
public override bool ClipsChildren { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_ScrollView_Content"></a> Content

The widget scrolled, or null.

```csharp
public Widget? Content { get; set; }
```

#### Property Value

 [Widget](MicroEngine.UI.Widget.md)?

### <a id="MicroEngine_UI_ScrollView_InterceptsDrag"></a> InterceptsDrag

A drag inside the view scrolls it, while the content does not fit.

```csharp
protected override bool InterceptsDrag { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_ScrollView_MaxScroll"></a> MaxScroll

How far the content can scroll by the last layout: its length beyond the view's, or 0 when it fits.

```csharp
public float MaxScroll { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_ScrollView_Orientation"></a> Orientation

The direction the content scrolls in (vertical by default).

```csharp
public Orientation Orientation { get; set; }
```

#### Property Value

 [Orientation](MicroEngine.UI.Orientation.md)

### <a id="MicroEngine_UI_ScrollView_Scroll"></a> Scroll

How far the content is scrolled, in UI units, from 0 to [MaxScroll](MicroEngine.UI.ScrollView.md#MicroEngine_UI_ScrollView_MaxScroll).

```csharp
public float Scroll { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_ScrollView_ShowScrollBar"></a> ShowScrollBar

Whether the bar that shows the scroll position is drawn while the content does not fit (true by default).

```csharp
public bool ShowScrollBar { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_ScrollView_WheelStep"></a> WheelStep

How far one notch of the wheel scrolls, in UI units; null (the default) is three lines of the font.

```csharp
public float? WheelStep { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

## Methods

### <a id="MicroEngine_UI_ScrollView_ArrangeContent_MicroEngine_RectF_MicroEngine_UI_UiContext_"></a> ArrangeContent\(RectF, UiContext\)

Places the children within <code class="paramref">bounds</code>; nothing by default.

```csharp
protected override void ArrangeContent(RectF bounds, UiContext context)
```

#### Parameters

`bounds` [RectF](MicroEngine.RectF.md)

`context` [UiContext](MicroEngine.UI.UiContext.md)

### <a id="MicroEngine_UI_ScrollView_DrawContent_MicroEngine_Graphics_IRenderer_MicroEngine_UI_UiContext_"></a> DrawContent\(IRenderer, UiContext\)

Draws the widget in [Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds), and its children.

```csharp
protected override void DrawContent(IRenderer renderer, UiContext context)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`context` [UiContext](MicroEngine.UI.UiContext.md)

### <a id="MicroEngine_UI_ScrollView_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the widget's state that decides what happens next (a text field's text) to the scene's state hash; nothing by default.

```csharp
protected override void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_UI_ScrollView_MeasureContent_MicroEngine_UI_UiContext_"></a> MeasureContent\(UiContext\)

The content's size, and the scroll bar's strip when the content is longer than the fixed length.

```csharp
protected override Vector2 MeasureContent(UiContext context)
```

#### Parameters

`context` [UiContext](MicroEngine.UI.UiContext.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_ScrollView_OnPointerDown_System_Numerics_Vector2_"></a> OnPointerDown\(Vector2\)

Called when a pointer press starts on the widget (the focusable widget under the pointer, or its nearest focusable
container). <code class="paramref">position</code> is in UI units, like [Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds). The widget then holds the press:
it receives the press's moves and its release wherever the pointer goes.

```csharp
protected override void OnPointerDown(Vector2 position)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_ScrollView_OnPointerMove_System_Numerics_Vector2_"></a> OnPointerMove\(Vector2\)

The content follows the pointer.

```csharp
protected override void OnPointerMove(Vector2 position)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_ScrollView_OnWheel_System_Numerics_Vector2_"></a> OnWheel\(Vector2\)

Scrolls by the wheel: away from the player goes back towards the start.

```csharp
protected override bool OnWheel(Vector2 delta)
```

#### Parameters

`delta` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
