# Class Entity

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.dll  
Guide: [Tile maps and collision](../manual/world.md)  

A lightweight game object: a position, a hitbox relative to it, and update/draw hooks.
MicroEngine deliberately prefers small inheritance-based entities over an ECS: the sample
games have dozens of objects, and plain classes keep game code readable and debuggable.

```csharp
public abstract class Entity
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Entity](MicroEngine.World.Entity.md)

## Constructors

### <a id="MicroEngine_World_Entity__ctor"></a> Entity\(\)

```csharp
protected Entity()
```

## Properties

### <a id="MicroEngine_World_Entity_Alive"></a> Alive

False after [Destroy](MicroEngine.World.Entity.md#MicroEngine_World_Entity_Destroy). Dead entities are skipped by their list and removed at its next flush.

```csharp
public bool Alive { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_Entity_Bounds"></a> Bounds

The hitbox in world pixels.

```csharp
public RectF Bounds { get; }
```

#### Property Value

 RectF

### <a id="MicroEngine_World_Entity_Center"></a> Center

The centre of [Bounds](MicroEngine.World.Entity.md#MicroEngine_World_Entity_Bounds).

```csharp
public Vector2 Center { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_World_Entity_Depth"></a> Depth

Draw order: lower first. Top-down games usually use the feet Y.

```csharp
public virtual float Depth { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_World_Entity_Hitbox"></a> Hitbox

Hitbox relative to [Position](MicroEngine.World.Entity.md#MicroEngine_World_Entity_Position).

```csharp
public RectF Hitbox { get; set; }
```

#### Property Value

 RectF

### <a id="MicroEngine_World_Entity_Position"></a> Position

World position in pixels; the hitbox and drawing are relative to it.

```csharp
public Vector2 Position { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_World_Entity_Destroy"></a> Destroy\(\)

Marks the entity dead; it stops updating and drawing and leaves its list (calling [Removed](MicroEngine.World.Entity.md#MicroEngine_World_Entity_Removed)) at the end of the list's update.

```csharp
public void Destroy()
```

### <a id="MicroEngine_World_Entity_Draw_MicroEngine_Graphics_IRenderer_"></a> Draw\(IRenderer\)

Draws the entity; called by [Draw](MicroEngine.World.md) while it is alive.

```csharp
public virtual void Draw(IRenderer renderer)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

### <a id="MicroEngine_World_Entity_Removed"></a> Removed\(\)

Called once when the entity leaves its list after [Destroy](MicroEngine.World.Entity.md#MicroEngine_World_Entity_Destroy).

```csharp
public virtual void Removed()
```

### <a id="MicroEngine_World_Entity_Update_System_Single_"></a> Update\(float\)

Advances the entity by <code class="paramref">dt</code> seconds; called by [Update](MicroEngine.World.md) while it is alive.

```csharp
public virtual void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
