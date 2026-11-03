# Namespace MicroEngine.Scenes

<div data-pagefind-weight="0.3">

The scene stack (scenes, transitions, coroutines and tweens) and the scene world: entity ids, transforms with a hierarchy, typed attachments, interpolation for drawing and world clocks.

Guide: [Scenes](../manual/scenes.md)  

### Classes

 [CoroutineHandle](MicroEngine.Scenes.CoroutineHandle.md)

Controls a coroutine started with [Start](MicroEngine.Scenes.CoroutineRunner.md#MicroEngine_Scenes_CoroutineRunner_Start_System_Collections_IEnumerator_).

 [CoroutineRunner](MicroEngine.Scenes.CoroutineRunner.md)

Runs iterator-based sequences one step per update. A coroutine may yield null (next update),
[Seconds](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Seconds_System_Single_), [Until](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Until_System_Func_System_Boolean__) or another IEnumerator (awaited as a sub-routine).
Ideal for cutscenes, dialogue flow and timed effects without hand-written state machines.

 [SceneManager](MicroEngine.Scenes.SceneManager.md)

Manages the scene stack and the transitions between scenes.

 [SceneWorld](MicroEngine.Scenes.SceneWorld.md)

The engine's [IWorld](MicroEngine.Scenes.IWorld.md): entity slots with generations, local and step-start transforms, a parent/child
tree, and one dense array per attachment type. Games program against [IWorld](MicroEngine.Scenes.IWorld.md); whoever owns the world
(its scene) calls [BeginStep](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_BeginStep_System_Int64_System_Single_) before the world's step, [EndStep](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_EndStep) after it and
[BeginDraw](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_BeginDraw_System_Int64_System_Single_) before drawing it.

 [Scene](MicroEngine.Scenes.Scene.md)

A screen of the game (title, level, battle, pause menu...). Scenes live on a stack:
an overlay scene (dialogue, pause) draws on top of the scene below and can freeze it.
Each scene owns its coroutines and tweens, which stop when it leaves the stack.

 [Transition](MicroEngine.Scenes.Transition.md)

A full-screen effect that covers the old scene (progress 0..0.5), swaps scenes at 0.5 and
reveals the new one (0.5..1). Subclasses draw a cover amount between 0 (clear) and 1 (covered).

 [Tweener](MicroEngine.Scenes.Tweener.md)

Simple value tweens driven by update time.

 [WaitSeconds](MicroEngine.Scenes.WaitSeconds.md)

Yield instruction that pauses a coroutine for a number of seconds; create it with [Seconds](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Seconds_System_Single_).

 [WaitUntil](MicroEngine.Scenes.WaitUntil.md)

Yield instruction that pauses a coroutine until a condition holds; create it with [Until](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Until_System_Func_System_Boolean__).

 [Wait](MicroEngine.Scenes.Wait.md)

Yield instructions understood by [CoroutineRunner](MicroEngine.Scenes.CoroutineRunner.md).

 [WorldClock](MicroEngine.Scenes.WorldClock.md)

A world's own time: a time scale and freezes (hitstop) on top of the engine's fixed step. It advances as part of its
world's step ([BeginStep](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_BeginStep_System_Int64_System_Single_)), once per step: a world that does not step (paused, under an
overlay) consumes no time and no freeze.

### Structs

 [Attachments<T\>.Enumerator](MicroEngine.Scenes.Attachments\-1.Enumerator.md)

Walks the attachments; allocates nothing.

 [Attachments<T\>](MicroEngine.Scenes.Attachments\-1.md)

The attachments of type <code class="typeparamref">T</code> of live entities, in attach order ([All``1](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_All__1)).

 [EntityChildren.Enumerator](MicroEngine.Scenes.EntityChildren.Enumerator.md)

Walks the children; allocates nothing.

 [EntityChildren](MicroEngine.Scenes.EntityChildren.md)

The live direct children of an entity, in parenting order ([Children](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Children_MicroEngine_Scenes_EntityId_)).

 [EntityId](MicroEngine.Scenes.EntityId.md)

An entity of an [IWorld](MicroEngine.Scenes.IWorld.md): a slot index and the generation of the entity that held the slot when the id
was made. A destroyed entity's slot is reused with a new generation, so an old id never reaches the new entity.
<code>default</code> is [None](MicroEngine.Scenes.EntityId.md#MicroEngine_Scenes_EntityId_None) (generation 0 is never issued).

 [Transform](MicroEngine.Scenes.Transform.md)

Position, rotation and scale relative to the parent (or to the world for a root entity). Stored in 3D; 2D games use
the <code>2D</code> helpers, which read and write X/Y and the rotation about Z. <code>default</code> is degenerate (zero scale and
a zero quaternion): entities start at [Identity](MicroEngine.Scenes.Transform.md#MicroEngine_Scenes_Transform_Identity).

### Interfaces

 [IDestroyedCallback](MicroEngine.Scenes.IDestroyedCallback.md)

An attachment told when its entity is released (at the end of the step its entity was destroyed in).

 [IWorld](MicroEngine.Scenes.IWorld.md)

The objects of a scene: entities with an id, a transform (with an optional parent) and typed attachments. The world
runs no game logic: it stores, identifies, orders, interpolates and destroys. Game code programs against this
interface; the scene that owns the world drives its step and draw hooks ([SceneWorld](MicroEngine.Scenes.SceneWorld.md)).

</div>
