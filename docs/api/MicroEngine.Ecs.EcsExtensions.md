# Class EcsExtensions

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Ecs](MicroEngine.Ecs.md)  
Assembly: MicroEngine.Ecs.dll  
Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

The store from a scene, and for every scene of a game.

```csharp
public static class EcsExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EcsExtensions](MicroEngine.Ecs.EcsExtensions.md)

## Methods

### <a id="MicroEngine_Ecs_EcsExtensions_AddEcs_MicroEngine_Scenes_Scene_MicroEngine_Ecs_EcsComponents_"></a> AddEcs\(Scene, EcsComponents\)

Gives <code class="paramref">scene</code> a component store for rows of <code class="paramref">components</code> and returns it. Call it from the scene's constructor or its <code>Enter</code>.

```csharp
public static EcsStore AddEcs(this Scene scene, EcsComponents components)
```

#### Parameters

`scene` [Scene](MicroEngine.Scenes.Scene.md)

`components` [EcsComponents](MicroEngine.Ecs.EcsComponents.md)

#### Returns

 [EcsStore](MicroEngine.Ecs.EcsStore.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The scene already has a store.

### <a id="MicroEngine_Ecs_EcsExtensions_AddEcs_MicroEngine_Scenes_SceneManager_MicroEngine_Ecs_EcsComponents_System_Func_MicroEngine_Scenes_Scene_System_Boolean__"></a> AddEcs\(SceneManager, EcsComponents, Func<Scene, bool\>?\)

Gives every scene pushed from now on a component store for rows of <code class="paramref">components</code>, with no code
in the scenes: call it once, from the game's <code>Load</code>. A scene reads its store with [Ecs](MicroEngine.Ecs.EcsExtensions.md#MicroEngine_Ecs_EcsExtensions_Ecs_MicroEngine_Scenes_Scene_).

```csharp
public static void AddEcs(this SceneManager scenes, EcsComponents components, Func<Scene, bool>? where = null)
```

#### Parameters

`scenes` [SceneManager](MicroEngine.Scenes.SceneManager.md)

The game's scenes.

`components` [EcsComponents](MicroEngine.Ecs.EcsComponents.md)

The component types of the game.

`where` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[Scene](MicroEngine.Scenes.Scene.md), [bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>?

Which scenes get one (a menu or a pause overlay rarely needs it); every scene when null.

### <a id="MicroEngine_Ecs_EcsExtensions_Ecs_MicroEngine_Scenes_Scene_"></a> Ecs\(Scene\)

The scene's component store.

```csharp
public static EcsStore Ecs(this Scene scene)
```

#### Parameters

`scene` [Scene](MicroEngine.Scenes.Scene.md)

#### Returns

 [EcsStore](MicroEngine.Ecs.EcsStore.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The scene has none.

</div>
