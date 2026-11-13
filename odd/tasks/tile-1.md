# TILE-1: maps from external editors (Tiled) and large 2D maps

Objective: decide, with evidence, how an external developer gets maps made in an editor into a MicroEngine game, then
implement what the owner approves. Issue NastMz/MicroEngine#121. Branch `docs/tile-1` for the evaluation;
implementation branches follow the approved option. Delivery strategy: `ask-on-risk`. Test runner:
`dotnet test MicroEngine.slnx -c Release`. Evaluation: [`docs/evaluations/tile-1.md`](../../docs/evaluations/tile-1.md).

## Specs

- S1 Formats: "Compare importing Tiled or other open formats for tiles, layers, object placements and custom properties
  without embedding game-specific behavior in the engine."
- S2 Integration: "Evaluate existing parsers/adapters, atlas/animation references and integration with the Content
  system and physics."
- S3 Scale: "Assess larger maps, visible-region culling and incremental loading only where measurable needs exist."
- S4 Scope limit: "Do not build a native map editor by default."
- S5 Done when: "Produce an evidence-based recommendation and, if justified, a small example consuming a real map
  through the public SDK."
- S6 Judge by an external developer (owner's rule, as in UI-1 S1 and PHYS-1 S10): could someone outside the repository
  build a game whose levels are drawn in a map editor today with the published SDK. The deliverable is the
  implementation the research justifies (UI-1 S2).
- S7 Owner gate (as PHYS-1 S8): a new package or a new third-party dependency in the SDK needs the owner's explicit
  approval.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Map tile types, renderer, Content loaders, packages, samples | S2 S3 | delegated (mapping trigger: >5 lookups across Physics.Tiles, Content, Graphics2D, samples, sdk) | done | evaluation §1 |
| T2 Survey formats and .NET parsers (Tiled TMX/TMJ, LDtk; DotTiled, TiledCS, LDtk loaders) | S1 S2 | inline | done | evaluation §2 |
| T3 Evaluation `docs/evaluations/tile-1.md` with the recommendation | S1–S6 | inline | done | this branch |
| T4 Owner decision on the option | S7 | owner | done | L3: option 1; ADR 0052 |
| T5 Engine fixes: `TileRenderer` culls with `VisibleBounds`; flipped and rotated cells | S3 | inline | pending | |
| T6 `MicroEngine.Tiled`: loader, layers, tilesets, objects, properties, collision grid, tests over real TMX/TMJ | S1 S2 | inline | pending | |
| T7 Release path: package list, package checks, notices, CLI notices, architecture test | S7 | inline | pending | |
| T8 Example consuming a real Tiled map through the public SDK | S5 | inline | pending | |
| T9 Large-map benchmark, manual, release notes | S3 | inline | pending | |

## Log

- L1 (2026-11-13) Owner: "Sigue trabajando en el siguiente issue". The board's top open item is #121 TILE-1.
- L2 (2026-11-13) Mapping: the only map input is ASCII rows plus a legend (`TileLayer.Parse`); `TileRenderer` culls
  with `Camera2D.View`, which ignores zoom and rotation, so a zoomed-out camera loses the outer tiles (defect found in
  passing, `src/MicroEngine/World/TileRenderer.cs:18`). DotTiled 1.0.0 (MIT, net8.0, no dependencies, TMX + TMJ +
  templates + infinite maps) is the only maintained .NET parser that covers the whole format. Recommendation: an
  optional `MicroEngine.Tiled` package over DotTiled whose public API exposes engine types only. Next: owner decision
  (T4).
- L3 (2026-11-13) Owner: "Approve option 1". Recorded as ADR 0052. Branch renamed to `feat/tile-1`; T5 starts.
