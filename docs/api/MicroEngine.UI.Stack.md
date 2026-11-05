# Class Stack

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

Children placed one after another, top to bottom or left to right, with a space between them: menus, rows of buttons, forms.

```csharp
public sealed class Stack : Widget
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Widget](MicroEngine.UI.Widget.md) ← 
[Stack](MicroEngine.UI.Stack.md)

#### Inherited Members

[Widget.Measure\(UiContext\)](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Measure\_MicroEngine\_UI\_UiContext\_), 
[Widget.Arrange\(RectF, UiContext\)](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Arrange\_MicroEngine\_RectF\_MicroEngine\_UI\_UiContext\_), 
[Widget.Draw\(IRenderer, UiContext\)](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Draw\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_UI\_UiContext\_), 
[Widget.Bounds](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Bounds), 
[Widget.Visible](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Visible), 
[Widget.Enabled](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Enabled), 
[Widget.Width](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Width), 
[Widget.Height](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Height), 
[Widget.Parent](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Parent), 
[Widget.Root](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Root), 
[Widget.Children](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Children), 
[Widget.Focusable](MicroEngine.UI.Widget.md\#MicroEngine\_UI\_Widget\_Focusable), 
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

### <a id="MicroEngine_UI_Stack__ctor"></a> Stack\(\)

```csharp
public Stack()
```

## Properties

### <a id="MicroEngine_UI_Stack_Align"></a> Align

How children are placed across the stack ([Stretch](MicroEngine.UI.Align.md) by default: as wide as the stack, or as tall in a row).

```csharp
public Align Align { get; set; }
```

#### Property Value

 [Align](MicroEngine.UI.Align.md)

### <a id="MicroEngine_UI_Stack_Children"></a> Children

The widgets this one contains, in focus and drawing order.

```csharp
public override IReadOnlyList<Widget> Children { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Widget](MicroEngine.UI.Widget.md)\>

### <a id="MicroEngine_UI_Stack_Orientation"></a> Orientation

The direction children follow each other ([Vertical](MicroEngine.UI.Orientation.md) by default).

```csharp
public Orientation Orientation { get; set; }
```

#### Property Value

 [Orientation](MicroEngine.UI.Orientation.md)

### <a id="MicroEngine_UI_Stack_Spacing"></a> Spacing

Space between children in UI units; null (the default) is 4 layout units.

```csharp
public float? Spacing { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

## Methods

### <a id="MicroEngine_UI_Stack_Add_MicroEngine_UI_Widget_"></a> Add\(Widget\)

Adds <code class="paramref">child</code> after the others and returns the stack, so stacks can be written in one expression.

```csharp
public Stack Add(Widget child)
```

#### Parameters

`child` [Widget](MicroEngine.UI.Widget.md)

#### Returns

 [Stack](MicroEngine.UI.Stack.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The child already belongs to another widget or root.

### <a id="MicroEngine_UI_Stack_ArrangeContent_MicroEngine_RectF_MicroEngine_UI_UiContext_"></a> ArrangeContent\(RectF, UiContext\)

Places the children within <code class="paramref">bounds</code>; nothing by default.

```csharp
protected override void ArrangeContent(RectF bounds, UiContext context)
```

#### Parameters

`bounds` [RectF](MicroEngine.RectF.md)

`context` [UiContext](MicroEngine.UI.UiContext.md)

### <a id="MicroEngine_UI_Stack_DrawContent_MicroEngine_Graphics_IRenderer_MicroEngine_UI_UiContext_"></a> DrawContent\(IRenderer, UiContext\)

Draws the widget in [Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds), and its children.

```csharp
protected override void DrawContent(IRenderer renderer, UiContext context)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`context` [UiContext](MicroEngine.UI.UiContext.md)

### <a id="MicroEngine_UI_Stack_MeasureContent_MicroEngine_UI_UiContext_"></a> MeasureContent\(UiContext\)

The size the widget's content needs, in UI units.

```csharp
protected override Vector2 MeasureContent(UiContext context)
```

#### Parameters

`context` [UiContext](MicroEngine.UI.UiContext.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_Stack_Remove_MicroEngine_UI_Widget_"></a> Remove\(Widget\)

Removes <code class="paramref">child</code>; false when it was not in the stack.

```csharp
public bool Remove(Widget child)
```

#### Parameters

`child` [Widget](MicroEngine.UI.Widget.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
