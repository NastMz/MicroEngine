# Class CoroutineRunner

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

Runs iterator-based sequences one step per update. A coroutine may yield null (next update),
[Seconds](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Seconds_System_Single_), [Until](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Until_System_Func_System_Boolean__) or another IEnumerator (awaited as a sub-routine).
Ideal for cutscenes, dialogue flow and timed effects without hand-written state machines.

```csharp
public sealed class CoroutineRunner
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CoroutineRunner](MicroEngine.Scenes.CoroutineRunner.md)

## Constructors

### <a id="MicroEngine_Scenes_CoroutineRunner__ctor"></a> CoroutineRunner\(\)

```csharp
public CoroutineRunner()
```

## Properties

### <a id="MicroEngine_Scenes_CoroutineRunner_Count"></a> Count

Number of coroutines that are running or waiting to start.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Scenes_CoroutineRunner_Start_System_Collections_IEnumerator_"></a> Start\(IEnumerator\)

Starts a coroutine. Its first step runs on the next [Update](MicroEngine.Scenes.CoroutineRunner.md#MicroEngine_Scenes_CoroutineRunner_Update_System_Single_), not immediately.

```csharp
public CoroutineHandle Start(IEnumerator routine)
```

#### Parameters

`routine` [IEnumerator](https://learn.microsoft.com/dotnet/api/system.collections.ienumerator)

The iterator, usually from a method that uses <code>yield return</code>.

#### Returns

 [CoroutineHandle](MicroEngine.Scenes.CoroutineHandle.md)

A handle to cancel the coroutine or check whether it finished.

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">routine</code> is null.

### <a id="MicroEngine_Scenes_CoroutineRunner_StopAll"></a> StopAll\(\)

Stops every coroutine, including those started but not yet run.

```csharp
public void StopAll()
```

### <a id="MicroEngine_Scenes_CoroutineRunner_Update_System_Single_"></a> Update\(float\)

Advances every coroutine by one step. A scene calls this for its own [Coroutines](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Coroutines); call it yourself only for a runner you created.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

Elapsed time in seconds, consumed by [Seconds](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Seconds_System_Single_).

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A coroutine yielded a value other than null, a wait instruction or an `IEnumerator`.
