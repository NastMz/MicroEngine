# Class DebugDraw

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Shapes and text any code can ask for while the game runs, drawn above the scenes at the end of the frame
([DebugDraw](MicroEngine.Core.Game.md#MicroEngine_Core_Game_DebugDraw)): a ray an enemy casts, the box a move swept, a path, a value next to an entity. Calls
do nothing while [Enabled](MicroEngine.Core.DebugDraw.md#MicroEngine_Core_DebugDraw_Enabled) is off, so they can stay in shipped code.

```csharp
public sealed class DebugDraw
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DebugDraw](MicroEngine.Core.DebugDraw.md)

## Examples

```csharp
// In Load:
DebugDraw.Enabled = true;
// Anywhere in an update:
game.DebugDraw.Line(enemy.Position, player.Position, Color.Red);
game.DebugDraw.Physics(world);
```

## Remarks

An item drawn with <code>seconds: 0</code> shows for one frame; ask for it every update to keep it. A longer one stays that
many seconds of real time. World items follow the camera of the scene on top ([WorldCamera](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_WorldCamera));
screen items are in the canvas's pixels. Lines are one world unit (or one canvas pixel) thick.

## Properties

### <a id="MicroEngine_Core_DebugDraw_Count"></a> Count

Items waiting to be drawn (the current frame's and the longer-lived ones).

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_DebugDraw_Enabled"></a> Enabled

Whether calls are recorded and drawn. Off by default; turning it off drops what is pending.

```csharp
public bool Enabled { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Core_DebugDraw_Circle_System_Numerics_Vector2_System_Single_MicroEngine_Color_System_Single_MicroEngine_Core_DebugSpace_"></a> Circle\(Vector2, float, Color, float, DebugSpace\)

The outline of a circle.

```csharp
public void Circle(Vector2 center, float radius, Color color, float seconds = 0, DebugSpace space = DebugSpace.World)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` [Color](MicroEngine.Color.md)

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

`space` [DebugSpace](MicroEngine.Core.DebugSpace.md)

### <a id="MicroEngine_Core_DebugDraw_Line_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Color_System_Single_MicroEngine_Core_DebugSpace_"></a> Line\(Vector2, Vector2, Color, float, DebugSpace\)

A line from <code class="paramref">from</code> to <code class="paramref">to</code>.

```csharp
public void Line(Vector2 from, Vector2 to, Color color, float seconds = 0, DebugSpace space = DebugSpace.World)
```

#### Parameters

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

`space` [DebugSpace](MicroEngine.Core.DebugSpace.md)

### <a id="MicroEngine_Core_DebugDraw_Physics_MicroEngine_Physics_IPhysicsWorld2D_System_Nullable_MicroEngine_RectF__"></a> Physics\(IPhysicsWorld2D, RectF?\)

Draws <code class="paramref">world</code> as it is now: every shape of every body (coloured by body kind; sensors and
one-way shapes in their own colour, [PhysicsColors](MicroEngine.Core.DebugDraw.PhysicsColors.md)), the solid and one-way tiles of tile grids inside
<code class="paramref">tileArea</code> (by default what the current scene's camera shows, or the canvas), and a normal for
each contact that began during the latest step. Does nothing while [Enabled](MicroEngine.Core.DebugDraw.md#MicroEngine_Core_DebugDraw_Enabled) is off.

```csharp
public void Physics(IPhysicsWorld2D world, RectF? tileArea = null)
```

#### Parameters

`world` [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md)

`tileArea` [RectF](MicroEngine.RectF.md)?

#### Exceptions

 [PhysicsCapabilityException](MicroEngine.Physics.PhysicsCapabilityException.md)

The backend does not declare [ShapeInspection](MicroEngine.Physics.PhysicsCapabilities.md).

### <a id="MicroEngine_Core_DebugDraw_Point_System_Numerics_Vector2_MicroEngine_Color_System_Single_System_Single_MicroEngine_Core_DebugSpace_"></a> Point\(Vector2, Color, float, float, DebugSpace\)

A small cross at <code class="paramref">position</code> (<code class="paramref">size</code> wide).

```csharp
public void Point(Vector2 position, Color color, float size = 4, float seconds = 0, DebugSpace space = DebugSpace.World)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

`size` [float](https://learn.microsoft.com/dotnet/api/system.single)

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

`space` [DebugSpace](MicroEngine.Core.DebugSpace.md)

### <a id="MicroEngine_Core_DebugDraw_Rect_MicroEngine_RectF_MicroEngine_Color_System_Single_MicroEngine_Core_DebugSpace_"></a> Rect\(RectF, Color, float, DebugSpace\)

The outline of <code class="paramref">rect</code>.

```csharp
public void Rect(RectF rect, Color color, float seconds = 0, DebugSpace space = DebugSpace.World)
```

#### Parameters

`rect` [RectF](MicroEngine.RectF.md)

`color` [Color](MicroEngine.Color.md)

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

`space` [DebugSpace](MicroEngine.Core.DebugSpace.md)

### <a id="MicroEngine_Core_DebugDraw_Text_System_Numerics_Vector2_System_String_MicroEngine_Color_System_Single_MicroEngine_Core_DebugSpace_"></a> Text\(Vector2, string, Color, float, DebugSpace\)

<code class="paramref">text</code> in the game's font ([Font](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Font)), its top-left corner at <code class="paramref">position</code>.

```csharp
public void Text(Vector2 position, string text, Color color, float seconds = 0, DebugSpace space = DebugSpace.World)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`color` [Color](MicroEngine.Color.md)

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

`space` [DebugSpace](MicroEngine.Core.DebugSpace.md)

</div>
