# Roadmap and current work

This scenario map is not an implementation schedule. Product direction is confirmed;
technical design and engine construction are not yet authorized.

## Current work

The owner approved repository transition on 2026-10-12. The prototype is preserved
at `legacy-final` and retired from the active tree. No new engine exists. See [history](history.md).

Next action: design minimal external-consumer startup/run/shutdown, including partial
initialization failure. Approve platform scope, ownership/errors and integration
acceptance before selecting a backend or creating projects. ECS and scenes are not prerequisites.

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
