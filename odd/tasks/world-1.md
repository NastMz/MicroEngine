# WORLD-1: 2D world scalability and spatial queries

Objective: measure where a large or dense 2D world stops, name a reproducible consumer scenario for each limit, and for
each candidate decide to optimize, reuse an external component or defer, implementing what the evidence justifies.
Issue NastMz/MicroEngine#126. Branch `feat/world-1`, from `main` at d29b65e. Delivery: local commits per work unit; no
push, pull request or release without the owner. Test runner: `dotnet test MicroEngine.slnx -c Release`. Evaluation:
[`docs/evaluations/world-1.md`](../../docs/evaluations/world-1.md). Builds on [PERF-3](perf-3.md), whose section 5.1
holds the starting numbers.

## Specs

Quoted fragments are the issue's text, verbatim (L1).

- S1 Why: "Dense or large 2D games may need efficient spatial queries, region activation and visibility policies.
  Investigate only after establishing current limits; do not introduce a compulsory ECS/chunk system."
- S2 Iteration and nearby objects: "Entity/world iteration costs and spatial indexing for nearby-object queries."
- S3 Regions: "Updating, rendering and loading only relevant regions for larger maps."
- S4 Responsibilities: "Coordination with physics broad phase, camera view and asset scopes without conflating
  responsibilities."
- S5 Reuse and stress cases: "Existing libraries/algorithms vs minimal extensions; profile real stress cases including
  PERF-3."
- S6 Done when: "Measured evidence identifies any scalability limit, a reproducible consumer scenario and a
  recommendation to optimize, reuse an external component or defer."
- S7 Status: "Conditional idea, not a beta blocker."
- S8 Judge by an external developer (owner's rule, as in NAV-1 S7): could someone outside the repository build that
  game today with the published SDK (a swarm whose members look for their neighbours, towers choosing targets, bullets
  against enemies without physics bodies, a world much larger than the view). A research issue ends in the
  implementation the evidence justifies; what is deferred carries its measured reason. The engine is in Alpha: a new
  public type or a behaviour change is valid and is recorded (ADR, release notes), not asked.
- S9 Bounds (this task's brief): nothing compulsory (no ECS, no chunk system a game must adopt); anything added is
  opt-in and genre-neutral; no redesign of an existing subsystem beyond what a measured limit justifies; existing
  contracts stay (the tile backend's candidates in shape-slot order, the world's state hash and iteration order); no
  new GitHub issue; nothing pushed, published or released.
- S10 Evidence words (this task's brief): the evaluation and the report distinguish what a measurement demonstrated,
  what is a hypothesis, and what a player or a developer would perceive.
- S11 Assumption: the budget every limit is read against is one 60 Hz update, 16.667 ms of CPU work at the 99th
  percentile, as in PERF-3; the scenarios are synthetic and a game's own logic comes on top.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Consumer scenarios in StressBench, measured on the code as it is (neighbours by brute force and through the physics world, a world larger than the view, a few destructions in a large world) | S2 S3 S5 S6 | inline | pending | |
| T2 Where the tile broad phase spends its time (the hypotheses of PERF-3 §8.1 and §8.7, isolated) | S4 S5 | inline | pending | |
| T3 Survey of managed libraries and the standard algorithms | S5 | inline | pending | |
| T4 Decision per candidate and the implementation that passes the bar, test first | S2 S3 S6 S8 S9 | inline | pending | |
| T5 After numbers, a sample quoted by the guide, API reference, release notes | S6 S8 | inline | pending | |
| T6 Evaluation, ADR, architecture, AGENTS.md | S1-S10 | inline | pending | |

## Log

- L1 (2026-11-16) The issue, verbatim:

  > WORLD-1 — Investigate 2D world scalability and spatial queries
  >
  > Why: Dense or large 2D games may need efficient spatial queries, region activation and visibility policies.
  > Investigate only after establishing current limits; do not introduce a compulsory ECS/chunk system.
  >
  > Research:
  > - Entity/world iteration costs and spatial indexing for nearby-object queries.
  > - Updating, rendering and loading only relevant regions for larger maps.
  > - Coordination with physics broad phase, camera view and asset scopes without conflating responsibilities.
  > - Existing libraries/algorithms vs minimal extensions; profile real stress cases including PERF-3.
  >
  > Done when: Measured evidence identifies any scalability limit, a reproducible consumer scenario and a
  > recommendation to optimize, reuse an external component or defer.
  >
  > Status: Conditional idea, not a beta blocker.

  The brief that opened this task adds S8 to S10 and asks for the implementation the evidence justifies.
