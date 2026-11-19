# Namespace MicroEngine.Scenes

<div data-pagefind-weight="0.3">

The scene stack (scenes, transitions, coroutines and tweens) and the scene world: entity ids, transforms with a hierarchy, typed attachments, interpolation for drawing and world clocks; and the spatial index a game creates to find what is near a place.

Guide: [Scenes](../manual/scenes.md)  

### Classes

 [CoroutineHandle](MicroEngine.Scenes.CoroutineHandle.md)

Controls a coroutine started with [Start](MicroEngine.Scenes.CoroutineRunner.md#MicroEngine_Scenes_CoroutineRunner_Start_System_Collections_IEnumerator_).

 [CoroutineRunner](MicroEngine.Scenes.CoroutineRunner.md)

Runs iterator-based sequences one step per update. A coroutine may yield null (next update),
[Seconds](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Seconds_System_Single_), [Until](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Until_System_Func_System_Boolean__) or another IEnumerator (awaited as a sub-routine).
Ideal for cutscenes, dialogue flow and timed effects without hand-written state machines.

 [Scene<TResult\>](MicroEngine.Scenes.Scene\-1.md)

A scene that ends by returning a value to whoever pushed it (a dialogue's choice, a shop's purchase, a menu's
answer): push it with [Push``1](MicroEngine.Scenes.SceneManager.md#MicroEngine_Scenes_SceneManager_Push__1_MicroEngine_Scenes_Scene___0__System_Action___0__MicroEngine_Scenes_Transition_) and
end it with [Finish](MicroEngine.Scenes.md).

 [SceneManager](MicroEngine.Scenes.SceneManager.md)

Manages the scene stack and the transitions between scenes.

 [SceneSystem](MicroEngine.Scenes.SceneSystem.md)

A part of a scene that lives with it: the engine starts it when the scene is on the stack, steps it with the scene
([Update](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_Update_System_Single_) before the scene's own update, [LateUpdate](MicroEngine.Scenes.SceneSystem.md#MicroEngine_Scenes_SceneSystem_LateUpdate_System_Single_) after it), adds its state to the
scene's state hash and stops it when the scene leaves, with no call from the scene's own <code>Update</code>,
<code>HashState</code> or <code>Exit</code>. Add one to [Systems](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Systems), or give every scene one through
[AddSystem``1](MicroEngine.Scenes.SceneManager.md#MicroEngine_Scenes_SceneManager_AddSystem__1_System_Func_MicroEngine_Scenes_Scene___0__). A component store, a physics world, a set of state machines and a
scheduler are scene systems.

 [SceneSystems](MicroEngine.Scenes.SceneSystems.md)

The systems of one scene ([Systems](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Systems)), one per type. The scene steps, hashes and stops them; see
[SceneSystem](MicroEngine.Scenes.SceneSystem.md).

 [SceneTimers](MicroEngine.Scenes.SceneTimers.md)

Calls actions after a time or at intervals, on the scene's time ([Timers](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Timers)). Timers advance before
the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) by the step it receives, so they slow down and freeze with the world's clock;
an unscaled timer advances by [EngineStep](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_EngineStep) instead. They stop with the scene, and their state is
part of its state hash. For a sequence of steps over time use a coroutine ([Coroutines](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Coroutines)).

 [SceneWorld](MicroEngine.Scenes.SceneWorld.md)

The engine's [IWorld](MicroEngine.Scenes.IWorld.md): entity slots with generations, local and step-start transforms, a parent/child
tree, and one dense array per attachment type. Games program against [IWorld](MicroEngine.Scenes.IWorld.md); whoever owns the world
(its scene) calls [BeginStep](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_BeginStep_System_Int64_System_Single_) before the world's step, [EndStep](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_EndStep) after it and
[BeginDraw](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_BeginDraw_System_Int64_System_Single_) before drawing it.

 [Scene](MicroEngine.Scenes.Scene.md)

A screen of the game (title, level, battle, pause menu...). Scenes live on a stack:
an overlay scene (dialogue, pause) draws on top of the scene below and can freeze it.
Each scene owns its coroutines, tweens and systems ([SceneSystem](MicroEngine.Scenes.SceneSystem.md)), which stop when it leaves the stack.

 [SpatialIndex2D<T\>](MicroEngine.Scenes.SpatialIndex2D\-1.md)

Finds what is near a place: items of the game's own (entity ids, objects, numbers), each filed under a point or a
rectangle of the 2D world, asked for by rectangle or by circle without walking every item. A game creates one for
what it needs to find (enemies for the towers, neighbours for a swarm, what is in view in a world much larger than
the screen), moves an item's place when the item moves and removes it when it goes. A world has none unless the
game makes one, and the physics world is not involved: items need no body.

 [Timeline](MicroEngine.Scenes.Timeline.md)

A playhead that drives several `Track`1`s together and raises named events at set times: a sequence
of movements with the moments the game reacts to (a footstep, the frame a swing lands). Advance it with
[Update](MicroEngine.Scenes.Timeline.md#MicroEngine_Scenes_Timeline_Update_System_Single_), like an <code>Animator</code>; several timelines over different values play as layers.

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

 [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

An item of a [SpatialIndex2D`1](MicroEngine.Scenes.md): a slot index and the generation of the item that held the slot when
the handle was made. A removed item's slot is reused with a new generation, so an old handle never reaches the new
item. <code>default</code> is [None](MicroEngine.Scenes.SpatialHandle.md#MicroEngine_Scenes_SpatialHandle_None) (generation 0 is never issued).

 [TimerHandle](MicroEngine.Scenes.TimerHandle.md)

A timer of a scene's [SceneTimers](MicroEngine.Scenes.SceneTimers.md); [None](MicroEngine.Scenes.TimerHandle.md#MicroEngine_Scenes_TimerHandle_None) names no timer.

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
