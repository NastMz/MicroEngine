# Class Tween

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

A tween added to a [Tweener](MicroEngine.Scenes.Tweener.md): what a coroutine yields to wait for it, and the way to end it early.

```csharp
public sealed class Tween
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Tween](MicroEngine.Scenes.Tween.md)

## Examples

```csharp
IEnumerator Intro()
{
    yield return Tweens.Add(0.9f, t => _curtain = 1 - t, Ease.OutCubic);   // goes on when the curtain is open
    var bob = Tweens.Add(0.3f, t => _hero.Offset = new Vector2(0, -6 * t));
    yield return Wait.Until(() => Input.Pressed("action") || bob.IsFinished);
    bob.Complete();                                                         // skipped: jump to the end
}
```

## Properties

### <a id="MicroEngine_Scenes_Tween_IsFinished"></a> IsFinished

Whether the tween reached its end, was completed or was cancelled. A coroutine waiting for it goes on.

```csharp
public bool IsFinished { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_Tween_Progress"></a> Progress

How far along the tween is, from 0 to 1, before easing; 0 while it waits for its delay.

```csharp
public float Progress { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Scenes_Tween_Bind_MicroEngine_Scenes_IWorld_MicroEngine_Scenes_EntityId_"></a> Bind\(IWorld, EntityId\)

Ties the tween to an entity: once <code class="paramref">entity</code> is no longer alive in <code class="paramref">world</code> the
tween is cancelled instead of applying another value, so a tween that moves an object may outlive it.

```csharp
public Tween Bind(IWorld world, EntityId entity)
```

#### Parameters

`world` [IWorld](MicroEngine.Scenes.IWorld.md)

`entity` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [Tween](MicroEngine.Scenes.Tween.md)

The tween, to chain or yield.

### <a id="MicroEngine_Scenes_Tween_Cancel"></a> Cancel\(\)

Stops the tween where it is: no further value is applied and <code>done</code> is not called. Safe to call more than once.

```csharp
public void Cancel()
```

### <a id="MicroEngine_Scenes_Tween_Complete"></a> Complete\(\)

Ends the tween now: applies its final value and calls <code>done</code>, as reaching the end does (skipping a cutscene,
closing a menu mid-slide). Nothing happens when it already ended or was cancelled.

```csharp
public void Complete()
```

</div>
