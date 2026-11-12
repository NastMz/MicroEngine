# PHYS-1: Box2D as a consumable provider, or the one 2D physics backend

Objective: decide, with evidence, how 2D physics reaches external developers (Tiles in the SDK plus Box2D optional,
Box2D as the standard, or a staged transition), then implement what the owner approves. Issue NastMz/MicroEngine#111.
Branch `docs/phys-1` for the evaluation; implementation branches follow the approved option. Delivery strategy:
`ask-on-risk`. Test runner: `dotnet test MicroEngine.slnx -c Release`. Evaluation:
[`docs/evaluations/phys-1.md`](../../docs/evaluations/phys-1.md).

## Specs

- S1 Revisit the premise first: "should MicroEngine continue maintaining its own tile collision algorithms alongside
  Box2D 3.2, or can Box2D reasonably replace the tile backend without compromising the engine's goals? Do not assume
  that publishing both—or replacing Tiles—is the correct outcome."
- S2 Feature and semantic coverage: "inventory what Tiles actually provides and whether Box2D can reproduce it,
  natively or with a modest adapter. Examine `TileGeometry` (including runtime tile changes), `MovingSolids`
  (push/carry), deterministic swept collide-and-slide, one-way platforms, sensors, queries, collision layers, and
  exact contact/skin behavior. Distinguish solver features from game-specific movement behavior."
- S3 Replacement cost: "identify precisely which proprietary collision algorithms and maintenance obligations could be
  removed, which adapters must remain or be built, and which gaps or regressions would be introduced."
- S4 Performance and distribution: "compare representative top-down/platformer/run-and-gun scenarios, dynamic-body
  scenarios, memory/allocations, startup and package footprint where measurable. Account for native binaries, AOT,
  WebAssembly, mobile, CI/build complexity and the declared support level of each target. Do not infer performance
  from C++ versus C# alone."
- S5 API and compatibility: "assess the existing `IPhysicsWorld2D` contract, capability-based errors, the `TileMotion`
  convenience API, existing demos, generated game templates, SDK dependencies and external consumers."
- S6 Confidence: "distinguish demonstrated behavior from assumptions." Reuse or extend conformance and substitution
  tests "especially fast movement, thin obstacles, simultaneous contacts, moving platforms, mutable tilemaps and
  sensors".
- S7 Options compared: "1. Keep Tiles in the default SDK and publish Box2D as an optional provider. 2. Standardize 2D
  physics on Box2D, retaining only narrowly necessary movement/tile adapters. 3. Use a staged transition".
- S8 Owner gate: "Do not remove Tiles, make Box2D mandatory, or change published package composition on your own;
  these require the owner's explicit architectural approval."
- S9 Follow-up if the provider path is approved: audit the provider, dependencies, native/AOT/web packaging,
  registration and platform coverage; verify common-capability conformance versus Tiles; "a small
  **external-consumer example** demonstrating bodies, collisions and backend selection"; close verified documentation
  gaps only.
- S10 Judge by an external developer (owner's rule, as in UI-1 S1): could someone outside the repository build a game
  that needs this physics today with the published SDK.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Map Tiles, Box2D, contract, tests, packaging | S2 S3 S5 | delegated (mapping trigger: >5 lookups across 4 projects, ADR, samples, CI) | done | evaluation §1–§2 |
| T2 Measurements: conformance runs, benchmarks, package footprint | S4 S6 | inline | done | Box2D tests 101/101, Tiles physics 48/48; `PhysicsBench --backend` (evaluation §3) |
| T3 Evaluation `docs/evaluations/phys-1.md` with the recommendation | S1–S7 S10 | inline | done | recommends option 1 |
| T4 Owner decision on the option | S8 | owner | done | L3: option 1; ADR 0051 |
| T5a Release path: Box2D packed by `sdk.yml` from `box2d-natives.yml`, eighth package in `version.mjs` (per-version list), package and external checks, CI routing | S9 | inline | done | version tests 29/29 (RED: digits regex, `packagesAt`), CI script tests 59/59, actionlint clean, external.mjs physics checks 5/5 on Windows |
| T5b Consumer sample: `PhysicsSubstitution` on package references | S9 | inline | done | runs both backends in repo and from the local feed; published win-x64 carries `me_box2d.dll` |
| T5c Documentation: ADR 0051, SDK guides, physics manual, CI and release docs, notices, release notes | S9 | inline | done | snippets check, `version.mjs check` |

## Log

- L1 (2026-11-11) Owner, choosing the next issue after the triage: "Sí, PHYS-1".
- L2 (2026-11-11) Box2D natives built locally for win-x64 (self-test passed, hash `93f3a79ebf294b23`). Kinematic
  tile scenario: Tiles 0.19 ms / Box2D 0.87 ms at 200 bodies, 1.58 / 5.02 ms at 800. Recommendation: option 1 (keep
  Tiles, publish Box2D optional). Next: owner decision (T4).
- L3 (2026-11-11) Owner: "Apruebo opción 1". Recorded as ADR 0051. T5 starts: package audit, release path, consumer
  sample, documentation.
- L4 (2026-11-11) The version tool's package-reference pattern did not match ids with digits (`Box2D`): a release
  would have left the sample's Box2D reference at the old version. Fixed with a test. Adding Box2D to the package list
  as is would have failed every sdk-release preflight on `main` (0.5.0-alpha.2 is published without it): packages now
  carry the published versions they were absent from (`shippedWithout`). Not verified: a browser build of a game
  through the package reference, iOS linking, Android and iOS on devices. NativeAOT: the sample fails in `MicroEngine.Saves` (reflection JSON, engine-wide, not Box2D); a Box2D-only AOT publish could not link on this machine (the MSVC linker of Visual Studio 18 is not found by ILCompiler 10.0.15).
- L5 (2026-11-11) Independent verifier on the release-path commit: workflows, version tool and package check pass
  (88 node tests, actionlint clean, differential `release` run identical to the baseline, live nuget.org probe gives
  `none` for 0.5.0-alpha.2). Two blockers fixed in one batch: the external project test now skips the Box2D check for
  SDK versions published without the package (a manual `finish` of 0.5.0-alpha.2 would have failed), and a change to
  `box2d-natives.yml` now runs every check like the other workflows daily CI calls.
