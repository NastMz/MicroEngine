# NAV-2: what a game writes after it has a path

Evaluation of 2026-11-20, on `main` at 2143542f, for NastMz/MicroEngine#179. It follows the method of
[CONSOLIDATION-1](consolidation-1.md), section 1, and reopens the line of [ADR 0056](../decisions/0056-navigation-optional-package.md)
"behaviour stays in games ... each waits for a measured need". Decision record:
[ADR 0069](../decisions/0069-navigation-agents.md). Task record: [`odd/tasks/nav-2.md`](../../odd/tasks/nav-2.md).

Machine: Ryzen 5 7600X, Windows 11, .NET SDK 10.0.400. Other builds were running on it during this work: the
timings of section 6 are an order of magnitude, and no decision rests on them.

## 1. What the audit said, checked against the code

The audit's line for navigation is one row: "Extend: path follower, steering and separation, clearance,
reachability, time-sliced search". Read against `src/MicroEngine.Navigation` at the start of this work:

- **Following.** `samples/Pathfinding/RoomScene.cs` held a `Walker` class, `Plan`, `Follow`, `Step`, a grid version
  per walker, the cells of its path, a `_chasedCell` to know when to search again and a block per kind of agent that
  called them in the right order: some sixty lines that every game with a moving thing repeats, and the guide page
  quoted them as the way to do it.
- **Other agents.** Nothing. The guide said "keep agents apart with your own rules", and the sample's swarm had
  none.
- **Size.** `FindPath(..., radius)` used the radius to straighten the route and searched as if the agent were one
  cell wide. The guide's advice for a larger agent was a second grid with the cells next to walls blocked by hand.
- **Reachability.** A search to a place that cannot be reached looks at every cell the agent reaches (NAV-1 measured
  it: several frames on a 1000 x 1000 map), and there was no way to ask "would this wall cut the way" without
  building the wall and searching.
- **One long search** could not be divided: `MaxExpansions` only gives up.

The audit was right on each point. One thing it did not name turned up while writing the sample: a route that
started on costly ground was straightened through it (the cost of the cell the agent stood in licensed the leg).

## 2. The capabilities, by the method

One pass of A to I for the group, since the five share a need and an owner; where a capability differs it is said.

**A. Need.** Any game where something walks somewhere: an enemy after the player, units sent to a clicked point
(alone or forty at once), creeps on a field where the player builds, a patrol and a door, a companion. Today the
developer writes, per game: waypoint advance, arrival, "search again when", keeping bodies apart, group arrival
(the units cannot all stand on the point), a second grid per unit size, a flood fill for "can I build here", and a
budget for searches. None of it is a rule of their game.

**B. Current experience.** Section 1. A developer outside the repository can build the chase and the patrol by
copying the sample; cannot build the group order or the tower defence without writing a crowd.

**C. Alternatives**, mature ones first (looked at on 2026-11-20):

| Candidate | What it is | Version, licence, state | Verdict |
| --- | --- | --- | --- |
| `DotRecast.Detour.Crowd` | Detour's crowd (path corridors, sampled-velocity avoidance, separation) in C# | 2026.3.1 on NuGet (2026-08-02), Zlib, repository active (2026-10-03) | Works on a navigation mesh only. ADR 0056 judged the mesh toolchain the wrong tier for tile games; agents of a `NavGrid` would need a mesh built from the grid and a second representation kept in step with it. Not run here |
| RVO2-CS (`snape/RVO2-CS`) | ORCA: reciprocal collision avoidance among discs | Apache-2.0, repository pushed 2026-11-13; no package on NuGet (a search for "rvo2" finds only `Retyped.rvo2`, a JavaScript binding) | Source only, so it would be vendored; it solves avoidance alone (no following, arrival, re-planning or walls of a grid) and owns its own agent list and neighbour tree. Kept as the next step if the simple rules prove too weak (section 5). Not run here |
| `SharpSteer2` | OpenSteer's behaviours | 10.0.0 on NuGet, 2015-08-29 | Stale for eleven years |
| `SharpNav` | Recast in C# with a crowd | 1.0.0-alpha2, 2015-05-12 | Stale |
| The engine's own | `SpatialIndex2D` (ADR 0058), scene systems (ADR 0060), `TopDownController2D` (ADR 0068), `NodeHeap` and the grid | in the repository | Used: neighbours, lifecycle and state hash, a body the game moves, the search |

No library gives a tile game "send this unit there". The two that are alive each solve one layer under a
different representation of space. The shapes of the mature engines' answers were the reference for D: an agent
object with a destination, a radius, a status and a velocity the game may apply itself (Godot's `NavigationAgent2D`,
Unity's `NavMeshAgent`).

**D. Target experience**, written before the API (it became the sample and the tests):

```csharp
var agents = Systems.Add(new NavAgents(grid));
var unit = agents.Add(spawn, new NavAgentSettings(radius: 5, speed: 80));
unit.MoveTo(click);                                        // an order
foreach (var other in selected) other.MoveTo(click);       // a group order
if (agents.CanBlock(tower)) grid.SetBlocked(tower, true);  // a tower; creeps go around
if (creep.Status == NavAgentStatus.Arrived) ...            // what happened
```

**E. Responsibility limit.** The package owns how to get there: the route, following it, arriving, searching again,
not walking through each other or through walls, what can be reached. The game owns where and why: targets,
formations, who yields, what a blocked unit does, towers, damage, saves of its own units. `IsBlocked` reports and
does not decide. A group is its units; there is no group object.

**F. Integration.**
- *Scene systems*: `NavAgents` is a `SceneSystem`: stepped before the scene's `Update`, in the scene's state hash,
  cleared when the scene leaves. `Step(dt)` is public for a system kept outside a scene.
- *State hash and determinism*: agents are stepped in the order added, searches served in the order asked, the
  budget is a number of nodes, neighbours are sorted by that order before anything is summed. Only sums, products
  and square roots are used. The tour of the sample is traced twice and compared.
- *Physics*: an agent moves along the grid by `NavGrid.Slide` and needs no physics body. `MovedByGame` hands the
  body to the game (a `TopDownController2D`, tested), with the system steering.
- *Neighbours*: one `SpatialIndex2D<NavAgent>`.
- *Saves*: routes are not saved; a game saves positions and destinations and orders again. Stated in the guide.
- *Input, assets*: none.
- *Trimming and AOT*: no reflection; the package stays `IsAotCompatible`.

**G. Trade-offs.**
- *Steering rules, not ORCA.* Seek and arrive, separation, a predictive turn aside, bodies moved apart every step,
  and one rule against the symmetric deadlock (two that head the same way into one place: the one added first goes
  first). It is a few hundred lines, deterministic and cheap, and it queues at doors instead of flowing; two dense
  crowds meeting in a corridor get through slowly. ORCA would flow better and costs a vendored solver. The limit is
  stated on the public page.
- *Clearance by whole cells around the centre.* An agent wider than a cell needs a passage three cells wide (five
  when wider than three). The alternative (a square anchored at a corner, Harabor and Botea's annotated grids)
  fits a two-cell unit through a two-cell gap but moves the body's centre off the cell centres, which every other
  contract of the grid assumes. Conservative and simple was chosen; finer cells are the way around it.
- *Group arrival by contact.* An agent has arrived when it stands beside one that arrived at the same place and
  finds no place nearer for a quarter of a second. The group's shape depends on the order of arrival; there is no
  formation.
- *Standing bodies hold eight times as hard.* Walkers go around those that stand instead of shoving them off their
  spot; a standing body in the only passage blocks it (`IsBlocked` tells).
- *Time slicing by nodes, not milliseconds*, so a run repeats.

**H. Decision: Extend.** The search, the grid and the fields are kept as they are (their tests did not change);
five capabilities are added on them in the same package. Nothing was remodelled: `INavGraph`, `PathFinder.Find`
and `NavGrid.FindPath` keep their signatures. Two behaviours of `FindPath` changed and are in the migration notes
(the radius takes part in the search; a route does not cross costly ground because it starts on it).

**I. Validation.** Section 4.

## 3. What was built

| Capability | Public surface |
| --- | --- |
| Clearance | `NavGrid.ClearanceAt`, `Fits`, `ForAgent(radius)`; `FindPath`, `IsOpen` and `DistanceField` take the radius into the search |
| Reachability | `NavRegions`: `Connected`, `RegionOf`, `Count`, `SizeOf`, `NearestReachable`, `StaysConnected` |
| Sliced search | `PathFinder.Begin`, `Continue`, `IsSearching`; `PathStatus.Searching` |
| Agents | `NavAgents` (scene system), `NavAgent`, `NavAgentSettings`, `NavAgentStatus`: `MoveTo`, `Follow(field)`, `Stop`, `Status`, `Path`, `Route`, `IsBlocked`, `DesiredVelocity`; `CanBlock`, `Query`, `Occupied`, `Regions`, `SearchBudget` |
| Steering | `Steering.Seek`, `Flee`, `Arrive`, `Separate`, `Avoid`, `Limit`; `NavGrid.Slide` |

## 4. Validation

- **The two games of the issue** are scenes of `samples/Pathfinding` and tests of `tests/Samples.Tests`: a group of
  twelve units of two sizes sent to one point (the small ones take a one-cell gap, the large ones the wide way,
  all arrive around the point), and a wave that walks to the keep while a wall of towers is built across the field
  (the last tower is refused; one on a creep is refused; the wave goes through the gap left). The room of NAV-1 is
  now built from agents: its `Walker`, `Plan`, `Follow`, `Step` and the per-agent bookkeeping are gone.
- **Against exhaustive answers**: sized paths against Dijkstra on a grid dilated by hand; `NearestReachable`
  against every cell of the region; `StaysConnected` against building the wall and searching; a sliced search
  against the whole one (same path, cost and nodes).
- **In every step of every crowd test** no body is in a wall and no two bodies overlap by more than a fifth of a
  radius (a quarter in the sample's tests).
- **Crowds under pressure** (`CrowdTests`, after the correction of section 7; cells of 1, 60 steps a second):

  | Scenario | Result |
  | --- | --- |
  | 400 agents of radius 0.3, speed 4, sent to one point through a gap of one cell | all through and arrived after 9894 steps (165 s); worst overlap 6 % of the two radii; 0 steps over 10 % |
  | 200 followers of one field through the same gap | all through and arrived within 90 s; none arrived before the wall; farthest 8.4 cells from the goal |
  | 60 agents on open ground, speed 4 and speed 1, `MoveTo` and `Follow` | all arrived; farthest 3.4 to 4.4 cells from the point (a packed disc of 60 reaches 2.6) |
  | 40 agents of radius 0.8 and 1.2 sent behind a closed wall | all arrived (unreachable), none blocked |
  | 40 agents of radius 1.2 and 10 of radius 1.5 through a gap of three cells | all through and arrived (the 40 within 100 s) |
  | 40 agents of radius 0.8 at a gap of two cells (too narrow) | all arrived before it |
  | 60 agents of radius 0.25 at speed 40 (a step of 2.7 radii) | all arrived; worst overlap 8 %; 0 steps over 10 % |
  | Two streams of 20 head-on in a passage one cell wide | all through after 2778 steps; worst overlap 15 % |
- **Determinism**: the same orders give the same hash at each of 400 steps, and a position moved by a hundredth of
  a unit gives another; the sample's scripted tour is traced twice (identical) and drawing changes no state.
- **Allocation**: 600 steps of 24 agents with orders, searches and a changing map allocate 0 bytes after warm-up.
- **Looked at**: the sample's tour was run with captures and its images read: routes drawn, the patrol going
  around the closed door, the swarm gathered, the group gathered around both points, the refused tower flashing,
  the wave bending around the towers. Two defects were found by looking and fixed (an agent "arrived" pressed
  against a wall far from its point; a chaser walking on through mud), each with a test.

## 5. Not done, and why

- **An order that ends by itself when nothing lets the agent through.** An agent held by a body that stands in the
  only passage stays `Moving` and `IsBlocked`. A time limit was tried in thought and dropped: an agent at the back of
  a long queue also makes no way for many seconds, and would give up. The game knows which it is.

- **ORCA / reciprocal velocity obstacles.** The simple rules pass the two games. The need would be dense opposing
  flows; RVO2-CS is the candidate, behind the same `NavAgents`.
- **Formations and group objects.** The game's (E).
- **Agents on `NavGraph` or a custom `INavGraph`.** Following needs a walkable area around the path, which a graph
  of points does not describe. The steering functions and the search serve those games; the guide says so.
- **Corner-anchored clearance** for units exactly as wide as a gap of an even number of cells (G).
- **Navigation meshes, generated platformer links, hierarchical search**: untouched, as ADR 0056 left them. No
  capability of this issue needs them.
- **The four demos** use no navigation package and were not changed (outside this task's surface); whether Courtyard
  Key's enemies should be agents is a question for the pass that migrates the demos.

## 6. Cost

`benchmarks/NavCrowd` orders N agents of radius 5 to the middle of a 128 x 128 grid with 1500 scattered walls and
steps 900 times. On the machine above, with other builds running:

| Agents | Mean step | Waiting for a route after 6 steps |
| --- | --- | --- |
| 100 | 0.35 ms | 31 |
| 250 | 0.42 ms | 181 |
| 500 | 0.81 ms | 431 |
| 1000 | 2.2 ms | 931 |
| 2000 | 6.3 ms | 1931 |

The default budget (4096 nodes a step) finds about a dozen cross-map routes a step on this map, so a thousand units
ordered at once should all be walking after about a second and a half (estimated from the first steps, not
measured). For one goal a `DistanceField` gives every agent
its route in one computation (`agent.Follow`). The last third of each run allocated between 0 and 2 kB (lists
still growing).

## 7. Correction after an independent verification (2026-11-20)

A verification of the branch confirmed the searches, clearance, regions, sliced search and determinism, and found
six defects in crowds. Each has a test that was seen to fail first (`CrowdTests`), except where said.

| Defect | Cause | Change |
| --- | --- | --- |
| Followers of a field stopped before a gap (142 of 200 through) and far from the goal | Progress was measured in whole cells, and an agent stopped at its first contact with one that had arrived | Arrival by taking a place (below); a follower in sight of its goal heads straight for it |
| Wide groups never ended their order (0 of 40 behind a closed wall; 3 of 40 through a gap of three cells) | Nobody could stand on the point while the others pushed it off; two bodies wedged at the mouth, pressed by those behind | Places; nobody walks into a body it touches; the one with less way left goes first |
| Overlap of 65 % for 4,178 steps at a gap, 92 % with a long step | Bodies walked into each other and were moved apart afterwards, four passes | Not walking into bodies; eight passes; a long step taken in parts |
| A wall on cells of the path already passed cost a search | Progress along the path's cells stopped once the straightened leg left them | Progress by the nearer cell centre; the legs themselves are checked when the grid changes, so a wall across a leg is seen at once |
| The state hash left out settings and the field followed | Not hashed | Hashed (the field by what it was computed for) |
| The last leg into a point off the centre of its cell could clip a corner (23 routes in the verifier's probe) | The step into the last cell was never checked | The route goes by the cell's centre when the direct step is not clear |

The verifier's probes were run again after the change: 0 followers arrived before the wall; 40 of 40 wide agents
arrived behind the closed wall and through the gap of three cells (radius 1.2 and 1.5); worst overlap 6 % at the gap
and 2 to 8 % with long steps; 0 searches for walls behind the agent, 1 in the next step for a wall across its leg;
hashes differ for different settings and fields; the two runs with events still give equal positions and hashes.

What it cost: the crowd goes through a gap of one cell more slowly than before (400 agents in 165 s; they pushed
through in about 90 s while overlapping), and a step costs about a third more (section 6). Not seen to fail first:
the test of bodies too wide for a gap (found with the probe after the first fixes: 4 of 40 had taken places beyond
the gap) and the head-on test (a measurement, it passed when written).
