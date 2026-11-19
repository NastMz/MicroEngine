# Interface IStateMachine

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

Something stepped like a state machine: what a scene's <code>StateMachines</code> system runs.

```csharp
public interface IStateMachine : IStateHashed
```

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Methods

### <a id="MicroEngine_IStateMachine_Update_System_Single_"></a> Update\(float\)

Advances the machine by one step of <code class="paramref">dt</code> seconds.

```csharp
void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
