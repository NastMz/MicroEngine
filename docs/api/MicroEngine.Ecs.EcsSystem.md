# Class EcsSystem

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Ecs](MicroEngine.Ecs.md)  
Assembly: MicroEngine.Ecs.dll  
Guide: [Entity Component Systems](../guide/entity-component-systems.md)  

One behaviour of a store: a class with the queries it walks and an [Update](MicroEngine.Ecs.EcsSystem.md#MicroEngine_Ecs_EcsSystem_Update_System_Single_) the store calls once per
step, in the order the systems were added ([Systems](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Systems) before the scene's update,
[LateSystems](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_LateSystems) after it). What it asks of [Commands](MicroEngine.Ecs.EcsSystem.md#MicroEngine_Ecs_EcsSystem_Commands) is applied when its update
returns, before the next system runs.

```csharp
public abstract class EcsSystem
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EcsSystem](MicroEngine.Ecs.EcsSystem.md)

## Examples

```csharp
public sealed class Movement : EcsSystem
{
    private static readonly QueryDescription Moving = new QueryDescription().WithAll<Position, Velocity>().WithNone<Frozen>();
    protected override void Update(float dt)
    {
        foreach (ref var chunk in World.Query(in Moving))
        {
            var positions = chunk.GetSpan<Position>();
            var velocities = chunk.GetSpan<Velocity>();
            for (int i = 0; i < chunk.Count; i++) positions[i].Value += velocities[i].Value * dt;
        }
    }
}
```

## Constructors

### <a id="MicroEngine_Ecs_EcsSystem__ctor"></a> EcsSystem\(\)

```csharp
protected EcsSystem()
```

## Properties

### <a id="MicroEngine_Ecs_EcsSystem_Commands"></a> Commands

The store's deferred changes ([Commands](MicroEngine.Ecs.EcsStore.md#MicroEngine_Ecs_EcsStore_Commands)).

```csharp
public CommandBuffer Commands { get; }
```

#### Property Value

 CommandBuffer

### <a id="MicroEngine_Ecs_EcsSystem_Enabled"></a> Enabled

Whether the store calls [Update](MicroEngine.Ecs.EcsSystem.md#MicroEngine_Ecs_EcsSystem_Update_System_Single_). Simulation state: part of the state hash.

```csharp
public bool Enabled { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Ecs_EcsSystem_Game"></a> Game

The scene's game.

```csharp
public Game Game { get; }
```

#### Property Value

 [Game](MicroEngine.Core.Game.md)

### <a id="MicroEngine_Ecs_EcsSystem_Name"></a> Name

The system's name within its list, unique there; the type's name unless overridden.

```csharp
public virtual string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Ecs_EcsSystem_Scene"></a> Scene

The store's scene.

```csharp
public Scene Scene { get; }
```

#### Property Value

 [Scene](MicroEngine.Scenes.Scene.md)

### <a id="MicroEngine_Ecs_EcsSystem_Store"></a> Store

The store this system belongs to.

```csharp
public EcsStore Store { get; }
```

#### Property Value

 [EcsStore](MicroEngine.Ecs.EcsStore.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The system is not in a store.

### <a id="MicroEngine_Ecs_EcsSystem_World"></a> World

The store's rows: the library's world. Available from [Start](MicroEngine.Ecs.EcsSystem.md#MicroEngine_Ecs_EcsSystem_Start) on.

```csharp
public World World { get; }
```

#### Property Value

 World

## Methods

### <a id="MicroEngine_Ecs_EcsSystem_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds what the system itself keeps between steps (a counter, a timer) to the state hash. The default adds nothing.

```csharp
public virtual void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_Ecs_EcsSystem_Start"></a> Start\(\)

Called once, when the store has its rows (its scene is on the stack): at once for a system added later.

```csharp
protected virtual void Start()
```

### <a id="MicroEngine_Ecs_EcsSystem_Update_System_Single_"></a> Update\(float\)

Advances the behaviour by one step.

```csharp
protected abstract void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

The step the scene's [Update](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Update_System_Single_) receives.

</div>
