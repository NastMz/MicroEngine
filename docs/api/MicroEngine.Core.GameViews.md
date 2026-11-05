# Class GameViews

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [The world view and the UI view](../manual/rendering.md#the-world-view-and-the-ui-view)  

The game's two views and the mapping between them ([Views](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Views)): the world view (the canvas, at the
canvas's resolution, placed in the window by the presentation) and the UI view (the window's resolution, in UI units),
drawn over it. Positions convert between window pixels, canvas pixels and UI units, and the pointer is given in each
view; the UI view gets it first ([PointerOnUi](MicroEngine.Core.GameViews.md#MicroEngine_Core_GameViews_PointerOnUi)).

```csharp
public sealed class GameViews
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GameViews](MicroEngine.Core.GameViews.md)

## Properties

### <a id="MicroEngine_Core_GameViews_CanvasSize"></a> CanvasSize

The canvas's size in pixels (the world view's resolution).

```csharp
public Vector2 CanvasSize { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_GameViews_HasScreenView"></a> HasScreenView

Whether the platform draws the UI view at the window's resolution (false: the UI view is the canvas).

```csharp
public bool HasScreenView { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GameViews_PointerOnUi"></a> PointerOnUi

Whether the pointer is over the UI of a visible scene ([UiContains](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_UiContains_System_Numerics_Vector2_)): the UI view gets the
pointer first, and [WorldPointer](MicroEngine.Core.GameViews.md#MicroEngine_Core_GameViews_WorldPointer_MicroEngine_Graphics_Camera2D_) is then not present.

```csharp
public bool PointerOnUi { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GameViews_ScreenSize"></a> ScreenSize

The window's size in pixels; the canvas's size where the platform has no UI view.

```csharp
public Vector2 ScreenSize { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_GameViews_UiPointer"></a> UiPointer

The pointer in UI units.

```csharp
public PointerState UiPointer { get; }
```

#### Property Value

 PointerState

### <a id="MicroEngine_Core_GameViews_UiScale"></a> UiScale

Window pixels per UI unit.

```csharp
public float UiScale { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Core_GameViews_UiSize"></a> UiSize

The UI view's size in UI units.

```csharp
public Vector2 UiSize { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_GameViews_WorldFrame"></a> WorldFrame

Where the canvas lands in the window, in window pixels.

```csharp
public RectF WorldFrame { get; }
```

#### Property Value

 RectF

## Methods

### <a id="MicroEngine_Core_GameViews_CanvasToScreen_System_Numerics_Vector2_"></a> CanvasToScreen\(Vector2\)

A canvas position in window pixels.

```csharp
public Vector2 CanvasToScreen(Vector2 canvas)
```

#### Parameters

`canvas` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_GameViews_CanvasToUi_System_Numerics_Vector2_"></a> CanvasToUi\(Vector2\)

A canvas position in UI units (to place a label over a world object).

```csharp
public Vector2 CanvasToUi(Vector2 canvas)
```

#### Parameters

`canvas` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_GameViews_ScreenToCanvas_System_Numerics_Vector2_"></a> ScreenToCanvas\(Vector2\)

A window position (pixels) in canvas pixels.

```csharp
public Vector2 ScreenToCanvas(Vector2 screen)
```

#### Parameters

`screen` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_GameViews_ScreenToUi_System_Numerics_Vector2_"></a> ScreenToUi\(Vector2\)

A window position (pixels) in UI units.

```csharp
public Vector2 ScreenToUi(Vector2 screen)
```

#### Parameters

`screen` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_GameViews_UiToCanvas_System_Numerics_Vector2_"></a> UiToCanvas\(Vector2\)

A UI position in canvas pixels.

```csharp
public Vector2 UiToCanvas(Vector2 ui)
```

#### Parameters

`ui` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_GameViews_UiToScreen_System_Numerics_Vector2_"></a> UiToScreen\(Vector2\)

A UI position in window pixels.

```csharp
public Vector2 UiToScreen(Vector2 ui)
```

#### Parameters

`ui` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_GameViews_WorldPointer_MicroEngine_Graphics_Camera2D_"></a> WorldPointer\(Camera2D?\)

The pointer in the world of <code class="paramref">camera</code> (canvas pixels without one); not present while it is over the
UI ([PointerOnUi](MicroEngine.Core.GameViews.md#MicroEngine_Core_GameViews_PointerOnUi)).

```csharp
public PointerState WorldPointer(Camera2D? camera = null)
```

#### Parameters

`camera` Camera2D?

#### Returns

 PointerState

</div>
