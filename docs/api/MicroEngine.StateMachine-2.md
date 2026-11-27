# Class StateMachine<TState, TEvent\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

A state machine for gameplay: a character's moves, an enemy's behaviour, a boss's phases, the flow of a level. Each
state has what runs when it is entered, every step while it lasts and when it is left, and the ways out of it: a
condition checked every step (<code>When</code>), a time in the state in seconds (<code>After</code>) or in updates
(<code>AfterSteps</code>) or an event the game fires (<code>On</code>). The machine keeps the seconds and the updates spent in
the current state, runs on whatever step it is given (so it follows a scene's clock through slow motion and a
hitstop), needs no scene (the owner of a rule object calls `Update`), takes part in the state hash and
can be saved and restored. Stepping and firing allocate nothing.

```csharp
public class StateMachine<TState, TEvent> : IStateMachine, IStateHashed where TState : notnull where TEvent : notnull
```

#### Type Parameters

`TState` 

The states, usually an enum.

`TEvent` 

The events the game fires, usually an enum.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StateMachine<TState, TEvent\>](MicroEngine.StateMachine\-2.md)

#### Implements

[IStateMachine](MicroEngine.IStateMachine.md), 
[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Examples

```csharp
var enemy = new StateMachine<Enemy, Hit>(Enemy.Patrol);
enemy.State(Enemy.Patrol)
    .OnUpdate(dt => Walk(dt))
    .When(() => SeesPlayer, Enemy.Chase);
enemy.State(Enemy.Chase)
    .OnEnter(() => Audio.Play("alert"))
    .OnUpdate(dt => RunAtPlayer(dt))
    .When(() => !SeesPlayer, Enemy.Patrol);
enemy.State(Enemy.Stunned).After(0.8f, Enemy.Patrol);
enemy.Any.On(Hit.Stomp, Enemy.Stunned);

enemy.Update(dt);          // every step
enemy.Fire(Hit.Stomp);     // when it happens
```

## Remarks

<p>
One step (`Update`) adds the step to the time, runs the update of the current state (its parents'
first), then takes at most one way out: the current state's, in the order they were declared, then its parents',
then those declared on `Any`. That order is the priority of an interruption.
</p>
<p>
A change runs to completion: the exits, then the entries, then `Changed`. An event fired or a state
asked for from inside one of those handlers waits and is applied right after, in the order asked, so a handler
never sees the machine half changed.
</p>
<p>
A state may be a part of another (<code>SubstateOf</code>): the parent's ways out and events apply to all its parts, and
its entry and exit run only when the machine crosses its border.
</p>

## Constructors

### <a id="MicroEngine_StateMachine_2__ctor__0_"></a> StateMachine\(TState\)

Creates a machine that starts in <code class="paramref">initial</code>. Its entry runs at the first step, the first event or `Start`, so the states can be declared first.

```csharp
public StateMachine(TState initial)
```

#### Parameters

`initial` TState

## Properties

### <a id="MicroEngine_StateMachine_2_Any"></a> Any

What applies in every state, after the state's own and its parents': ways out and events that interrupt anything (a hit that stuns, a death).

```csharp
public StateMachine<TState, TEvent>.StateConfig Any { get; }
```

#### Property Value

 [StateMachine](MicroEngine.StateMachine\-2.md)<TState, TEvent\>.[StateConfig](MicroEngine.StateMachine\-2.StateConfig.md)

### <a id="MicroEngine_StateMachine_2_Changes"></a> Changes

How many changes of state the machine made.

```csharp
public int Changes { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_StateMachine_2_Current"></a> Current

The current state.

```csharp
public TState Current { get; }
```

#### Property Value

 TState

### <a id="MicroEngine_StateMachine_2_IsStarted"></a> IsStarted

Whether the initial state was entered.

```csharp
public bool IsStarted { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_StateMachine_2_Previous"></a> Previous

The state before the latest change; the initial state until the first one.

```csharp
public TState Previous { get; }
```

#### Property Value

 TState

### <a id="MicroEngine_StateMachine_2_Steps"></a> Steps

Updates spent in the current state: 0 when it is entered, then one more for every update of more than zero seconds.

```csharp
public int Steps { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_StateMachine_2_ThrowOnUnhandled"></a> ThrowOnUnhandled

When true, an event nothing takes throws `InvalidOperationException` instead of being ignored. False by default.

```csharp
public bool ThrowOnUnhandled { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_StateMachine_2_Time"></a> Time

Seconds spent in the current state: 0 when it is entered, then the sum of the steps since, as they were given. It
is a number to read, draw and save; leave a state after a time with <code>After</code>, which counts exactly, and not
by comparing this sum with the time.

```csharp
public float Time { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_StateMachine_2_Fire__1_"></a> Fire\(TEvent\)

Fires <code class="paramref">trigger</code>: the first way out that takes it (the current state's, then its parents', then
`Any`'s) changes the state or runs its action. Fired from one of the machine's handlers, it waits
until the change in progress is complete.

```csharp
public bool Fire(TEvent trigger)
```

#### Parameters

`trigger` TEvent

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

False when nothing took the event; true when it was taken or is waiting.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Nothing took the event and `ThrowOnUnhandled` is set.

### <a id="MicroEngine_StateMachine_2_Go__0_"></a> Go\(TState\)

Changes to <code class="paramref">state</code> whatever the declared ways out say, running the exits and the entries.
Going to the current state leaves and enters it again. Called from one of the machine's handlers, it waits until
the change in progress is complete.

```csharp
public void Go(TState state)
```

#### Parameters

`state` TState

### <a id="MicroEngine_StateMachine_2_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the current state (by the order the states were first named) and the time and the steps in it and in each of its parents.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_StateMachine_2_IsIn__0_"></a> IsIn\(TState\)

Whether the machine is in <code class="paramref">state</code> or in one of its parts.

```csharp
public bool IsIn(TState state)
```

#### Parameters

`state` TState

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_StateMachine_2_Restore_MicroEngine_MachineSnapshot__0__"></a> Restore\(MachineSnapshot<TState\>\)

Puts the machine in the state of <code class="paramref">snapshot</code> with its time and steps, without running any exit or
entry: what loading a save does. The parents of the state take the same time and steps. The machine counts as
started, and its ways out by time and by steps are taken on the update the saved machine would have taken them.

```csharp
public void Restore(MachineSnapshot<TState> snapshot)
```

#### Parameters

`snapshot` [MachineSnapshot](MicroEngine.MachineSnapshot\-1.md)<TState\>

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Called from one of the machine's own handlers.

### <a id="MicroEngine_StateMachine_2_SetSteps_System_Int32_"></a> SetSteps\(int\)

Sets the updates spent in the current state (`Steps`), as `SetTime` sets its seconds.
Its ways out by steps count from the new value. The parents of the state keep their own.

```csharp
public void SetSteps(int steps)
```

#### Parameters

`steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">steps</code> is negative.

### <a id="MicroEngine_StateMachine_2_SetTime_System_Single_"></a> SetTime\(float\)

Sets the seconds spent in the current state, as if it had been entered that long ago: a head start
(<code>Go</code> then <code>SetTime</code>), a state held at a moment while something else finishes, a countdown put back.
Its ways out by time count from the new value; nothing runs. The parents of the state keep their own time, and
`Steps` does not change.

```csharp
public void SetTime(float seconds)
```

#### Parameters

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">seconds</code> is negative or not a number.

### <a id="MicroEngine_StateMachine_2_Snapshot"></a> Snapshot\(\)

The current state, its time and its steps, to write into a save.

```csharp
public MachineSnapshot<TState> Snapshot()
```

#### Returns

 [MachineSnapshot](MicroEngine.MachineSnapshot\-1.md)<TState\>

### <a id="MicroEngine_StateMachine_2_Start"></a> Start\(\)

Enters the initial state now (its parents' entries first). Does nothing once started.

```csharp
public void Start()
```

### <a id="MicroEngine_StateMachine_2_State__0_"></a> State\(TState\)

Declares or continues declaring <code class="paramref">state</code>.

```csharp
public StateMachine<TState, TEvent>.StateConfig State(TState state)
```

#### Parameters

`state` TState

#### Returns

 [StateMachine](MicroEngine.StateMachine\-2.md)<TState, TEvent\>.[StateConfig](MicroEngine.StateMachine\-2.StateConfig.md)

### <a id="MicroEngine_StateMachine_2_StepsIn__0_"></a> StepsIn\(TState\)

Updates since <code class="paramref">state</code> was entered while the machine is in it or in one of its parts; 0 otherwise.

```csharp
public int StepsIn(TState state)
```

#### Parameters

`state` TState

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_StateMachine_2_TimeIn__0_"></a> TimeIn\(TState\)

Seconds since <code class="paramref">state</code> was entered while the machine is in it or in one of its parts; 0 otherwise.

```csharp
public float TimeIn(TState state)
```

#### Parameters

`state` TState

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_StateMachine_2_Update_System_Single_"></a> Update\(float\)

Advances the machine by one step: see the remarks of `StateMachine`2`.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

The step in seconds. Zero runs the updates and the conditions without moving the time or the steps, and takes no
way out by time or by steps; a negative, infinite or not-a-number step is taken as zero.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Called from one of the machine's own handlers.

### <a id="MicroEngine_StateMachine_2_Changed"></a> Changed

Raised after every change, with the state left and the state entered, once the entries ran.

```csharp
public event Action<TState, TState>? Changed
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-2)<TState, TState\>?

### <a id="MicroEngine_StateMachine_2_Unhandled"></a> Unhandled

Raised when an event is fired that no way out of the current state, its parents or `Any` takes.

```csharp
public event Action<TState, TEvent>? Unhandled
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-2)<TState, TEvent\>?

</div>
