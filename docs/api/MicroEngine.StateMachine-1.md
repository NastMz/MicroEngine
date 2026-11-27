# Class StateMachine<TState\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

A state machine without events: its states change by conditions, by time and by <code>Go</code>.

```csharp
public sealed class StateMachine<TState> : StateMachine<TState, NoEvent>, IStateMachine, IStateHashed where TState : notnull
```

#### Type Parameters

`TState` 

The states, usually an enum.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StateMachine<TState, NoEvent\>](MicroEngine.StateMachine\-2.md) ← 
[StateMachine<TState\>](MicroEngine.StateMachine\-1.md)

#### Implements

[IStateMachine](MicroEngine.IStateMachine.md), 
[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

#### Inherited Members

[StateMachine<TState, NoEvent\>.State\(TState\)](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_State\_\_0\_), 
[StateMachine<TState, NoEvent\>.IsIn\(TState\)](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_IsIn\_\_0\_), 
[StateMachine<TState, NoEvent\>.TimeIn\(TState\)](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_TimeIn\_\_0\_), 
[StateMachine<TState, NoEvent\>.StepsIn\(TState\)](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_StepsIn\_\_0\_), 
[StateMachine<TState, NoEvent\>.SetTime\(float\)](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_SetTime\_System\_Single\_), 
[StateMachine<TState, NoEvent\>.SetSteps\(int\)](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_SetSteps\_System\_Int32\_), 
[StateMachine<TState, NoEvent\>.Start\(\)](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Start), 
[StateMachine<TState, NoEvent\>.Update\(float\)](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Update\_System\_Single\_), 
[StateMachine<TState, NoEvent\>.Fire\(NoEvent\)](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Fire\_\_1\_), 
[StateMachine<TState, NoEvent\>.Go\(TState\)](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Go\_\_0\_), 
[StateMachine<TState, NoEvent\>.Snapshot\(\)](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Snapshot), 
[StateMachine<TState, NoEvent\>.Restore\(MachineSnapshot<TState\>\)](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Restore\_MicroEngine\_MachineSnapshot\_\_0\_\_), 
[StateMachine<TState, NoEvent\>.HashState\(StateHasher\)](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_HashState\_MicroEngine\_Kernel\_StateHasher\_), 
[StateMachine<TState, NoEvent\>.Current](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Current), 
[StateMachine<TState, NoEvent\>.Previous](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Previous), 
[StateMachine<TState, NoEvent\>.Time](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Time), 
[StateMachine<TState, NoEvent\>.Steps](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Steps), 
[StateMachine<TState, NoEvent\>.Changes](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Changes), 
[StateMachine<TState, NoEvent\>.IsStarted](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_IsStarted), 
[StateMachine<TState, NoEvent\>.ThrowOnUnhandled](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_ThrowOnUnhandled), 
[StateMachine<TState, NoEvent\>.Any](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Any), 
[StateMachine<TState, NoEvent\>.Changed](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Changed), 
[StateMachine<TState, NoEvent\>.Unhandled](MicroEngine.StateMachine\-2.md\#MicroEngine\_StateMachine\_2\_Unhandled), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_StateMachine_1__ctor__0_"></a> StateMachine\(TState\)

Creates a machine that starts in <code class="paramref">initial</code>.

```csharp
public StateMachine(TState initial)
```

#### Parameters

`initial` TState

</div>
