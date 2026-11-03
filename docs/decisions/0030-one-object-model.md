# ADR 0030: One object model: ids, 3D-storage transforms, hierarchy, attachments; no core ECS

## Status

Accepted (2026-11-03). Amends [ADR 0003](0003-scenes-entities-runtime.md) (plain `Entity` classes in `EntityList<T>`).
Evidence: ARCH-1 §3 (S6 no transforms or identity, S8 no engine interpolation, D11 a frozen scene under an overlay
receives the global blend), §6.3 (the `MicroEngine.Scene` and `MicroEngine.Migration.Entities` rows), §6.4 (the
`IWorld` sketch), §7 (this ADR), §9 (stage 3), annex A3 (WO-3 to WO-5, SC-1 to SC-3, DM-1), and the P7 report
([`experiments/arch-1/p7-entities`](../../experiments/arch-1/p7-entities/README.md)) with architect directive 11
(the P7 verdict) and directive 12 (stage 3).

## Context

`Entity` was a sprite object with a 16×16 hitbox: no ids, no transform or hierarchy, no attachments, drawing inside
the object. All four demos wrapped or replaced it, and three reimplemented per-scene interpolation, one of them also
the fix for a frozen scene drawn at the global blend. ADR 0003 bundled "no ECS" with "no transform or hierarchy",
which are separate decisions. P7 ported Courtyard Key's actors, camera, interpolation and hitstop onto a prototype
world: bit-identical per-step state traces on all eleven scripts, the game's 47 tests passing, frame time within
±5 % and allocation within ±0.1 %, and twelve engine-like helpers gone from the game. It also showed that iteration
order is behaviour (a swap-remove store diverged in one script in eleven), that a per-entity dictionary of
attachments is 4.7 times slower to look up and allocates 240 B per entity, and that passing the blend to drawing
calls lets games reintroduce D11.

## Decision

- **One object model** in `MicroEngine.Scene` (an assembly of the `MicroEngine` package that references only the
  kernel; its types are in the `MicroEngine.Scenes` namespace beside the scene stack, because a `MicroEngine.Scene`
  namespace would hide the `Scene` class from every file in a `MicroEngine.*` namespace). Games program against
  `IWorld`; the engine's implementation is `SceneWorld`.
- **Ids** are `EntityId(Index, Generation)`; `None` is `default`; a stale id is detected, never aliased.
- **Transforms** are stored in 3D (`Vector3` position, `Quaternion` rotation, `Vector3` scale) with exact 2D helpers
  for positions; an optional parent; `Children(parent)` walks the direct children without allocating, and a 2D draw
  position under translation-only parents never builds a matrix.
- **Attachments** are the game's own classes, keyed by the type argument, in one dense array per type with a sparse
  slot index. `All<T>()` walks them in attach order, stable under removal (compaction keeps order; swap-remove is
  forbidden). Struct attachments are not part of v1.
- **Timing**: creation is visible at once (no spawn queue) to every iteration that starts afterwards; destruction is
  immediate for queries, data stays readable until the step ends, then `IDestroyedCallback` attachments are told in
  destroy order and slots are released, never during an iteration.
- **Interpolation belongs to the world**: drawing reads `WorldMatrix(id)` and `DrawPosition2D(id)` at the world's own
  `Blend`, which is the engine's blend only when this world stepped in the engine's latest step (per-world step
  tracking), otherwise 1. No drawing call takes a blend argument. `Teleport` (and re-parenting) draws an entity at its
  current transform until the next step.
- **World clocks** advance as part of the world's step (`BeginStep` returns the scaled step): `Scale` for slow motion,
  `Freeze` for hitstop (the longer freeze wins), and a world that does not step consumes no time.
- **State hash**: the world contributes ids, hierarchy, transforms, which attachments exist, the state of
  `IStateHashed` attachments and its clock; drawing state is excluded.
- **No core ECS.** The legacy `Entity`/`EntityList` survives only as a transitional `[Obsolete]` adapter over `IWorld`
  in the non-packable `MicroEngine.Migration.Entities`, consumed only by the demos, deleted at the end of ARCH-1
  stage 5 and guarded by the architecture test.

## Alternatives considered

- **A core ECS** (archetypes, queries, systems): more indirection than games with tens to hundreds of objects need,
  and a second model for the scene stack and UI; a data-oriented module can be layered on later.
- **Keep `Entity` as is**: leaves S6 and S8 open; every demo already works around it.
- **Two object models** (keep `Entity` beside the world, or keep it permanently as a compatibility layer): it would
  constrain the new model and double the documentation and the conformance work.
- **A per-entity dictionary of attachments**: measured 4.7x slower lookups and 240 B per entity.
- **`WorldMatrix(id, blend)`** as in the first sketch: lets a game pass the global blend and brings D11 back.

## Consequences

- Games lose their interpolation helpers, spawn queues and hitstop timers; they gain one call per position that
  must stay inlinable.
- Iteration order is part of the contract and is tested directly; demo migrations compare state traces before and
  after.
- Data readable after `Destroy` is a feature and a footgun: `IsAlive` is the only guard.
- Measured on desktop CoreCLR only; WebAssembly and Android costs are not measured yet.
