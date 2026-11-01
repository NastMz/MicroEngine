# Class CoroutineHandle

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

Controls a coroutine started with [Start](MicroEngine.Scenes.CoroutineRunner.md#MicroEngine_Scenes_CoroutineRunner_Start_System_Collections_IEnumerator_).

```csharp
public sealed class CoroutineHandle
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CoroutineHandle](MicroEngine.Scenes.CoroutineHandle.md)

## Constructors

### <a id="MicroEngine_Scenes_CoroutineHandle__ctor_System_Action_System_Func_System_Boolean__"></a> CoroutineHandle\(Action, Func<bool\>\)

Controls a coroutine started with [Start](MicroEngine.Scenes.CoroutineRunner.md#MicroEngine_Scenes_CoroutineRunner_Start_System_Collections_IEnumerator_).

```csharp
public CoroutineHandle(Action cancel, Func<bool> finished)
```

#### Parameters

`cancel` [Action](https://learn.microsoft.com/dotnet/api/system.action)

`finished` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

## Properties

### <a id="MicroEngine_Scenes_CoroutineHandle_IsFinished"></a> IsFinished

Whether the coroutine ran to its end or was cancelled.

```csharp
public bool IsFinished { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Scenes_CoroutineHandle_Cancel"></a> Cancel\(\)

Stops the coroutine; it does not run another step. Safe to call more than once.

```csharp
public void Cancel()
```
