# MicroEngine.Ecs

An Entity Component System for [MicroEngine](https://github.com/NastMz/MicroEngine) games: a component store of the
[Arch](https://github.com/genaray/Arch) library that lives in a scene, with what joins it to the engine. The store is
created and destroyed with its scene, its systems run in a declared order around the scene's update, changes asked
during a walk are applied between systems, a row can be linked to an entity of the scene's world, drawing reads
components blended between two steps, and the rows are part of the state hash.

```csharp
public static readonly EcsComponents Components = new EcsComponents()
    .AddState<Position>().AddState<Velocity>().AddTag<Frozen>()
    .Interpolate<Position>(static (in Position a, in Position b, float t) => new Position { Value = Vector2.Lerp(a.Value, b.Value, t) });

public override void Enter()
{
    var ecs = this.AddEcs(Components);            // the store lives and dies with this scene
    ecs.Systems.Add(new Movement());              // before the scene's Update, in this order
    ecs.Systems.Add(new Combat());
    ecs.LateSystems.Add(new Deaths());            // after it
    ecs.World.Create(new Position { Value = start }, new Velocity { Value = speed });   // Arch's own world
}
```

The package adopts the library and does not hide it: `EcsStore.World` is Arch's `World`, and rows, queries, components
and the command buffer are used as Arch documents them. The scene's world stays the engine's default model; reference
this package only for a game that models with components and systems.

MicroEngine is in **Alpha** (0.x): public APIs may change incompatibly between versions; each change is listed in the
[migration notes](https://github.com/NastMz/MicroEngine/blob/main/docs/migrations.md). MIT licensed; Arch is
Apache-2.0.
