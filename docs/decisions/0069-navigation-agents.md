# 0069 — Navigation moves agents: following, arrival, avoidance, size and reachability

Status: Accepted (2026-11-20). The SDK is in Alpha: public API changes carry no compatibility commitment. Amends
[0056](0056-navigation-optional-package.md) ("Behaviour stays in games"; "Not built now": clearance during the
search and local avoidance; "registers no module, no system and no scene type"). Evidence:
[NAV-2](../evaluations/nav-2.md). Task record: [`odd/tasks/nav-2.md`](../../odd/tasks/nav-2.md). Issue
NastMz/MicroEngine#179.

## Context

ADR 0056 gave the SDK a search and stopped at the path: "the package never decides what an agent wants, when it
re-plans or how it moves along a route", with clearance and avoidance waiting "for a measured need". Its one sentence
held two different things. What an agent *wants* is a rule of the game. *How it gets there* (advance along the
waypoints, stop on the point, search again when the way closes, not walk through the others, fit through the gap,
know that a place cannot be reached) is the same code in every game, and the sample of the package had to write it
by hand to show a search working. A group order and a tower defence could not be built without writing a crowd.
CONSOLIDATION-1 rules out "the game can write it" and "no demo needs it" as reasons, and the libraries that exist
solve one layer each over a navigation mesh or with no following at all (NAV-2, section 2).

## Decision

- **The line moves from the path to the destination.** The package owns how an agent gets where it was sent; the
  game owns where and why. Formations, who yields, what a blocked unit does and what is saved stay in the game.
- **`NavAgents` is a scene system** (ADR 0060) in the same optional package: stepped before the scene's `Update`,
  part of the scene's state hash, cleared with the scene; `Step(dt)` is public for one kept outside a scene. An
  agent (`NavAgent`) is a body with a radius and a speed that takes `MoveTo`, `Follow(field)` and `Stop` and tells
  `Status`, `Path`, `Route` and `IsBlocked`. The package still registers no module.
- **Agents steer by rules, not by a solver**: seek and arrive, separation, a turn aside from a body about to be
  run into, bodies moved apart every step, walls of the grid never entered. An agent does not walk into a body it
  touches: it waits behind one that walks and goes around one that stands. Two agents that head the same way into
  the same place do not yield to each other: the one with less of its way left goes first, so nobody presses those
  in front into each other. The rules are public as `Steering`.
- **A group is its units.** Each is ordered; they gather around the point because an agent that reaches those that
  arrived at the same place takes the free place nearest the point, on a honeycomb of places one body apart.
- **The level is still the grid.** Agents are not obstacles in it. A change of the grid makes the agents whose way
  it closes search again, and those that could not reach their point try again when a way opens.
- **Searches are budgeted in nodes per step** (`SearchBudget`), served in the order asked, and always to a cell
  that can be reached (the nearest of the agent's region), so an order never costs a search of the whole map.
  `PathFinder.Begin` and `Continue` expose the same slicing.
- **Size takes part in the search.** The radius given to `FindPath`, `IsOpen`, `DistanceField` and `NavRegions`
  selects the cells the body fits in, measured as whole cells of room around the cell's centre.
- **Reachability is a query** (`NavRegions`), kept up to date with the grid by the first question after a change:
  connected, nearest reachable, and whether blocking an area would cut a way.
- **A body may be the game's** (`MovedByGame`): the system steers, a character controller of ADR 0068 or anything
  else moves it.
- **Deterministic and allocation-free once warm**, like the search: the order of agents, of requests and of
  neighbours is the order of the calls, and only sums, products and square roots are computed.

## Alternatives considered

- **A crowd from DotRecast** (`DotRecast.Detour.Crowd` 2026.3.1, Zlib): mature, over a navigation mesh. Tile games
  would keep a mesh in step with their grid to move units over it.
- **ORCA from RVO2-CS** (Apache-2.0, source only): better flow in dense opposing crowds, for a vendored solver that
  does avoidance alone. Left as the next step behind the same `NavAgents` if the rules prove too weak.
- **Steering functions and a documented recipe**: every game would still write the system around them, which is the
  part with the bugs (when to search again, group arrival, the deadlock at a door).
- **Agents in the core package**: every game would carry them, and the newest API would sit in the core's surface;
  the reasons of ADR 0056 for an optional package hold.
- **Clearance anchored at a corner of the body** (annotated grids): fits even widths exactly; moves the body off
  the cell centres every other contract of the grid uses.

## Consequences

- The package is no longer "plain data and a search": it has a scene system and state that takes part in the hash.
  A game that wants only the search references the same package and uses none of it.
- `FindPath` changed behaviour in two cases (a body wider than a cell; a route that starts on costly ground), listed
  in the migration notes. `IsOpen` and `DistanceField`'s constructor gained an optional parameter; `PathStatus`
  gained `Searching`.
- Stated limits: a wide agent needs a passage of three cells (five when wider than three); crowds queue at narrow
  passages at about half the rate of a free single file; bodies pressed at a gap overlap by under a tenth of their
  two radii, and by up to a sixth where two cannot pass side by side; an agent nothing lets through stays `Moving`
  and `IsBlocked`; no formations; agents walk a `NavGrid` only.
- `samples/Pathfinding` is built from agents and holds the two games of the issue; the documentation check builds
  and plays its longer tour from the packed feed.
- Correctness is the engine's to keep: sized paths, nearest reachable cells and "would this cut the way" are
  compared with exhaustive answers; crowds are checked in every step for walls and overlap.
