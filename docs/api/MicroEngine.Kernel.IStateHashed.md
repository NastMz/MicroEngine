# Interface IStateHashed

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

Something whose simulation state takes part in the per-step state hash ([StateHasher](MicroEngine.Kernel.StateHasher.md)): it adds the
values that decide what the game does next, and leaves out what only drawing uses.

```csharp
public interface IStateHashed
```

## Methods

### <a id="MicroEngine_Kernel_IStateHashed_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds this object's simulation state to <code class="paramref">hash</code>, always in the same order.

```csharp
void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

</div>
