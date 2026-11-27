# Namespace MicroEngine.Scenes

<div data-pagefind-weight="0.3">

The scene stack (scenes, transitions, coroutines and tweens) and the scene world: entity ids, transforms with a hierarchy, typed attachments, interpolation for drawing and world clocks; and the spatial index a game creates to find what is near a place.

Guide: [Scenes](../manual/scenes.md)  

### Classes

 [CoroutineHandle](MicroEngine.Scenes.CoroutineHandle.md)

Controls a coroutine started with [Start](MicroEngine.Scenes.CoroutineRunner.md#MicroEngine_Scenes_CoroutineRunner_Start_System_Collections_IEnumerator_).

 [CoroutineRunner](MicroEngine.Scenes.CoroutineRunner.md)

Runs iterator-based sequences one step per update. A coroutine may yield null (next update),
[Seconds](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Seconds_System_Single_), [Steps](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Steps_System_Int32_), [Until](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Until_System_Func_System_Boolean__), a [Tween](MicroEngine.Scenes.Tween.md) (until it
ends) or another IEnumerator (awaited as a sub-routine).
Ideal for cutscenes, dialogue flow and timed effects without hand-written state machines.

 [Scene<TResult\>](MicroEngine.Scenes.Scene\-1.md)

A scene that ends by returning a value to whoever pushed it (a dialogue's choice, a shop's purchase, a menu's
answer): push it with [Push``1](MicroEngine.Scenes.SceneManager.md#MicroEngine_Scenes_SceneManager_Push__1_MicroEngine_Scenes_Scene___0__System_Action___0__MicroEngine_Scenes_Transition_) and
end it with [Finish](MicroEngine.Scenes.md).

 [SceneAudio](MicroEngine.Scenes.SceneAudio.md)

A scene's sounds in space ([Sounds](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Sounds)): a listener and emitters in the scene's world units. After
every [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) each emitter's volume follows its distance to the listener and its pan the side
it is on, moved there over the step so nothing clicks; an emitter that follows an entity goes where the entity
goes and stops when it is destroyed; every emitter stops when the scene leaves.

 [SceneManager](MicroEngine.Scenes.SceneManager.md)

Manages the scene stack and the transitions between scenes.

 [ScenePhysics](MicroEngine.Scenes.ScenePhysics.md)

A scene's physics world ([Physics](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Physics)): a scene system that steps the world after the scene's
[Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_), by the same step (the step on the scene world's clock, so slow motion slows physics and
a frozen or covered scene's physics stands still), writes the pose of every moving body to its owner's transform,
adds the world to the scene's state hash and disposes it when the scene leaves the stack.

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

Calls actions after a time or at intervals, on the scene's time ([Timers](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Timers)): the scene's own
[Timers](MicroEngine.Timers.md), stepped for it. Timers advance before the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) by the step it
receives, so they slow down and freeze with the world's clock; an unscaled timer advances by
[EngineStep](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_EngineStep) instead. They stop with the scene, and their state is part of its state hash. For a
sequence of steps over time use a coroutine ([Coroutines](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Coroutines)); for the rules of a game kept outside
a scene, create a [Timers](MicroEngine.Timers.md) and step it yourself.

 [SceneWorld](MicroEngine.Scenes.SceneWorld.md)

The engine's [IWorld](MicroEngine.Scenes.IWorld.md): entity slots with generations, local and step-start transforms, a parent/child
tree, and one dense array per attachment type. Games program against [IWorld](MicroEngine.Scenes.IWorld.md); whoever owns the world
(its scene) calls [BeginStep](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_BeginStep_System_Int64_System_Single_) before the world's step, [EndStep](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_EndStep) after it and
[BeginDraw](MicroEngine.Scenes.SceneWorld.md#MicroEngine_Scenes_SceneWorld_BeginDraw_System_Int64_System_Single_) before drawing it.

 [Scene](MicroEngine.Scenes.Scene.md)

A screen of the game (title, level, battle, pause menu...). Scenes live on a stack:
an overlay scene (dialogue, pause) draws on top of the scene below and can freeze it.
Each scene owns its coroutines, tweens and systems ([SceneSystem](MicroEngine.Scenes.SceneSystem.md)), which stop when it leaves the stack.

 [SoundEmitter](MicroEngine.Scenes.SoundEmitter.md)

A sound with a place in a scene's world ([Emit](MicroEngine.Scenes.SceneAudio.md#MicroEngine_Scenes_SceneAudio_Emit_System_String_MicroEngine_Scenes_EntityId_System_Boolean_System_Single_)): each step
its volume follows its distance to the listener and its pan its side of the listener. Its properties can be
changed while it plays.

 [SpatialIndex2D<T\>](MicroEngine.Scenes.SpatialIndex2D\-1.md)

Finds what is near a place: items of the game's own (entity ids, objects, numbers), each filed under a point or a
rectangle of the 2D world, asked for by rectangle or by circle without walking every item. A game creates one for
what it needs to find (enemies for the towers, neighbours for a swarm, what is in view in a world much larger than
the screen), moves an item's place when the item moves and removes it when it goes. A world has none unless the
game makes one, and the physics world is not involved: items need no body.

 [StateMachines](MicroEngine.Scenes.StateMachines.md)

Runs state machines for a scene ([Machines](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Machines)): each machine added is stepped every update before
the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_), with the same step, in the order they were added, and is part of the
scene's state hash. It suits the machines of the scene itself (the flow of a level, a boss fight's phases, a
cutscene). A machine that belongs to an object is better stepped by that object, where its place among the object's
rules is explicit; such a machine needs no runner, only a call to its <code>Update</code>.

 [Timeline](MicroEngine.Scenes.Timeline.md)

A playhead that drives several `Track`1`s together and raises named events at set times: a sequence
of movements with the moments the game reacts to (a footstep, the frame a swing lands). Advance it with
[Update](MicroEngine.Scenes.Timeline.md#MicroEngine_Scenes_Timeline_Update_System_Single_), like an <code>Animator</code>; several timelines over different values play as layers.

 [Transition](MicroEngine.Scenes.Transition.md)

A full-screen effect that covers the old scene (progress 0..0.5), swaps scenes at 0.5 and
reveals the new one (0.5..1). Subclasses draw a cover amount between 0 (clear) and 1 (covered).

 [Tween](MicroEngine.Scenes.Tween.md)

A tween added to a [Tweener](MicroEngine.Scenes.Tweener.md): what a coroutine yields to wait for it, and the way to end it early.

 [Tweener](MicroEngine.Scenes.Tweener.md)

Value tweens driven by update time: each calls an action with an eased progress from 0 to 1 over a length in
seconds or in updates. A scene has one ([Tweens](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Tweens)) and steps it; one you create is stepped with
[Update](MicroEngine.Scenes.Tweener.md#MicroEngine_Scenes_Tweener_Update_System_Single_).

 [WaitSeconds](MicroEngine.Scenes.WaitSeconds.md)

Yield instruction that pauses a coroutine for a number of seconds; create it with [Seconds](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Seconds_System_Single_).

 [WaitSteps](MicroEngine.Scenes.WaitSteps.md)

Yield instruction that pauses a coroutine for a number of updates; create it with [Steps](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Steps_System_Int32_).

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

 [ChildAttachments<T\>.Enumerator](MicroEngine.Scenes.ChildAttachments\-1.Enumerator.md)

Walks the attachments; allocates nothing.

 [ChildAttachments<T\>](MicroEngine.Scenes.ChildAttachments\-1.md)

The attachments of type <code class="typeparamref">T</code> of an entity's live direct children, in parenting order
([Children``1](MicroEngine.Scenes.IWorld.md#MicroEngine_Scenes_IWorld_Children__1_MicroEngine_Scenes_EntityId_)).

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

### Enums

 [SoundRolloff](MicroEngine.Scenes.SoundRolloff.md)

How a sound's volume falls between an emitter's [MinDistance](MicroEngine.Scenes.SoundEmitter.md#MicroEngine_Scenes_SoundEmitter_MinDistance) and its [MaxDistance](MicroEngine.Scenes.SoundEmitter.md#MicroEngine_Scenes_SoundEmitter_MaxDistance).

</div>
