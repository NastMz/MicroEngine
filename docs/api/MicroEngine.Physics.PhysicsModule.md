# Class PhysicsModule

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

The physics module: registers a [PhysicsSystem](MicroEngine.Physics.PhysicsSystem.md) service and steps it in [Physics](MicroEngine.Kernel.FramePhase.md)
(system [SystemName](MicroEngine.Physics.PhysicsModule.md#MicroEngine_Physics_PhysicsModule_SystemName)), once per fixed step, after the simulation's [FixedUpdate](MicroEngine.Kernel.FramePhase.md).

```csharp
public sealed class PhysicsModule : IEngineModule
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PhysicsModule](MicroEngine.Physics.PhysicsModule.md)

#### Implements

[IEngineModule](MicroEngine.Kernel.IEngineModule.md)

## Examples

```csharp
public override void Configure(EngineBuilder builder) => builder.Add(new PhysicsModule());
// later, in a scene: Game.Engine.Services.Get<PhysicsSystem>().Add(physics, world);
```

## Constructors

### <a id="MicroEngine_Physics_PhysicsModule__ctor"></a> PhysicsModule\(\)

```csharp
public PhysicsModule()
```

## Fields

### <a id="MicroEngine_Physics_PhysicsModule_SystemName"></a> SystemName

The name of the frame system that steps physics.

```csharp
public const string SystemName = "engine.physics"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Physics_PhysicsModule_Configure_MicroEngine_Kernel_EngineBuilder_"></a> Configure\(EngineBuilder\)

Registers what the module provides on <code class="paramref">builder</code>; called once, when the module is added.

```csharp
public void Configure(EngineBuilder builder)
```

#### Parameters

`builder` [EngineBuilder](MicroEngine.Kernel.EngineBuilder.md)

</div>
