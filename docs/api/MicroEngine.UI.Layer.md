# Class Layer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

The whole UI view, with each child placed on its own: against an edge or a corner (a banner in a corner, a HUD
block), or at a point (a speech bubble over a character, a damage number, a marker that follows something in the
world). Children keep the size they measure; later children draw over earlier ones. Where no child is, the pointer
passes through to gameplay. Use it as a root's [Content](MicroEngine.UI.UiRoot.md#MicroEngine_UI_UiRoot_Content).

```csharp
public sealed class Layer : Widget
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Widget](MicroEngine.UI.Widget.md) ← 
[Layer](MicroEngine.UI.Layer.md)

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

### <a id="MicroEngine_UI_Layer__ctor"></a> Layer\(\)

```csharp
public Layer()
```

## Properties

### <a id="MicroEngine_UI_Layer_BlocksPointer"></a> BlocksPointer

The layer's empty areas are not the UI's: presses there reach gameplay.

```csharp
public override bool BlocksPointer { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Layer_Children"></a> Children

The widgets this one contains, in focus and drawing order.

```csharp
public override IReadOnlyList<Widget> Children { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Widget](MicroEngine.UI.Widget.md)\>

## Methods

### <a id="MicroEngine_UI_Layer_Add_MicroEngine_UI_Widget_MicroEngine_UI_Anchor_System_Numerics_Vector2_"></a> Add\(Widget, Anchor, Vector2\)

Adds <code class="paramref">child</code> with its <code class="paramref">anchor</code> point on the layer's same point, moved by
<code class="paramref">offset</code>: <code>Add(banner, Anchor.TopRight, new(-4, 4))</code> puts a banner 4 units from the top-right
corner. Returns the layer.

```csharp
public Layer Add(Widget child, Anchor anchor = Anchor.TopLeft, Vector2 offset = default)
```

#### Parameters

`child` [Widget](MicroEngine.UI.Widget.md)

`anchor` [Anchor](MicroEngine.UI.Anchor.md)

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [Layer](MicroEngine.UI.Layer.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The child already belongs to another widget or root.

### <a id="MicroEngine_UI_Layer_AddAt_MicroEngine_UI_Widget_System_Numerics_Vector2_MicroEngine_UI_Anchor_"></a> AddAt\(Widget, Vector2, Anchor\)

Adds <code class="paramref">child</code> with its <code class="paramref">pivot</code> point at <code class="paramref">point</code> (UI units from the
layer's top-left): <code>AddAt(bubble, head, Anchor.Bottom)</code> puts a bubble's bottom centre over a head. Returns the
layer.

```csharp
public Layer AddAt(Widget child, Vector2 point, Anchor pivot = Anchor.Center)
```

#### Parameters

`child` [Widget](MicroEngine.UI.Widget.md)

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`pivot` [Anchor](MicroEngine.UI.Anchor.md)

#### Returns

 [Layer](MicroEngine.UI.Layer.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The child already belongs to another widget or root.

### <a id="MicroEngine_UI_Layer_ArrangeContent_MicroEngine_RectF_MicroEngine_UI_UiContext_"></a> ArrangeContent\(RectF, UiContext\)

Places the children within <code class="paramref">bounds</code>; nothing by default.

```csharp
protected override void ArrangeContent(RectF bounds, UiContext context)
```

#### Parameters

`bounds` [RectF](MicroEngine.RectF.md)

`context` [UiContext](MicroEngine.UI.UiContext.md)

### <a id="MicroEngine_UI_Layer_DrawContent_MicroEngine_Graphics_IRenderer_MicroEngine_UI_UiContext_"></a> DrawContent\(IRenderer, UiContext\)

Draws the widget in [Bounds](MicroEngine.UI.Widget.md#MicroEngine_UI_Widget_Bounds), and its children.

```csharp
protected override void DrawContent(IRenderer renderer, UiContext context)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`context` [UiContext](MicroEngine.UI.UiContext.md)

### <a id="MicroEngine_UI_Layer_MeasureContent_MicroEngine_UI_UiContext_"></a> MeasureContent\(UiContext\)

The UI view's size: a layer fills it.

```csharp
protected override Vector2 MeasureContent(UiContext context)
```

#### Parameters

`context` [UiContext](MicroEngine.UI.UiContext.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_UI_Layer_Move_MicroEngine_UI_Widget_System_Numerics_Vector2_"></a> Move\(Widget, Vector2\)

Moves <code class="paramref">child</code>'s pivot to <code class="paramref">point</code>, keeping its pivot (the centre if it was placed
against an edge). Call it every update to follow something that moves.

```csharp
public void Move(Widget child, Vector2 point)
```

#### Parameters

`child` [Widget](MicroEngine.UI.Widget.md)

`point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The child is not in this layer.

### <a id="MicroEngine_UI_Layer_Place_MicroEngine_UI_Widget_MicroEngine_UI_Anchor_System_Numerics_Vector2_"></a> Place\(Widget, Anchor, Vector2\)

Places <code class="paramref">child</code> against the layer's <code class="paramref">anchor</code> point, as [Add](MicroEngine.UI.Layer.md#MicroEngine_UI_Layer_Add_MicroEngine_UI_Widget_MicroEngine_UI_Anchor_System_Numerics_Vector2_) does.

```csharp
public void Place(Widget child, Anchor anchor, Vector2 offset = default)
```

#### Parameters

`child` [Widget](MicroEngine.UI.Widget.md)

`anchor` [Anchor](MicroEngine.UI.Anchor.md)

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The child is not in this layer.

### <a id="MicroEngine_UI_Layer_Remove_MicroEngine_UI_Widget_"></a> Remove\(Widget\)

Removes <code class="paramref">child</code>; false when it was not in the layer.

```csharp
public bool Remove(Widget child)
```

#### Parameters

`child` [Widget](MicroEngine.UI.Widget.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
