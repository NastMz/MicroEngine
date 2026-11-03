# ARCH-1 P7: Courtyard Key on ids, transforms and attachments

Prototype P7 of ARCH-1 Phase II (dossier [docs/evaluations/arch-1.md](../../../docs/evaluations/arch-1.md) §3 S6/S8,
§6.4 Scene sketch, §6.5 Scene row, §7 ADR 0030, §9 P7; annex A3 WO-3, WO-4/SC-2, WO-5, SC-1, SC-3, DM-1; annex R2
§2 entities). Gate: the `IWorld` contract of stage 3 is frozen after this prototype.

## Question

Does the proposed `IWorld` (ids with generations, a `Transform` hierarchy, typed attachments, engine interpolation with
`Teleport`, world clocks with time scale) make Courtyard Key's code simpler and correct, and what API shape should be
frozen?

## Answer in one paragraph

Yes on correctness, mostly yes on simplicity, with two corrections to the §6.4 sketch. The port of Courtyard Key's
actors, hero, room objects, camera, interpolation and hitstop onto a prototype world records **bit-identical state
traces** to the original on all 11 scripted scenarios (22 runs: two storage designs), passes all 47 of the game's own
tests (two test files adapted to the deleted helpers), and costs nothing measurable per frame (time within ±5 %,
allocation within ±0.1 %). The game no longer carries 12 engine-like helpers (interpolation, snap, per-scene step
tracking, spawn queue, list choreography, hitstop timer). It does not get shorter: it gains the members the engine's
`Entity` base used to provide and a 36-line room filter, because the sketch has no way to group entities. Corrections
to freeze: the world owns its blend (`WorldMatrix(id)` without a blend argument, set from per-world step tracking;
the sketch's `WorldMatrix(id, blend)` alone lets games pass the global blend and reintroduces D11), and iteration
order is part of the contract (attach order, stable under removal: a swap-remove store changed the simulation).
Attachments: one dense array per type; the per-entity dictionary is 4.7x slower to look up, 11x to 160x slower to iterate
and allocates 240 B per created entity.

## Method

- `Scene/` is a self-contained prototype of the stage 3 contract (`IWorld`, `World`, `EntityId`, `Transform`,
  `WorldClock`, `Attachments<T>`, `IDestroyedCallback`), with three attachment storage designs selectable per world:
  `DenseStable` (one dense array per type, sparse slot index, stable compaction at the step's end), `DenseSwapRemove`
  (the same, filling holes with the last element, as sparse-set ECSs do) and `PerEntityDictionary`
  (`Dictionary<Type, object>` per entity). It references no engine assembly. `Scene.Tests/` checks every rule below
  (44 tests, each rule on every storage design where it applies). RED was observed first: 42 of 44 failed against a
  stub that threw `NotImplementedException` (the clock and transform tests passed already).
- `CourtyardKey.Port/` is the ported game. It compiles every unchanged file straight from `games/CourtyardKey` and
  replaces only nine files (`Entities/Actor.cs`, `Boss.cs`, `Enemies.cs`, `Scenes/PlayScene*.cs`, `Spawner.cs`), so
  `git diff --no-index games/CourtyardKey/<file> experiments/arch-1/p7-entities/CourtyardKey.Port/<file>` is exactly
  the port. `games/CourtyardKey` and the engine are untouched. `Scenes/StorageChoice.cs` is a measurement switch only.
- `CourtyardKey.Port.Tests/` runs the game's own tests against the port (linked from `tests/CourtyardKey.Tests`, with
  `FrameBlendTests.cs` and `PlayTests.cs` adapted, see below) and `PortEquivalenceTests`: each of the 11 scripts of
  the state trace check runs on the original (referenced under the alias `original`) and on the port, and every
  per-step state hash (all sections, play scene included) must be equal; drawing must not change the hash.
- `Bench/` measures attachment lookup and iteration per storage design, struct vs class attachments, transform access,
  interpolation and hierarchy cost, step cost, entity churn, and whole frames of the original vs the port (headless,
  deterministic, muted, captures removed, update + draw per frame, one warm-up pass then best of 7 interleaved rounds).
  Raw output: [results/measurements.md](results/measurements.md).
- None of these projects is in `MicroEngine.slnx` or CI (`.github/scripts/changes.mjs` runs nothing for
  `experiments/`). Licence of the derived game files: [NOTICE.md](NOTICE.md).

Reproduce (from this folder):

```sh
dotnet test Scene.Tests -c Release
dotnet test CourtyardKey.Port.Tests -c Release
dotnet run -c Release --project Bench -- all > results/measurements.md
```

## Results

| Measure | Original | Port | Notes |
| --- | --- | --- | --- |
| State traces, 11 scripts | reference | **identical, every step** (DenseStable and PerEntityDictionary) | 22 runs; drawing changes nothing in any of them |
| State traces with swap-remove storage | reference | **diverges** in `gameover.script` at update 248 (`play.actors`) | the other 10 scripts did not catch it: order dependence is latent |
| Courtyard Key tests | 47 pass | **47 pass** | two files adapted: the four `FrameBlendTests` read the world's interpolation; one line of `PlayTests` constructs a knight instead of `Spawn`ing it |
| Code lines (non-blank, non-comment) of the 9 files | 1645 | 1668 (+23) | Actor +13 (members the engine's `Entity` provided), PlayScene +32 (the 36-line room filter, the camera entity and the world, less the spawn queue, its flushes and the two lists), Draw −15, others −7 |
| Diff of the port | — | +211 / −169 lines | 2 one-line changes in `Boss.cs` and `Enemies.cs` (`World.Spawn(new …)` → `new …`) |
| Engine-like helpers in the game | 12 | 0 (2 glue lines the scene module takes over in stage 3) | list below |
| Frame time, µs (fight / boss / showcase / gameover / opening) | 400.6 / 742.0 / 1189.4 / 212.5 / 152.1 | dense: 408.4 / 709.9 / 1198.9 / 218.8 / 152.6; dictionary: 405.4 / 714.9 / 1230.7 / 213.6 / 152.8 | within ±5 %, no consistent direction |
| Allocation per frame, B (same scripts) | 64 213 / 384 238 / 309 881 / 36 564 / 31 442 | dense: 64 234 / 384 238 / 309 867 / 36 592 / 31 436 | within ±0.1 %; the frame's allocation is outside the world, unchanged by the port |
| Interpolation correctness | own code; frozen-overlay fix written by the game (`DrawBlend`) | world-owned; frozen overlay blend 1 with no game code | see "Interpolation" |

Micro-measurements (1000 entities, .NET 10.0.14, x64, Windows; ns per operation, best of 9 runs, 0 B allocated unless
stated):

| Operation | DenseStable | PerEntityDictionary |
| --- | ---: | ---: |
| `TryGet<class>` hit, 4 attachment types per entity, random order | 2.76 | 12.88 |
| `TryGet<class>` miss | 1.61 | 8.46 |
| `Value<struct>` by reference, read + write | 2.81 | 12.65 (boxed) |
| `All<class>` iteration, every entity has it, per element | 1.15 | 12.36 |
| `All<class>` iteration, 1 in 10 has it, per element found | 2.29 | 370.55 |
| Create + `Attach<class>` + `Destroy` + release, per entity, steady state | 45.07, 0 B | 115.27, **240 B** |
| Baseline: walking an array of references | 0.41 | |

| Operation (DenseStable) | ns |
| --- | ---: |
| `Local(id).Position2D` read (what `Actor.Position` costs) | 1.33 (10.6 before the id check was made inlinable) |
| `Local(id).Position2D += delta` | 1.57 |
| Baseline: a `Vector2` field | 0.60 |
| `DrawPosition2D(id, 0.5)`, root entity | 2.69 |
| Baseline: the game's own `Actor.Blend` over two `Vector2` arrays | 0.82 |
| `DrawPosition2D(id, 0.5)`, child at depth 1 / 3 (matrix path) | 32.12 / 59.26 |
| `WorldMatrix(id, 0.5)`, root entity | 50.59 |
| `BeginStep` (keep step-start transforms), per entity | 0.59 (baseline copy of `Vector2`s: 0.06) |

Cost of the hierarchy for a 2D game that does not use it: one branch per draw position (root entities never build a
matrix) and 12 bytes per entity (parent, first child, next sibling); measured inside the 2.69 ns above. The larger cost
is 3D storage, not the hierarchy: a `Transform` is 40 bytes instead of an 8-byte `Vector2`, so keeping step-start
transforms costs 0.59 ns per entity per step instead of 0.06 (0.6 µs per step for 1000 entities; Courtyard Key has
about 40). Struct attachments cost the same as class attachments in dense storage.

## Helpers the game no longer needs

1. `Actor._previousPosition` / `_hasPrevious` and `RememberPosition()`
2. `Actor.SnapDrawPosition()`
3. `Actor.DrawPositionAt(blend)`
4. `Actor.Blend(previous, current, blend)` (the "exact at 1" lerp)
5. `PlayScene.DrawBlend` with `_rememberedUpdate` (the game's own fix of D11/SC-2)
6. `PlayScene._previousCamera` / `_hasPreviousCamera` and `CameraDrawPositionAt(blend)`
7. `PlayScene.RememberDrawPositions()`, including its rule "remember only actors drawn last frame"
8. The camera save/restore around drawing (`try`/`finally`)
9. The spawn queue `_pending` and `Spawn<T>()`
10. `FlushSpawns()` and its six call sites
11. The two `EntityList`s and the scroll choreography that swaps, clears and flushes them
12. The hitstop timer `_hitstop` (now `World.Clock.Freeze`/`Advance`)

What the game gained: the `Actor` members the engine's `Entity` base class provided (`Position` over the world
transform, `Hitbox`, `Alive`, `Bounds`, `Center`, `Destroy`, `Update`, `Draw`, `Removed`), a `RoomActors<T>` filter
(the room an actor belongs to; the world has no grouping), a `CameraPosition` property over the camera entity, and two
glue lines (`Objects.BeginStep(Game.UpdateCount)` at the top of `Update`, `Objects.BeginDraw(Game.UpdateCount,
Game.FrameBlend)` at the top of `Draw`) that stage 3's scene module makes unnecessary.

## Before and after

Interpolation and the frozen-overlay fix, before (`games/CourtyardKey/Scenes/PlayScene.Draw.cs`, abridged):

```csharp
private Vector2 _previousCamera;
private bool _hasPreviousCamera;
private long _rememberedUpdate = -1;
public float DrawBlend => Game.UpdateCount == _rememberedUpdate ? Game.FrameBlend : 1;
public Vector2 CameraDrawPositionAt(float blend) => Actor.Blend(_hasPreviousCamera ? _previousCamera : Camera.Position, Camera.Position, blend);
private void RememberDrawPositions()
{
    _rememberedUpdate = Game.UpdateCount;
    _previousCamera = Camera.Position;
    _hasPreviousCamera = true;
    Hero.RememberPosition();
    foreach (var a in _drawList) a.RememberPosition();
}
public override void Draw(IRenderer r)
{
    var camera = Camera.Position;
    Camera.Position = CameraDrawPositionAt(DrawBlend);
    try { DrawWorld(r); }
    finally { Camera.Position = camera; }
}
```

After (`CourtyardKey.Port/Scenes/PlayScene.Draw.cs`; the first line moves into the scene module in stage 3):

```csharp
public override void Draw(IRenderer r)
{
    Objects.BeginDraw(Game.UpdateCount, Game.FrameBlend);   // stage 3: the scene module does this before drawing
    Camera.Position = Objects.DrawPosition2D(_camera);
    DrawWorld(r);
}
```

The step, before and after (`PlayScene.cs`):

```csharp
// before
if (_hitstop > 0) { _hitstop -= dt; return; }
...
Hero.Update(dt);
FlushSpawns();
_actors.Update(dt);
FlushSpawns();
ResolveCombat();
FlushSpawns();
UpdateRoomRules(dt);

// after
if (Objects.Clock.Advance(dt) == 0) return;   // hitstop: the world's clock is frozen; dt exactly otherwise
...
Hero.Update(dt);
foreach (var a in Actors) a.Update(dt);
ResolveCombat();
UpdateRoomRules(dt);
```

Scrolling to the next room, before and after (`PlayScene.Rooms.cs`):

```csharp
// before
_scrollFrom = Room;
(_leaving, _actors) = (_actors, _leaving);
_actors.Clear();
_pending.Clear();
foreach (var b in _leaving.OfType<Boomerang>()) b.Destroy();
_leaving.Flush();
LoadRoom(Room.X + dx, Room.Y + dy, _actors);
_actors.Flush();

// after: the room's actors become the leaving room's because another room becomes current
_scrollFrom = Room;
foreach (var b in LeavingActors.OfType<Boomerang>()) b.Destroy();
LoadRoom(Room.X + dx, Room.Y + dy);
```

The actor base, after (`CourtyardKey.Port/Entities/Actor.cs`): one class attachment per entity; constructing an actor
creates its entity, so `World.Spawn(new Projectile(…))` became `new Projectile(…);`.

```csharp
public abstract class Actor : IStateHashed, IDestroyedCallback
{
    protected Actor(PlayScene world)
    {
        World = world;
        Id = world.Objects.Create();
        world.Objects.Attach(Id, this);
        Home = world.Room;
    }
    public EntityId Id { get; }
    public Room? Home { get; }
    public Vector2 Position
    {
        get => World.Objects.Local(Id).Position2D;
        set => World.Objects.Local(Id).Position2D = value;
    }
    public bool Alive => World.Objects.IsAlive(Id);
    public void Destroy() => World.Objects.Destroy(Id);
    protected Vector2 DrawPosition => World.Objects.DrawPosition2D(Id);
    ...
}
```

## Findings

**Correctness.** Bit-identical traces mean the world reproduces every rule the game relied on: creation order,
skipping destroyed actors at once, data readable after `Destroy` (`Collect` destroys a pickup, then bursts sparkles at
its position), the hitstop arithmetic, and the camera and hero positions. The scripts run at blend 1, so traces do not
exercise drawing between steps; three properties do, beyond the game's own code: (1) a scene under an overlay is drawn
at blend 1 with no game code (tested: `APausedRoomIsDrawnAsItsLastUpdate` sets an engine blend of 0.4 and reads 0.4
while playing, 1 under the pause menu); (2) an actor spawned in the first of two steps run in one frame is
interpolated in the second (code reading: the original remembered only actors drawn last frame, so it drew that one
unblended); (3) a teleport holds through later moves in the same step (tested in `Scene.Tests`).

**Iteration order is behaviour.** Courtyard Key's rules take the first match (`TryInteract`, `OnHeroPushing`), consume
one random generator in update order, and hash actors in list order. With swap-remove storage, `gameover.script`
diverges at update 248; the other ten scripts did not notice. The contract must state the order, and conformance must
test it directly, not through game traces.

**Creation needs no queue.** Making a new entity visible to every iteration that starts after its creation, and
invisible to the one running, replaced the spawn queue and its six flush points, and the traces stayed identical. The
original's state hash had a section for the queue; the port writes its empty count there (`hash.Add(0)`) only so the
traces compare.

**Ergonomics: what was awkward.**

1. Grouping. Courtyard Key keeps two rooms alive while scrolling. The world has no group, so the port tags each actor
   with its room (`Home`) and filters (`RoomActors<T>`, 36 code lines, the largest addition). A room entity as parent would
   group for free but costs 32 ns per child draw position through the matrix path versus 2.7 for a root.
2. Construction as registration. Attaching needs an id and actors want theirs at construction, so the constructor
   creates the entity and `new Projectile(…);` stands alone as a statement. It reads like dead code.
3. Name clash. `World` is the namespace `MicroEngine.World` (tiles, `TileMotion`, `Entity`) and Courtyard Key's own
   `CourtyardKey.World` folder; inside the game, `World` resolves to the namespace, so the port writes
   `Arch1.Scene.World`, and actors already call their scene `World`.
4. The 2D helpers are fine for positions (exact) but `Rotation2D` reads back within a few ulps, not bit for bit.
5. The hot accessor must inline: `Local(id)` cost 10.6 ns with the throwing checks inline and 1.3 ns once they moved
   out of line. Every `Actor.Position` read and write pays it (hitboxes, centres, moves, drawing), so it is the one
   world call whose cost games feel; the frame measurements above use the fast version.
6. Struct attachments work (2.8 ns by reference in dense storage) but a reference is invalid after the type's array
   grows or compacts; Courtyard Key needed none.

## Verdict: the API to freeze in stage 3

Namespace `MicroEngine.Scene`. Games see `IWorld` (through their scene); the concrete type is engine-owned and should
not be named `World` (finding 3; suggestion: `SceneWorld`).

```csharp
public readonly record struct EntityId(uint Index, uint Generation)
{
    public static EntityId None { get; }        // default; generation 0 is never issued
    public bool IsNone { get; }
}

public struct Transform
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public static Transform Identity { get; }
    public Vector2 Position2D { readonly get; set; }   // exact
    public float Rotation2D { readonly get; set; }     // about Z; reads back within a few ulps
    public Vector2 Scale2D { readonly get; set; }
    public readonly Matrix4x4 ToMatrix();
    public static Transform Interpolate(in Transform from, in Transform to, float t);   // exactly `to` at t >= 1 or when equal
}

public interface IWorld
{
    int Count { get; }
    WorldClock Clock { get; }
    float Blend { get; }

    EntityId Create(EntityId parent = default);
    bool IsAlive(EntityId entity);
    void Destroy(EntityId entity);
    EntityId ParentOf(EntityId entity);
    void SetParent(EntityId entity, EntityId parent);

    ref Transform Local(EntityId entity);
    Matrix4x4 WorldMatrix(EntityId entity);
    Matrix4x4 WorldMatrix(EntityId entity, float blend);
    Vector2 DrawPosition2D(EntityId entity);
    Vector2 DrawPosition2D(EntityId entity, float blend);
    void Teleport(EntityId entity);

    T Attach<T>(EntityId entity, T attachment) where T : class;
    bool TryGet<T>(EntityId entity, [NotNullWhen(true)] out T? attachment) where T : class;
    T Get<T>(EntityId entity) where T : class;
    bool Detach<T>(EntityId entity) where T : class;
    Attachments<T> All<T>() where T : class;
}

public interface IDestroyedCallback { void OnDestroyed(EntityId entity); }

public readonly struct Attachments<T> : IEnumerable<T> where T : class
{
    public Enumerator GetEnumerator();
    public struct Enumerator : IEnumerator<T> { public T Current { get; } public EntityId Entity { get; } public bool MoveNext(); }
}

public sealed class WorldClock
{
    public float Scale { get; set; }            // >= 0; 1 by default
    public double Time { get; }                 // scaled seconds
    public float Delta { get; }                 // scaled length of the latest step; 0 while frozen
    public float FreezeRemaining { get; }
    public bool Frozen { get; }
    public void Freeze(float seconds);          // the longer freeze wins
    public void Unfreeze();
}
```

Engine side (the scene module, not games): `BeginStep(long engineStep)` before the world's step, `EndStep()` after it,
`BeginDraw(long latestEngineStep, float engineBlend)` before drawing, and the clock's `Advance(float dt)` once per
step. Struct attachments (`AttachValue`/`Value`/`DetachValue`) are not frozen: no consumer needs them yet and their
reference-invalidation rule needs a consumer to judge; they can be added later without breaking games.

Rules (the contract text; each is a test in `Scene.Tests`):

1. **Ids.** Generation-checked; a released slot is reused with a new generation; a stale id: `IsAlive` and `TryGet`
   are false, `Local`, `Get` and `ParentOf` throw `InvalidOperationException` naming the id and why.
2. **Destroy timing.** At once for queries: `IsAlive` false, `Count` drops, every iteration skips it, its subtree too.
   Its transform and attachments stay readable through its id until the world ends the step; then
   `IDestroyedCallback` attachments are told in destroy order (data still readable), and the slot is released. Never
   during an iteration. Destroying a destroyed, stale or `None` id does nothing.
3. **Creation.** At once: the id is valid and its transform writable. An iteration already running does not visit it;
   every later one does. No spawn queue.
4. **Ordering and iteration.** `All<T>` walks attachments of type `T` of live entities in attach order, stable under
   removal (compaction keeps order; swap-remove is forbidden). Attachments are keyed by the type argument, not the
   runtime type. `foreach` allocates nothing.
5. **Interpolation.** `BeginStep` keeps every transform as the step's start. Drawing reads `WorldMatrix(id)` or
   `DrawPosition2D(id)` at the world's `Blend`; at blend 1, or when nothing moved, the result is exactly the simulated
   transform. A child's matrix is its interpolated local matrix times its parent's. Root entities never build a matrix
   for `DrawPosition2D`.
6. **Teleport.** A new or teleported entity is drawn at its current transform, whatever it is when drawn, until the
   next step starts; a teleported parent carries its children.
7. **Per-world step tracking.** `Blend` is the engine's blend when this world stepped in the engine's latest fixed
   step, otherwise 1 (paused, under an overlay, in a transition): this is the fix for D11/SC-2, and why games get no
   blend-free `WorldMatrix` other than the world's.
8. **Clocks.** Each world has a clock; the scene module advances it once per step. At scale 1 the scaled step is `dt`
   bit for bit. `Freeze(s)` keeps the longer freeze; a frozen step returns 0 and subtracts its unscaled length (the
   remainder may end slightly below zero). Hitstop is `Freeze`; slow motion is `Scale`.
9. **Storage.** One dense array per attachment type with a sparse slot index, compacted at the end of the step.

What the transitional `Entity` adapter (`MicroEngine.Migration.Entities`, deleted in stage 5) must map, from what this
port needed:

| Legacy | IWorld | Behaviour change to document |
| --- | --- | --- |
| `Entity` added to an `EntityList` | `Create()` + `Attach<TBase>(id, entity)` | visible to later iterations at once (no deferral until the list's flush) |
| `Entity.Position` | `Local(id).Position2D` | none (exact) |
| `Hitbox`, `Bounds`, `Center`, `Depth` | stay on the adapter class | none |
| `Alive`, `Destroy()` | `IsAlive(id)`, `Destroy(id)` | none |
| `Removed()` | `IDestroyedCallback` | runs at the world's step end instead of the list's flush |
| `EntityList.Update(dt)` | `foreach (var e in All<T>()) e.Update(dt)` | none |
| `EntityList.Draw(renderer, camera, cull)` | depth sort over `All<T>()` (the adapter keeps the sort buffer) | none |
| `OfType<T>()`, `Overlapping(area)` | filtered walks over `All<TBase>()` | none |
| `EntityList.Clear()` | `Destroy` each | `Removed` now runs (the legacy `Clear` skipped it) |
| `EntityList.Flush()` | nothing (the world releases at the step's end) | none visible |
| several lists in one game | one world; each list tags its entities (as `Actor.Home` here) | the adapter needs the tag; see risk 2 |
| game-side interpolation (`DrawPositionAt`, `Motion.Between`, `DrawBlend`) | `DrawPosition2D(id)` | games may keep theirs until migrated |

## Risks

1. **Order dependence is latent.** One script in eleven caught a changed iteration order. Conformance tests must pin
   the order; stage 5's demo migration must compare traces before and after, as this port did.
2. **Grouping is missing.** Rooms, layers and "the level being left" will recur. Options for stage 3, neither
   measured in a game here: a `Children(EntityId)` walk with a translation-only fast path for 2D parents (today's
   matrix path is 32 ns per child), or leave grouping to games (36 code lines here). Decide before freezing, as it changes
   the interface.
3. **Clock placement.** The port calls `Clock.Advance` where the original decremented its hitstop, after the pause
   check, to keep traces identical. With the scene module advancing it before the scene's step, a pause pressed during
   a hitstop would also consume freeze time: one step of difference, not measured here.
4. **Hot-path cost.** Every `Actor.Position` goes through `Local(id)`. Keep the id check inlinable (finding 5) and
   cover it with a benchmark, or a well-meant change brings back a 8x cost.
5. **Readable-after-destroy is a footgun as well as a feature.** A game may keep using a dead entity's data within
   the step without noticing; `IsAlive` is the only guard.
6. **Constructor registration** (finding 2) will spread through games unless the documentation shows a factory
   pattern.
7. **Measurement scope.** Desktop CoreCLR on one machine; not measured on WebAssembly or Android, where per-call
   overheads differ. The game-level frame cost is dominated by code outside the world, so the world's share is below
   the run-to-run noise there.
