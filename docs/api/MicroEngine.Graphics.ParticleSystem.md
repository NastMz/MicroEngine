# Class ParticleSystem

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

A pooled particle system for dust, sparks, confetti, smoke and the like.

```csharp
public sealed class ParticleSystem
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ParticleSystem](MicroEngine.Graphics.ParticleSystem.md)

## Constructors

### <a id="MicroEngine_Graphics_ParticleSystem__ctor_System_Int32_System_Int32_"></a> ParticleSystem\(int, int\)

Creates an empty system.

```csharp
public ParticleSystem(int capacity = 512, int seed = 3)
```

#### Parameters

`capacity` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Maximum number of live particles; emissions beyond it are dropped.

`seed` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Seed of the system's own random numbers, so effects repeat exactly.

## Properties

### <a id="MicroEngine_Graphics_ParticleSystem_Additive"></a> Additive

When true, particles are drawn with additive blending (glows, sparks).

```csharp
public bool Additive { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_ParticleSystem_Count"></a> Count

Number of live particles.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_ParticleSystem_Clear"></a> Clear\(\)

Removes every particle.

```csharp
public void Clear()
```

### <a id="MicroEngine_Graphics_ParticleSystem_Draw_MicroEngine_Graphics_IRenderer_"></a> Draw\(IRenderer\)

Draws every live particle. With [Additive](MicroEngine.Graphics.ParticleSystem.md#MicroEngine_Graphics_ParticleSystem_Additive), the blend mode is set to additive and back to [Alpha](MicroEngine.Graphics.BlendMode.md) afterwards.

```csharp
public void Draw(IRenderer renderer)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

### <a id="MicroEngine_Graphics_ParticleSystem_Emit_System_Numerics_Vector2_MicroEngine_Graphics_ParticleSpec_System_Int32_System_Numerics_Vector2_"></a> Emit\(Vector2, ParticleSpec, int, Vector2\)

Spawns particles with random values within the ranges of <code class="paramref">spec</code>.

```csharp
public void Emit(Vector2 at, ParticleSpec spec, int count = 1, Vector2 inheritVelocity = default)
```

#### Parameters

`at` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Spawn point in the coordinates the system is drawn in.

`spec` [ParticleSpec](MicroEngine.Graphics.ParticleSpec.md)

How the particles look and move; kept by reference, so do not change it while its particles live.

`count` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Number of particles; those beyond the capacity are dropped.

`inheritVelocity` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Velocity added to each particle, in pixels per second (for example the emitter's).

### <a id="MicroEngine_Graphics_ParticleSystem_Update_System_Single_"></a> Update\(float\)

Ages and moves every particle by <code class="paramref">dt</code> seconds and removes those past their life.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
