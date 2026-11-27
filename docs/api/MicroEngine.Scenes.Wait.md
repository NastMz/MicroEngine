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

Pauses the coroutine for <code class="paramref">seconds</code> of update time. The wait counts down in single precision, as
a loop written by hand does: at R updates a second it lasts <code>seconds x R</code> updates, or one more when the
steps add up to a hair under the time (0.5 seconds at 60 is 30 updates, 1 second is 61). For a wait of an exact
number of updates use [Steps](MicroEngine.Scenes.Wait.md#MicroEngine_Scenes_Wait_Steps_System_Int32_).

```csharp
public static WaitSeconds Seconds(float seconds)
```

#### Parameters

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [WaitSeconds](MicroEngine.Scenes.WaitSeconds.md)

### <a id="MicroEngine_Scenes_Wait_Steps_System_Int32_"></a> Steps\(int\)

Pauses the coroutine for <code class="paramref">steps</code> updates: 1 is the next update, as yielding null; 0 does not pause.

```csharp
public static WaitSteps Steps(int steps)
```

#### Parameters

`steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [WaitSteps](MicroEngine.Scenes.WaitSteps.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">steps</code> is negative.

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
