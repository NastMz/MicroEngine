# Class StateMachine<TState, TEvent\>.StateConfig

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

The declaration of one state (or of `Any`); every call returns it, to chain them.

```csharp
public sealed class StateMachine<TState, TEvent>.StateConfig
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StateMachine<TState, TEvent\>.StateConfig](MicroEngine.StateMachine\-2.StateConfig.md)

## Methods

### <a id="MicroEngine_StateMachine_2_StateConfig_After_System_Single__0_System_Func_System_Boolean__"></a> After\(float, TState, Func<bool\>?\)

A way out by time: once the state lasted <code class="paramref">seconds</code> (and <code class="paramref">condition</code> is true,
when given) the machine changes to <code class="paramref">target</code>. Declared on a parent, the time is the parent's;
declared on `Any`, the current state's.

```csharp
public StateMachine<TState, TEvent>.StateConfig After(float seconds, TState target, Func<bool>? condition = null)
```

#### Parameters

`seconds` [float](https://learn.microsoft.com/dotnet/api/system.single)

`target` TState

`condition` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>?

#### Returns

 [StateMachine](MicroEngine.StateMachine\-2.md)<TState, TEvent\>.[StateConfig](MicroEngine.StateMachine\-2.StateConfig.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">seconds</code> is negative or not a number.

### <a id="MicroEngine_StateMachine_2_StateConfig_Do__1_System_Action_System_Func_System_Boolean__"></a> Do\(TEvent, Action, Func<bool\>?\)

Takes an event without leaving the state: when <code class="paramref">trigger</code> is fired (and <code class="paramref">guard</code> is true, when given) <code class="paramref">action</code> runs. An empty action ignores the event on purpose.

```csharp
public StateMachine<TState, TEvent>.StateConfig Do(TEvent trigger, Action action, Func<bool>? guard = null)
```

#### Parameters

`trigger` TEvent

`action` [Action](https://learn.microsoft.com/dotnet/api/system.action)

`guard` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>?

#### Returns

 [StateMachine](MicroEngine.StateMachine\-2.md)<TState, TEvent\>.[StateConfig](MicroEngine.StateMachine\-2.StateConfig.md)

### <a id="MicroEngine_StateMachine_2_StateConfig_On__1__0_System_Func_System_Boolean__"></a> On\(TEvent, TState, Func<bool\>?\)

A way out by event: when <code class="paramref">trigger</code> is fired (and <code class="paramref">guard</code> is true, when given) the machine changes to <code class="paramref">target</code>.

```csharp
public StateMachine<TState, TEvent>.StateConfig On(TEvent trigger, TState target, Func<bool>? guard = null)
```

#### Parameters

`trigger` TEvent

`target` TState

`guard` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>?

#### Returns

 [StateMachine](MicroEngine.StateMachine\-2.md)<TState, TEvent\>.[StateConfig](MicroEngine.StateMachine\-2.StateConfig.md)

### <a id="MicroEngine_StateMachine_2_StateConfig_OnEnter_System_Action_"></a> OnEnter\(Action\)

Runs when the state is entered. Several calls add up, in order.

```csharp
public StateMachine<TState, TEvent>.StateConfig OnEnter(Action action)
```

#### Parameters

`action` [Action](https://learn.microsoft.com/dotnet/api/system.action)

#### Returns

 [StateMachine](MicroEngine.StateMachine\-2.md)<TState, TEvent\>.[StateConfig](MicroEngine.StateMachine\-2.StateConfig.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Declared on `Any`, which is never entered.

### <a id="MicroEngine_StateMachine_2_StateConfig_OnExit_System_Action_"></a> OnExit\(Action\)

Runs when the state is left. Several calls add up, in order.

```csharp
public StateMachine<TState, TEvent>.StateConfig OnExit(Action action)
```

#### Parameters

`action` [Action](https://learn.microsoft.com/dotnet/api/system.action)

#### Returns

 [StateMachine](MicroEngine.StateMachine\-2.md)<TState, TEvent\>.[StateConfig](MicroEngine.StateMachine\-2.StateConfig.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Declared on `Any`, which is never left.

### <a id="MicroEngine_StateMachine_2_StateConfig_OnUpdate_System_Action_System_Single__"></a> OnUpdate\(Action<float\>\)

Runs every step while the machine is in the state or in one of its parts, with the step. Several calls add up, in order.

```csharp
public StateMachine<TState, TEvent>.StateConfig OnUpdate(Action<float> action)
```

#### Parameters

`action` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

#### Returns

 [StateMachine](MicroEngine.StateMachine\-2.md)<TState, TEvent\>.[StateConfig](MicroEngine.StateMachine\-2.StateConfig.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Declared on `Any`.

### <a id="MicroEngine_StateMachine_2_StateConfig_SubstateOf__0_"></a> SubstateOf\(TState\)

Makes the state a part of <code class="paramref">parent</code>: the parent's ways out and events apply to it, and the parent's entry and exit run only when the machine crosses the parent's border.

```csharp
public StateMachine<TState, TEvent>.StateConfig SubstateOf(TState parent)
```

#### Parameters

`parent` TState

#### Returns

 [StateMachine](MicroEngine.StateMachine\-2.md)<TState, TEvent\>.[StateConfig](MicroEngine.StateMachine\-2.StateConfig.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Declared on `Any`, the state already has a parent, or the parent is the state or one of its parts.

### <a id="MicroEngine_StateMachine_2_StateConfig_When_System_Func_System_Boolean___0_"></a> When\(Func<bool\>, TState\)

A way out checked every step: when <code class="paramref">condition</code> is true the machine changes to <code class="paramref">target</code>.

```csharp
public StateMachine<TState, TEvent>.StateConfig When(Func<bool> condition, TState target)
```

#### Parameters

`condition` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

`target` TState

#### Returns

 [StateMachine](MicroEngine.StateMachine\-2.md)<TState, TEvent\>.[StateConfig](MicroEngine.StateMachine\-2.StateConfig.md)

</div>
