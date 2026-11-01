# Class SceneManager

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

Manages the scene stack and the transitions between scenes.

```csharp
public sealed class SceneManager
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SceneManager](MicroEngine.Scenes.SceneManager.md)

## Properties

### <a id="MicroEngine_Scenes_SceneManager_Count"></a> Count

Number of scenes on the stack.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_SceneManager_Current"></a> Current

The top scene, or null when the stack is empty.

```csharp
public Scene? Current { get; }
```

#### Property Value

 [Scene](MicroEngine.Scenes.Scene.md)?

### <a id="MicroEngine_Scenes_SceneManager_InTransition"></a> InTransition

Whether a transition is playing.

```csharp
public bool InTransition { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SceneManager_Stack"></a> Stack

The scenes from bottom (index 0) to top.

```csharp
public IReadOnlyList<Scene> Stack { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Scene](MicroEngine.Scenes.Scene.md)\>

## Methods

### <a id="MicroEngine_Scenes_SceneManager_Pop_MicroEngine_Scenes_Transition_"></a> Pop\(Transition?\)

Removes the top scene, deferred like [Push](MicroEngine.Scenes.SceneManager.md#MicroEngine_Scenes_SceneManager_Push_MicroEngine_Scenes_Scene_MicroEngine_Scenes_Transition_). The scene below receives [Resume](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Resume). Does nothing on an empty stack.

```csharp
public void Pop(Transition? transition = null)
```

#### Parameters

`transition` [Transition](MicroEngine.Scenes.Transition.md)?

### <a id="MicroEngine_Scenes_SceneManager_Push_MicroEngine_Scenes_Scene_MicroEngine_Scenes_Transition_"></a> Push\(Scene, Transition?\)

Puts <code class="paramref">scene</code> on top of the stack. The change is deferred: it applies before the next update (or
after [Game](MicroEngine.Core.Game.md)'s <code>Load</code>), or halfway through <code class="paramref">transition</code> when one is given.

```csharp
public void Push(Scene scene, Transition? transition = null)
```

#### Parameters

`scene` [Scene](MicroEngine.Scenes.Scene.md)

`transition` [Transition](MicroEngine.Scenes.Transition.md)?

#### Remarks

A change requested while a transition plays drops its own transition and applies like a change without one, even
before the playing transition swaps its scenes.

### <a id="MicroEngine_Scenes_SceneManager_Replace_MicroEngine_Scenes_Scene_MicroEngine_Scenes_Transition_"></a> Replace\(Scene, Transition?\)

Replaces the top scene.

```csharp
public void Replace(Scene scene, Transition? transition = null)
```

#### Parameters

`scene` [Scene](MicroEngine.Scenes.Scene.md)

`transition` [Transition](MicroEngine.Scenes.Transition.md)?

### <a id="MicroEngine_Scenes_SceneManager_Reset_MicroEngine_Scenes_Scene_MicroEngine_Scenes_Transition_"></a> Reset\(Scene, Transition?\)

Clears the stack and starts over with <code class="paramref">scene</code>.

```csharp
public void Reset(Scene scene, Transition? transition = null)
```

#### Parameters

`scene` [Scene](MicroEngine.Scenes.Scene.md)

`transition` [Transition](MicroEngine.Scenes.Transition.md)?
