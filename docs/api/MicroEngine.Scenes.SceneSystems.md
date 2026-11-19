# Class SceneSystems

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

The systems of one scene ([Systems](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Systems)), one per type. The scene steps, hashes and stops them; see
[SceneSystem](MicroEngine.Scenes.SceneSystem.md).

```csharp
public sealed class SceneSystems
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SceneSystems](MicroEngine.Scenes.SceneSystems.md)

## Properties

### <a id="MicroEngine_Scenes_SceneSystems_Count"></a> Count

How many systems the scene has.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Scenes_SceneSystems_Add__1___0_"></a> Add<T\>\(T\)

Adds <code class="paramref">system</code> to the scene and returns it. It starts at once when the scene is on the stack,
else when the scene is pushed. A system added while the scene's systems are being stepped takes part from the
next call on.

```csharp
public T Add<T>(T system) where T : SceneSystem
```

#### Parameters

`system` T

#### Returns

 T

#### Type Parameters

`T` 

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">system</code> is null.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The system is already in a scene, or the scene already has a system of that type.

### <a id="MicroEngine_Scenes_SceneSystems_Contains__1"></a> Contains<T\>\(\)

Whether the scene has a system of type <code class="typeparamref">T</code>.

```csharp
public bool Contains<T>() where T : SceneSystem
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

### <a id="MicroEngine_Scenes_SceneSystems_Get__1"></a> Get<T\>\(\)

The scene's system of type <code class="typeparamref">T</code> (that type or one derived from it).

```csharp
public T Get<T>() where T : SceneSystem
```

#### Returns

 T

#### Type Parameters

`T` 

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The scene has no such system.

### <a id="MicroEngine_Scenes_SceneSystems_GetOrAdd__1"></a> GetOrAdd<T\>\(\)

The scene's system of type <code class="typeparamref">T</code>, added with its parameterless constructor when the scene has none.

```csharp
public T GetOrAdd<T>() where T : SceneSystem, new()
```

#### Returns

 T

#### Type Parameters

`T` 

### <a id="MicroEngine_Scenes_SceneSystems_Remove_MicroEngine_Scenes_SceneSystem_"></a> Remove\(SceneSystem\)

Removes <code class="paramref">system</code> from the scene, stopping it when it had started; false when the scene does not have it.

```csharp
public bool Remove(SceneSystem system)
```

#### Parameters

`system` [SceneSystem](MicroEngine.Scenes.SceneSystem.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SceneSystems_TryGet__1___0__"></a> TryGet<T\>\(out T?\)

Finds the scene's system of type <code class="typeparamref">T</code> (that type or one derived from it).

```csharp
public bool TryGet<T>(out T? system) where T : SceneSystem
```

#### Parameters

`system` T?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T`

</div>
