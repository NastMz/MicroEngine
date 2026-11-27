# Class Tweener

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

Value tweens driven by update time: each calls an action with an eased progress from 0 to 1 over a length in
seconds or in updates. A scene has one ([Tweens](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Tweens)) and steps it; one you create is stepped with
[Update](MicroEngine.Scenes.Tweener.md#MicroEngine_Scenes_Tweener_Update_System_Single_).

```csharp
public sealed class Tweener : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Tweener](MicroEngine.Scenes.Tweener.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Remarks

<p>
A tween applies its starting value (<code>ease(0)</code>) the moment it is added, so what it moves is in place before
anything is drawn, and advances once per update from the next one. With a delay, it leaves its target alone until
the update the delay ends on, which applies the starting value. Its last update applies <code>ease(1)</code> and then
calls <code>done</code>. A tween of zero seconds or zero updates applies its final value on the next update.
</p>
<p>
An update whose step is zero, negative, infinite or not a number advances nothing: no value is applied and no tween
ends. A callback (<code>apply</code> or <code>done</code>) may add tweens, cancel or complete any tween, its own included, and
clear them all.
</p>
<p>
A tween in seconds adds each update's step to its time in single precision, exactly as the loop
<code>for (t = 0; t &lt; seconds; t += step)</code> does, and applies <code>ease(t / seconds)</code>: it replaces such a loop value
for value. At R updates a second it lasts <code>seconds x R</code> updates, or one more when the steps add up to a hair
under the length (0.5 seconds at 60 is 30 updates; 0.9 is 55, the 54th value being a hair under 1). A tween in steps
([AddSteps](MicroEngine.Scenes.Tweener.md#MicroEngine_Scenes_Tweener_AddSteps_System_Int32_System_Action_System_Single__System_Func_System_Single_System_Single__System_Action_System_Int32_System_Boolean_)) lasts exactly its number of updates.
</p>

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

### <a id="MicroEngine_Scenes_Tweener_Add_System_Single_System_Action_System_Single__System_Func_System_Single_System_Single__System_Action_System_Single_System_Boolean_"></a> Add\(float, Action<float\>, Func<float, float\>?, Action?, float, bool\)

Calls <code class="paramref">apply</code> with eased progress 0..1 every update for <code class="paramref">duration</code> seconds, then
<code class="paramref">done</code>. The starting value is applied at once (after <code class="paramref">delay</code>, when given).

```csharp
public Tween Add(float duration, Action<float> apply, Func<float, float>? ease = null, Action? done = null, float delay = 0, bool scaled = false)
```

#### Parameters

`duration` [float](https://learn.microsoft.com/dotnet/api/system.single)

The length in seconds, zero or more; zero applies the final value on the next update.

`apply` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

Receives the eased progress: 0 at the start, 1 at the end.

`ease` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[float](https://learn.microsoft.com/dotnet/api/system.single), [float](https://learn.microsoft.com/dotnet/api/system.single)\>?

The curve; [OutQuad](MicroEngine.Ease.md#MicroEngine_Ease_OutQuad_System_Single_) when null.

`done` [Action](https://learn.microsoft.com/dotnet/api/system.action)?

Called once, after the final value is applied; not when the tween is cancelled.

`delay` [float](https://learn.microsoft.com/dotnet/api/system.single)

Seconds before the tween starts.

`scaled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True to advance by the second clock of [Update](MicroEngine.Scenes.Tweener.md#MicroEngine_Scenes_Tweener_Update_System_Single_): in a scene, the step of the world's
clock, so the tween slows down and freezes with the world (an object's squash during a hitstop). False (the
default) keeps the engine's time, which interface and cutscene tweens want.

#### Returns

 [Tween](MicroEngine.Scenes.Tween.md)

The tween: yield it from a coroutine to wait for it, or keep it to cancel or complete it.

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">duration</code> or <code class="paramref">delay</code> is negative, infinite or not a number.

### <a id="MicroEngine_Scenes_Tweener_AddSteps_System_Int32_System_Action_System_Single__System_Func_System_Single_System_Single__System_Action_System_Int32_System_Boolean_"></a> AddSteps\(int, Action<float\>, Func<float, float\>?, Action?, int, bool\)

A tween whose length is a number of updates: <code class="paramref">apply</code> receives the eased value of 1/steps, 2/steps...
on the updates after the one it is added on, and 1 on the last. It counts updates, not time: slow motion does not
stretch it, and an update of zero seconds (a frozen world, for a scaled tween) is not counted.

```csharp
public Tween AddSteps(int steps, Action<float> apply, Func<float, float>? ease = null, Action? done = null, int delaySteps = 0, bool scaled = false)
```

#### Parameters

`steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The length in updates; 0 and 1 both apply the final value on the next update.

`apply` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

Receives the eased progress.

`ease` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[float](https://learn.microsoft.com/dotnet/api/system.single), [float](https://learn.microsoft.com/dotnet/api/system.single)\>?

The curve; [OutQuad](MicroEngine.Ease.md#MicroEngine_Ease_OutQuad_System_Single_) when null.

`done` [Action](https://learn.microsoft.com/dotnet/api/system.action)?

Called once, after the final value is applied.

`delaySteps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Updates before the tween starts.

`scaled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True to count the updates of the second clock of [Update](MicroEngine.Scenes.Tweener.md#MicroEngine_Scenes_Tweener_Update_System_Single_).

#### Returns

 [Tween](MicroEngine.Scenes.Tween.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">steps</code> or <code class="paramref">delaySteps</code> is negative.

### <a id="MicroEngine_Scenes_Tweener_Clear"></a> Clear\(\)

Cancels every tween: no final value is applied and no <code>done</code> callback is called. A coroutine waiting for one
goes on. Safe from a callback of one of the tweens.

```csharp
public void Clear()
```

### <a id="MicroEngine_Scenes_Tweener_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the tweens' state to the per-step state hash: for each tween, its length, time played, delay left, and how it counts.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Scenes_Tweener_Update_System_Single_"></a> Update\(float\)

Advances every tween by <code class="paramref">dt</code> seconds and calls the <code>done</code> callback of those that ended, in the order they were added. A scene calls this for its own [Tweens](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Tweens).

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_Tweener_Update_System_Single_System_Single_"></a> Update\(float, float\)

Advances the tweens on two clocks: <code class="paramref">dt</code> for the ordinary ones and <code class="paramref">scaledDt</code> for
those added with <code>scaled: true</code>. A scene gives the engine's step and its world's. A clock whose step is no
time (zero, negative, infinite, not a number) stands still: its tweens apply nothing and do not end.

```csharp
public void Update(float dt, float scaledDt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

`scaledDt` [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
