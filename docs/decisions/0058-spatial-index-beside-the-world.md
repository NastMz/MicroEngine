# 0058 — Nearby objects through an opt-in spatial index beside the scene world; no region system

Status: Accepted (2026-11-16). The SDK is in Alpha: a new public type carries no compatibility commitment. Evidence:
[WORLD-1](../evaluations/world-1.md). Task record: [`odd/tasks/world-1.md`](../../odd/tasks/world-1.md).

## Context

A scene's world (`IWorld`) stores, identifies, orders and interpolates entities; it answers "which entities have this
attachment" and nothing about where they are. The only spatial question the SDK answered was the physics world's
(`IPhysicsWorld2D.Overlap`, `RayCast`), so only an entity with a body could be found, at the price of a body, a shape
and the collision rules that come with them. A developer outside the repository could not write a swarm whose members
look at their neighbours, towers that choose a target, bullets against enemies kept as plain data, or a world much
larger than the view that updates and draws only what is near it, without writing a grid of cells first.

WORLD-1 measured where each way stops within one 60 Hz update on the reference machine: a walk over every entity for
every entity, under 2,000 entities; the tile physics world with a body each, about 6,000; and a world of 200,000
entities drawn in a window, nothing left of the frame. It also found three costs inside the engine that grew with the
size of the world rather than with the work asked for: the release of destroyed entities (a pass over the whole world
for one destruction), the copy that keeps each step's starting transforms (twenty times slower for worlds of 130,000
to 260,000 entities, by the placement of two arrays in memory), and the tile backend's broad phase (every shape of
the cells an operation reached was a candidate, sorted on every query).

The survey of managed libraries found none to reuse for the index: the maintained ones (RBush, NetTopologySuite)
return a new list per query and declare nothing about trimming or NativeAOT; the rest are unmaintained, tied to
another framework or are ECS frameworks, which the issue rules out as a compulsory model.

## Decision

- **`SpatialIndex2D<T>` is a public type of `MicroEngine.Scene`** (namespace `MicroEngine.Scenes`, in the
  `MicroEngine` package): the game's own items (entity ids, attachments, indices into its arrays), each under a point
  or a rectangle, found by rectangle or by circle. It is a grid of square cells behind a hash table; items move, a
  query or a move allocates nothing, and results come in an order that depends only on the calls made.
- **It is beside the world, not in it.** `IWorld` gains nothing and no scene has an index unless the game creates
  one. The world does not know which entities moved (a transform is written through a reference), so the game tells
  the index, in the place where it already moves the entity. An index of anything else (bullets in an array) works
  the same way.
- **No package of its own.** The type needs only `RectF` (kernel) and sits with `EntityId`, its usual item; the
  assembly it is in already references nothing but the kernel. A package would cost a release unit for one class.
- **No region, chunk or activation system.** "Update and draw only what is near the view" is a rectangle query with
  the camera's `VisibleBounds`; which entities sleep, and what sleeping means, is the game's rule. Loading by region
  stays what the asset scopes and one map per region already allow.
- **Responsibilities stay apart.** The index answers "what is near" for whatever the game files; the physics world
  answers collisions, rays and overlaps for bodies; `Camera2D` answers what is visible; asset scopes own what is
  loaded. None calls another.
- **The tile backend's broad phase is the same structure.** `ShapeGrid` files box shapes in a `SpatialIndex2D<int>`
  and sorts what it returns, so its candidates are still in shape-slot order and every result is the one a scan of
  every shape gives; the candidates are now the shapes that touch the area, not every shape of its cells.
- **The world's costs follow the work.** Releasing destroyed entities costs what is released: the creation order and
  the attachment stores keep their holes, which every walk already skipped, and close them in order once they are an
  eighth of their length. Both transforms of every slot live in one array, whose halves are a whole number of memory
  pages apart, and the step's start is copied a block at a time. Iteration order, attach order and the state hash
  are unchanged (pinned by a test written against the previous implementation).

## Alternatives considered

- **Queries on `IWorld`** (`world.Near<T>(point, radius)`): the world would have to track every transform write or
  rebuild an index every step for every scene, a cost for games that never ask; and it would only find entities.
- **A body for everything** (the physics world as the only index): measured at about a third of the index's
  capacity after the broad phase change, and it makes every findable thing a collider.
- **A quadtree, a loose grid, sweep and prune or a bounding volume hierarchy**: each wins for sizes that differ by
  orders of magnitude, for static sets or for rays; none was needed by a measured scenario. The public API does not
  expose the grid beyond the cell size, so the structure can change.
- **RBush or NetTopologySuite as a dependency**: a list allocated per query, doubles, items that implement the
  library's interface or wrap an envelope object, and no trimming or NativeAOT declaration.
- **A sorted result order** (by handle): a sort per query for an order no measured consumer needs; a caller that
  wants one sorts its results.

## Consequences

- One more public type to keep, documented in the guide ([Large worlds and nearby objects](../guide/large-worlds.md))
  with an example the tests play.
- The index is exact and unsorted; nearest-first, ray and segment queries are the caller's or a later addition.
- An index is not part of a world's state hash: it is derived from the positions the game gives it.
- The tile backend depends on the index's correctness; its conformance and broad-phase tests, unchanged, and the
  stress harness's checksums cover it.
- Deferred, with the measurement behind each, in [WORLD-1](../evaluations/world-1.md), section 9.
