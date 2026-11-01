# Class Wait

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

Yield instructions understood by [CoroutineRunner](MicroEngine.Scenes.CoroutineRunner.md).

```csharp
public static class Wait
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Wait](MicroEngine.Scenes.Wait.md)

## Methods

### <a id="MicroEngine_Scenes_Wait_Seconds_System_Single_"></a> Seconds\(float\)

Pauses the coroutine for <code class="paramref">seconds</code> of update time.

```csharp
public static WaitSeconds Seconds(float seconds)
```

#### Parameters

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [WaitSeconds](MicroEngine.Scenes.WaitSeconds.md)

### <a id="MicroEngine_Scenes_Wait_Until_System_Func_System_Boolean__"></a> Until\(Func<bool\>\)

Pauses the coroutine until <code class="paramref">condition</code> returns true; checked once per update.

```csharp
public static WaitUntil Until(Func<bool> condition)
```

#### Parameters

`condition` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

#### Returns

 [WaitUntil](MicroEngine.Scenes.WaitUntil.md)

</div>
