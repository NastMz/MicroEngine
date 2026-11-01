# Namespace MicroEngine.Scenes

The scene stack: scenes, transitions, coroutines and tweens.

Guide: [Scenes](../manual/scenes.md)  

### Classes

 [CoroutineHandle](MicroEngine.Scenes.CoroutineHandle.md)

Controls a coroutine started with [Start](MicroEngine.Scenes.CoroutineRunner.md#MicroEngine_Scenes_CoroutineRunner_Start_System_Collections_IEnumerator_).

 [CoroutineRunner](MicroEngine.Scenes.CoroutineRunner.md)

Runs iterator-based sequences one step per update. A coroutine may yield null (next update),
[Seconds](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Seconds_System_Single_), [Until](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Until_System_Func_System_Boolean__) or another IEnumerator (awaited as a sub-routine).
Ideal for cutscenes, dialogue flow and timed effects without hand-written state machines.

 [Scene](MicroEngine.Scenes.Scene.md)

A screen of the game (title, level, battle, pause menu...). Scenes live on a stack:
an overlay scene (dialogue, pause) draws on top of the scene below and can freeze it.
Each scene owns its coroutines and tweens, which stop when it leaves the stack.

 [SceneManager](MicroEngine.Scenes.SceneManager.md)

Manages the scene stack and the transitions between scenes.

 [Transition](MicroEngine.Scenes.Transition.md)

A full-screen effect that covers the old scene (progress 0..0.5), swaps scenes at 0.5 and
reveals the new one (0.5..1). Subclasses draw a cover amount between 0 (clear) and 1 (covered).

 [Tweener](MicroEngine.Scenes.Tweener.md)

Simple value tweens driven by update time.

 [Wait](MicroEngine.Scenes.Wait.md)

Yield instructions understood by [CoroutineRunner](MicroEngine.Scenes.CoroutineRunner.md).

 [WaitSeconds](MicroEngine.Scenes.WaitSeconds.md)

Yield instruction that pauses a coroutine for a number of seconds; create it with [Seconds](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Seconds_System_Single_).

 [WaitUntil](MicroEngine.Scenes.WaitUntil.md)

Yield instruction that pauses a coroutine until a condition holds; create it with [Until](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Until_System_Func_System_Boolean__).
