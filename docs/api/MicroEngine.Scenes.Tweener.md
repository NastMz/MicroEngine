# Class Tweener

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

Simple value tweens driven by update time.

```csharp
public sealed class Tweener : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Tweener](MicroEngine.Scenes.Tweener.md)

#### Implements

IStateHashed

## Constructors

### <a id="MicroEngine_Scenes_Tweener__ctor"></a> Tweener\(\)

```csharp
public Tweener()
```

## Properties

### <a id="MicroEngine_Scenes_Tweener_Count"></a> Count

Number of tweens playing or waiting for their delay.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Scenes_Tweener_Add_System_Single_System_Action_System_Single__System_Func_System_Single_System_Single__System_Action_System_Single_"></a> Add\(float, Action<float\>, Func<float, float\>?, Action?, float\)

Calls <code class="paramref">apply</code> with eased progress 0..1 every update for <code class="paramref">duration</code> seconds.

```csharp
public void Add(float duration, Action<float> apply, Func<float, float>? ease = null, Action? done = null, float delay = 0)
```

#### Parameters

`duration` [float](https://learn.microsoft.com/dotnet/api/system.single)

`apply` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

`ease` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[float](https://learn.microsoft.com/dotnet/api/system.single), [float](https://learn.microsoft.com/dotnet/api/system.single)\>?

`done` [Action](https://learn.microsoft.com/dotnet/api/system.action)?

`delay` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_Tweener_Clear"></a> Clear\(\)

Removes every tween without applying its final value or calling its <code>done</code> callback.

```csharp
public void Clear()
```

### <a id="MicroEngine_Scenes_Tweener_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the tweens' state to the per-step state hash: for each tween, its duration, time played and delay left.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` StateHasher

### <a id="MicroEngine_Scenes_Tweener_Update_System_Single_"></a> Update\(float\)

Advances every tween by <code class="paramref">dt</code> seconds and calls the <code>done</code> callback of those that ended. A scene calls this for its own [Tweens](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Tweens).

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
