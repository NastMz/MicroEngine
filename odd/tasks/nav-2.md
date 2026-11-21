# NAV-2: path following, steering, clearance and reachability

Objective: give a developer outside the repository what every game writes after it has a path (following it,
arriving, searching again, keeping agents apart, agents with a size, reachability, searches spread over steps), in
`MicroEngine.Navigation`. Issue NastMz/MicroEngine#179, an initiative of
[CONSOLIDATION-1](consolidation-1.md). Branch `feat/nav-2`; one pull request, opened by the owner's session (nothing
is pushed from this record's work). Test runner: `dotnet test MicroEngine.slnx -c Release`. Evaluation:
[`docs/evaluations/nav-2.md`](../../docs/evaluations/nav-2.md). Decision:
[ADR 0069](../../docs/decisions/0069-navigation-agents.md).

## Specs

Quoted fragments are the issue's text, verbatim (L1).

- S1 Path follower: "A path follower (waypoint advance, arrival, replan on grid version)". Acceptance: an agent sent
  to a point walks around walls and stops on it; it searches again when a change of the grid closes its way and not
  otherwise; a point that cannot be reached sends it as near as it gets.
- S2 Steering and avoidance: "steering primitives (seek, arrive, separation) with local avoidance for groups".
  Acceptance: the rules are public functions; agents of one system keep apart, pass each other in a passage, and a
  group sent to one point gathers around it; in every step no body is in a wall and no two bodies of a crowd are on
  top of each other ("units stack on one cell" no longer holds).
- S3 Clearance: "clearance-aware search for larger agents" ("Agents wider than a cell are ignored by the search").
  Acceptance: a body wider than a cell is not led through a gap it does not fit; its paths are the cheapest among
  those it fits (checked against an exhaustive search).
- S4 Reachability: "a connected-region and \"would this block the path\" query" ("tower-defence placement has no
  reachability query"). Acceptance: both answers agree with building the wall and searching; asking changes nothing.
- S5 Time-sliced search: "time-sliced search" ("a search cannot be spread over frames"). Acceptance: a search
  continued a few nodes at a time ends with the path, cost and work of the whole search; agents' searches share a
  budget per step.
- S6 Limit of responsibility: "Formation and game-specific tactics stay in the game."
- S7 Games: "An RTS group move order and a tower-defence wave with run-time placement are built from the package, as
  tests and in the sample."
- S8 Determinism: "Deterministic results kept." A system in a scene is part of the scene's state hash.
- S9 Records: "ADR 0056 amended, guide and release notes."
- S10 Method (CONSOLIDATION-1 S5, S7): alternatives looked at before building; contracts, lifecycle, errors, tests,
  documentation, sample and migration notes delivered with the code.

Assumption (mine): the guide page of the package is `docs/guide/navigation.md` and the amended record is
`docs/decisions/0056-navigation-optional-package.md`; both are named by S9 and neither was in the list of edit
surfaces handed to this task, which named `docs/manual/**`. They were edited as the package's own page and record;
see L9.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Read the audit, ADR 0056, the package, the sample and the integration points (scene systems, controllers, spatial index) | S10 | inline | done | evaluation §1 |
| T2 Clearance: `ClearanceAt`, `Fits`, `ForAgent`; the radius in `FindPath`, `IsOpen`, `DistanceField` | S3 | inline | done | `85764438`; `ClearanceTests` 6/6 (L2) |
| T3 Reachability: `NavRegions` | S4 | inline | done | `b6ed2eb4`; RED: does not compile; `NavRegionsTests` 5/5 (L3) |
| T4 Sliced search: `PathFinder.Begin`, `Continue`, `PathStatus.Searching` | S5 | inline | done | `27087ed3`; RED: does not compile; `SlicedSearchTests` 3/3 |
| T5 Agents: `NavAgents`, `NavAgent`, `NavAgentSettings`, `Steering`, `NavGrid.Slide` | S1 S2 S5 S6 S8 | inline | done | `e494062b`; RED: does not compile, then 3 of 73 failing (L4); `NavAgentsTests` and `SteeringTests` |
| T6 Sample: the room built from agents, the group order, the tower defence, the route by hand; its tests and tour; the guide | S7 S9 | inline | done | `1b08244a`; `PathfindingTests` 11/11; captures looked at (L5, L6) |
| T7 Evaluation, ADR 0069 and its row, ADR 0056's status, this record, `benchmarks/NavCrowd`, API reference | S9 S10 | inline | done | the documentation commit (L7, L8) |
| T8 Correction batch after the independent verification: crowds under pressure (D1 to D6) | S1 S2 S3 S8 | inline | done | the correction commit; RED: 9 of 9 new tests failing; `MicroEngine.Navigation.Tests` 88/88, `PathfindingTests` 11/11 (L10) |

## Log

- L1 (2026-11-20) The issue, verbatim:

  > ## Problem
  > Path search is solid (A*, smoothing, distance fields), but the engine stops at the path: following it, replanning
  > when the grid changes, keeping agents apart and arriving are rewritten by every game, and units stack on one
  > cell. Agents wider than a cell are ignored by the search, tower-defence placement has no reachability query, and
  > a search cannot be spread over frames. ADR 0056 (NAV-1 #112) left behaviour to games for "no measured need".
  >
  > ## Target
  > A path follower (waypoint advance, arrival, replan on grid version); steering primitives (seek, arrive,
  > separation) with local avoidance for groups; clearance-aware search for larger agents; a connected-region and
  > "would this block the path" query; time-sliced search. Formation and game-specific tactics stay in the game.
  >
  > ## Acceptance
  > - [ ] An RTS group move order and a tower-defence wave with run-time placement are built from the package, as
  >   tests and in the sample.
  > - [ ] Deterministic results kept.
  > - [ ] ADR 0056 amended, guide and release notes.
  >
  > ---
  > Part of CONSOLIDATION-1; audit in `docs/evaluations/consolidation-1.md`. Judged by what a developer outside the
  > repository must write, not by what the current demos use or by a benchmark.

- L2 (T2) The tests of this slice were written before the code but first run after it (no RED was seen for it).
  Their first run found two things the design had missed: a wide body pressed into a corner has its centre in a cell
  it does not fit in and could not leave it across the corner (fixed: a body leaves a tight cell any way it can), and
  a point in a cell the body fits in can still be too near a wall for it (fixed: the route ends beside it). Clearance
  is measured in whole cells around the centre; the corner-anchored alternative and why it was not taken are in the
  evaluation, section 2 G.
- L3 (T3) `StaysConnected` for bodies wider than a cell refuses an area that a body centred on one of the places
  would touch; the first version let that place "walk out", which disagreed with building the wall and searching.
- L4 (T5) What the first runs of the crowd tests found, in order: (1) two agents heading into one gap each gave way to
  the other for ever (separation and avoidance are symmetric): the agent added first now ignores, in both rules, a
  later one that heads the same way; (2) agents held off the group that had arrived by its separation never touched
  it: bodies that stand are gone around and stood beside, not kept away from; (3) followers of a field jostled for
  the middle of the goal's cell for ever: in that cell, pressing against others of the same field counts; (4) a group
  arrived as a line when each stopped at its first contact: an agent now stops when it finds no place nearer for a
  quarter of a second; (5) a wall kept one body of an overlapping pair from giving way: the other gives what the
  wall kept.
- L5 (T6) Looking at the captures found what the tests had not: a unit "arrived" pressed against a wall far from the
  point (the rule of (3) applied to routes: removed, it holds for fields only), and the chaser walking on through
  mud. The second had two causes, each fixed with a test: an agent passed a waypoint when within its own radius of
  it, cutting the corner over costly ground (it now passes it on reaching it, or as soon as the way to the next is
  clear and no costlier); and `NavGrid`'s straightening let a route that starts on costly ground cross it (the cost
  of the cell the agent stands in no longer counts; RED seen by restoring the old line). The second is a change of
  `FindPath` and `Smooth` and is in the migration notes.
- L6 (T6) The sample's room lost `Walker`, `Plan`, `Follow`, `Step`, the per-walker grid version and cells and the
  chased cell. `Courier.cs` keeps the route followed by hand, for the guide's section on the searches.
- L7 (T7) Alternatives were looked up on 2026-11-20 on nuget.org and the projects' repositories (versions, dates and
  licences in the evaluation). None was run.
- L8 (T7) `benchmarks/NavCrowd` was run once while other builds were running on the machine; its table is an order
  of magnitude.
- L9 Outside the edit surfaces handed to this task, and needed by it: `docs/guide/navigation.md` (edited: it is
  the package's page and quotes the sample), `docs/decisions/0056-navigation-optional-package.md` (edited: one
  status paragraph, S9), and, not edited, the places that still describe the
  package as pathfinding alone: `site/scripts/api-reference.mjs` (the description at the head of the namespace's API
  page), `docs/architecture.md` (lines 85 and 301), `docs/guide/sdk.md` (the package table),
  `docs/guide/getting-microengine.md` (the package table) and `docs/guide/project-structure.md` (the samples row).
  `sdk/tests/docs.mjs` plays the sample's tour, which is now 1802 updates instead of 482; it needs no change.
- L10 (T8, 2026-11-20) The coordinator's verification, in its words: "found the search, clearance, regions, sliced
  search and determinism all hold (bit-identical to main at radius 0), but found defects in crowds", D1 to D6
  (followers of a field that plug a passage and report `Arrived` before the wall; wide groups that never end their
  order; overlap of 65 % and 92 %; a search for cells already passed; settings and the field missing from the hash;
  a last leg that clips a corner). Nine tests were written from its probes' scenarios and all nine failed, with the
  verifier's numbers (65 %, 4,178 steps, 92 %). What changed, and what was tried and dropped on the way:
  - Giving all the way to the agent added first when two overlap squeezed whoever stood between two with priority
    (overlap went to 100 %): the share by weight stays, and priority is in steering only.
  - Priority by the order of adding let agents behind press those in front into each other at a gap: it is now by
    the way left (less goes first), with one measure for all followers of a field.
  - An agent does not walk into a body it touches (it waits behind one that walks, goes around one that stands).
    With that alone the group stopped in a line behind the first to arrive; with a time to go around, it never
    stopped. Arrival is now by places: the free one nearest the point on a honeycomb, with the old rules (held
    between bodies that stand; no nearer after the time to walk six radii) kept for an agent that cannot reach it.
  - A wall across the way is walked along towards the point headed for, unless a body it must not walk into is
    that way; without the exception two bodies pressed each other at the mouth of a gap for ever.
  - Sub-steps for a step longer than half a radius; eight passes of moving apart.
  The evaluation, section 7, has the table and the numbers measured after. Not done: an order that ends by itself
  when a body that stands blocks the only passage (it stays `Moving` and `IsBlocked`); the reason is in the
  evaluation, section 5. The crowd is slower through a one-cell gap than before (165 s for 400 agents against
  about 90 s) and a step costs about a third more.
