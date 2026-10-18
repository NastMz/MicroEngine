# ADR-0013 — bounded consumer-owned RPG encounter

Status: accepted2026-10-18 by root under owner technical delegation after independent checkpoint_design_review PASS, before source. Navigation-direction advisory incorporated in RPG-1.
Scope [RPG-1](../specs/rpg-encounter.md), not an engine API decision or full Pokemon architecture.

## Context and decision

Owner authorizes H3 exploration/interaction/UI/transitions/simple turn combat. Reuse public
Explorer for the already verified two-room navigation; a separate RpgSession owns HP, commands,
mode transitions, results and encounter progression. Public engine image/animation/input/canvas
utilities provide presentation. No new core systems, dependency or backend needed.

## Alternatives and consequences

- Extend TurnSession: rejected; its durable four-phase tracer/codec behavior is already consumed
  and tested. Combat requires different state/semantics and must not silently change old saves.
- Reimplement movement/portals: rejected; current public Explorer is sufficient. Reusing a
  consumer assembly has explicit room/type/content coupling, not a general world API promise.
  If that coupling becomes unsuitable later, evaluate source-authored content or a demonstrated
  general capability then; do not lift a tracer into Core by default.
- Add engine battle/menu/scene/ECS/tilemap: rejected; these are not required by a single
  deterministic game-owned encounter and risk genre/style coupling.
- New general map/depth/visual tooling: deferred by explicit owner instruction and F014/F015.

Snapshot is a small consumer value, not an engine report/storage format; no serialization or
compatibility promise for RPG checkpoints. Direct project references and unchanged original
licensed content links keep ownership visible. Native calls stay in Desktop or verification
probe, never game/model. Current Windows validation only; no portability/export guarantee.
Independent review checks frame/input consumption, terminal/retaliation ordering, strict zone,
repeat policies, oldconsumer integrity and public-only reuse before delegated approval.
