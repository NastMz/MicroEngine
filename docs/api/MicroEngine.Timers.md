# Class Timers

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

Calls actions after a time or at intervals, counted in seconds or in updates. Whatever owns it calls
[Update](MicroEngine.Timers.md#MicroEngine_Timers_Update_System_Single_) once per step: a scene has one of its own (<code>Scene.Timers</code>), and the rules of a game
kept in a plain class (a stage, a board, a match that tests step without a scene) create theirs. Its state takes
part in the state hash.

```csharp
public sealed class Timers : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Timers](MicroEngine.Timers.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Examples

```csharp
public sealed class Stage
{
    private readonly Timers _timers = new();

    public void BossDefeated()
    {
        _timers.After(2.6f, BigExplosion);
        _timers.After(3.4f, Fanfare);
    }

    public void Update(float dt)
    {
        _timers.Update(dt);
        // the rules
    }

    public void HashState(StateHasher hash) => hash.Add(_timers);
}
```

## Remarks

<p>
A timer in seconds is called on the update on which the time since it was added reaches its delay. At a fixed rate
of R updates a second that is after exactly <code>seconds x R</code> updates when the product is a whole number
(0.5 seconds at 60: 30) and after the next whole number otherwise; a repeating timer keeps what is left over, so
its n-th call follows the same rule for <code>n x seconds</code>. A timer in updates counts the updates of more than zero
seconds.
</p>
<p>
Timers are called in the order they were added. A timer added by an action waits for the next update; an action may
cancel any timer, its own included.
</p>

## Constructors

### <a id="MicroEngine_Timers__ctor"></a> Timers\(\)

```csharp
public Timers()
```

## Properties

### <a id="MicroEngine_Timers_Count"></a> Count

How many timers are waiting.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Timers_After_System_Single_System_Action_System_Boolean_"></a> After\(float, Action, bool\)

Calls <code class="paramref">action</code> once, <code class="paramref">seconds</code> from now.

```csharp
public TimerHandle After(float seconds, Action action, bool unscaled = false)
```

#### Parameters

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

The delay; 0 calls it in the next update.

`action` [Action](https://learn.microsoft.com/dotnet/api/system.action)

What to call.

`unscaled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True to count the second clock of [Update](MicroEngine.Timers.md#MicroEngine_Timers_Update_System_Single_) (a scene's engine step), so the timer runs through slow motion and a frozen world.

#### Returns

 [TimerHandle](MicroEngine.TimerHandle.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">seconds</code> is negative or not a number.

### <a id="MicroEngine_Timers_AfterSteps_System_Int32_System_Action_System_Boolean_"></a> AfterSteps\(int, Action, bool\)

Calls <code class="paramref">action</code> once, on the <code class="paramref">steps</code>-th update from now: for a game that thinks in
frames, and for a hand-kept timer moved here without changing the update it fires on. It counts updates, not time:
slow motion does not stretch it, and an update of zero seconds (a frozen world) is not counted.

```csharp
public TimerHandle AfterSteps(int steps, Action action, bool unscaled = false)
```

#### Parameters

`steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The delay in updates; 0 and 1 both call it in the next update.

`action` [Action](https://learn.microsoft.com/dotnet/api/system.action)

What to call.

`unscaled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True to count the updates of the second clock of [Update](MicroEngine.Timers.md#MicroEngine_Timers_Update_System_Single_).

#### Returns

 [TimerHandle](MicroEngine.TimerHandle.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">steps</code> is negative.

### <a id="MicroEngine_Timers_Cancel_MicroEngine_TimerHandle_"></a> Cancel\(TimerHandle\)

Cancels a timer; false when it already ended or was cancelled. A timer may cancel itself from its action.

```csharp
public bool Cancel(TimerHandle handle)
```

#### Parameters

`handle` [TimerHandle](MicroEngine.TimerHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Timers_Clear"></a> Clear\(\)

Cancels every timer.

```csharp
public void Clear()
```

### <a id="MicroEngine_Timers_Every_System_Single_System_Action_System_Int32_System_Boolean_"></a> Every\(float, Action, int, bool\)

Calls <code class="paramref">action</code> every <code class="paramref">seconds</code>, the first time one interval from now.

```csharp
public TimerHandle Every(float seconds, Action action, int times = 0, bool unscaled = false)
```

#### Parameters

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

The interval, above zero. A step longer than the interval calls the action once per interval that passed.

`action` [Action](https://learn.microsoft.com/dotnet/api/system.action)

What to call.

`times` [int](https://learn.microsoft.com/dotnet/api/system.int32)

How many calls before the timer ends; 0 (the default) repeats until it is cancelled.

`unscaled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True to count the second clock of [Update](MicroEngine.Timers.md#MicroEngine_Timers_Update_System_Single_).

#### Returns

 [TimerHandle](MicroEngine.TimerHandle.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">seconds</code> is not above zero, or <code class="paramref">times</code> is negative.

### <a id="MicroEngine_Timers_EverySteps_System_Int32_System_Action_System_Int32_System_Boolean_"></a> EverySteps\(int, Action, int, bool\)

Calls <code class="paramref">action</code> every <code class="paramref">steps</code> updates, the first time that many updates from now.

```csharp
public TimerHandle EverySteps(int steps, Action action, int times = 0, bool unscaled = false)
```

#### Parameters

`steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The interval in updates, above zero.

`action` [Action](https://learn.microsoft.com/dotnet/api/system.action)

What to call.

`times` [int](https://learn.microsoft.com/dotnet/api/system.int32)

How many calls before the timer ends; 0 (the default) repeats until it is cancelled.

`unscaled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True to count the updates of the second clock of [Update](MicroEngine.Timers.md#MicroEngine_Timers_Update_System_Single_).

#### Returns

 [TimerHandle](MicroEngine.TimerHandle.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">steps</code> is not above zero, or <code class="paramref">times</code> is negative.

### <a id="MicroEngine_Timers_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the waiting timers to the state hash: for each, its number, what is left, its interval, its calls left and how it counts.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Timers_IsActive_MicroEngine_TimerHandle_"></a> IsActive\(TimerHandle\)

Whether <code class="paramref">handle</code> names a timer that is still waiting.

```csharp
public bool IsActive(TimerHandle handle)
```

#### Parameters

`handle` [TimerHandle](MicroEngine.TimerHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Timers_Remaining_MicroEngine_TimerHandle_"></a> Remaining\(TimerHandle\)

What is left before the next call of a timer: seconds for one added with [After](MicroEngine.Timers.md#MicroEngine_Timers_After_System_Single_System_Action_System_Boolean_) or [Every](MicroEngine.Timers.md#MicroEngine_Timers_Every_System_Single_System_Action_System_Int32_System_Boolean_),
updates for one added with [AfterSteps](MicroEngine.Timers.md#MicroEngine_Timers_AfterSteps_System_Int32_System_Action_System_Boolean_) or [EverySteps](MicroEngine.Timers.md#MicroEngine_Timers_EverySteps_System_Int32_System_Action_System_Int32_System_Boolean_); 0 when it ended or was cancelled.
With it the owner of a save writes what it needs to add the timer again when the save is loaded.

```csharp
public float Remaining(TimerHandle handle)
```

#### Parameters

`handle` [TimerHandle](MicroEngine.TimerHandle.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Timers_Update_System_Single_"></a> Update\(float\)

Advances every timer by one update of <code class="paramref">dt</code> seconds and calls those that are due, in the order they were added.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

The step in seconds; 0 advances nothing.

### <a id="MicroEngine_Timers_Update_System_Single_System_Single_"></a> Update\(float, float\)

Advances the timers on two clocks: <code class="paramref">dt</code> for the ordinary ones and <code class="paramref">unscaledDt</code>
for those added with <code>unscaled: true</code>. A scene gives its world's step and the engine's, so an unscaled timer
runs while the world is slowed or frozen.

```csharp
public void Update(float dt, float unscaledDt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

`unscaledDt` [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
