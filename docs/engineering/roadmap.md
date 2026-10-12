# Roadmap and current work

This scenario map is not an implementation schedule. Product direction is confirmed;
technical design and engine construction are not yet authorized.

## Current work

The documentary foundation is authored for owner review. Creation does not approve
all proposed conventions or technical choices. No greenfield capability is implemented
or verified; the existing engine is the historical prototype.

Next action: review this foundation, choose a bounded discovery milestone and resolve
only the decisions it requires. Do not infer runtime implementation authorization.

## Scenario candidates

| Candidate | Purpose | Requirements | Risks and acceptance to refine |
| --- | --- | --- | --- |
| Simple Mario-inspired platformer | Movement, collision and animation | C-001, FR-002 | External game flow; no core-owned jump rules |
| Zelda-inspired top-down adventure | Exploration, transitions and interaction | C-002, FR-002 | Public contracts; game owns combat |
| Pokemon-inspired RPG | Exploration, menus and turn-based interaction | C-003, FR-002 | Game owns turns; no mandatory RPG libraries |
| Later metroidvania / modern platformer | Stress needs discovered by earlier scenarios | None committed | Candidate only; no date or exhaustive capability backlog |

Order and game scope are not approved. Use the [milestone template](templates/milestone.md)
when concrete scope exists.

## Open decisions

| Question | Resolve when |
| --- | --- |
| Initial platforms and distribution | Before selecting backends/dependencies |
| First validation scenario | Before planning construction |
| Entity/composition model and storage | Scenario requirements justify comparison |
| Runtime, time and scene semantics | Before dependent capability approval |
| Rendering, input, audio and physics choices | Relevant milestone, using evidence |
| New code placement, packaging and compatibility | Before project creation or public release |

No ECS, DI container, backend, solver, render graph, asynchronous loading, hot reload,
editor or 3D commitment is created here.
