# DEBUG-1: developer diagnostics and runtime inspection

Objective: find what an external developer cannot diagnose today with the published SDK, then implement the justified
primitives, optional and cheap when disabled. Issue NastMz/MicroEngine#119. Branch `feat/debug-1`. Delivery strategy:
`ask-on-risk`. Test runner: `dotnet test MicroEngine.slnx -c Release`. Evaluation:
[`docs/evaluations/debug-1.md`](../../docs/evaluations/debug-1.md).

## Specs

- S1 Goal: "A developer needs to diagnose why their MicroEngine game misbehaves without reverse-engineering the engine.
  Current counters/profiling exist but may not provide actionable cross-system insight."
- S2 Inspection scope: "In-development inspection of entities/world transforms, active scenes, cameras, physics
  collision shapes/contacts, device input and consumed actions."
- S3 Measurement scope: "Per-system CPU/frame timing, GPU draw calls and render state, managed allocations/GC,
  texture/asset lifetimes and scopes, audio voices."
- S4 Tooling: "Useful error messages, logging, diagnostics capture and a possible debug overlay or inspector;
  prioritize reusable infrastructure over prebuilt game UI."
- S5 Reuse: "Determine what the SDK already exposes and how to integrate existing .NET tooling and diagnostics rather
  than recreate a profiler."
- S6 Done when: "A gap assessment and a small, usable developer-side diagnostic workflow are demonstrated; implement
  only justified primitives, keeping diagnostics optional and cheap/disabled in production."
- S7 Judge by an external developer (owner's rule): could someone outside the repository diagnose a misbehaving game
  with the published SDK; research ends in implementation of the primitives that fail that check.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Map the existing diagnostics surface per area | S1–S5 | delegated (mapping trigger: every subsystem, >5 lookups) | done | evaluation §1, spot check of `GraphicsDeviceBase.Counters` and `FrameSchedule` |
| T2 Evaluation `docs/evaluations/debug-1.md` with the gap list and plan | S1–S7 | inline | done | 9 scenarios, 9 primitives |
| T3 Owner approval of the primitives (public contract additions) | S6 | owner | done | L3 |
| T4a `Game.Diagnostics`, per-system timing, engine meter, counters without casts | S3 S5 | inline | done | 03f4100; DiagnosticsTests 6/6, FrameScheduleTests +2 |
| T4b `DebugDraw` and physics shape inspection | S2 | inline | done | 49080c4; DebugDrawTests 4/4, conformance on both backends, Box2D 103/103 |
| T4c Input trace, engine messages through `ILog`, crash report | S2 S4 | inline | done | d409076; CrashReportTests 3/3, input trace test |
| T4d Example, guide page, release notes | S6 | inline | done | Diagnostics example + test, `docs/manual/diagnostics.md`, API pages regenerated, site build 513 pages |

## Log

- L1 (2026-11-12) Owner: "Sigue trabajando en el siguiente issue segun la prioridad". Top open board item after
  PHYS-1 (#149 merged): DEBUG-1 #119 (P0, M).
- L2 (2026-11-12) Map done. Answered today: assets, platform facts, coarse `--profile`. Failing: physics shapes and
  contacts cannot be read back or drawn, input consumption is not recorded, crashes print only the message, engine
  messages bypass `ILog`, no live numbers or .NET metrics, counters hidden behind casts. Proposal: 9 primitives in
  four units (evaluation §4). Next: owner approval (T3).
- L3 (2026-11-12) Owner: "Approve all nine". T4a starts.
- L4 (2026-11-12) Units A–C committed. Deviations from the evaluation: no `--diagnostics` launcher flag (the game sets
  `Diagnostics.Enabled`, and a meter listener turns measuring on); actions fired are already readable (`Input.Actions`,
  `Down`, `IsConsumed`), so the input trace records only layer routing; launcher and host notes stay on the host's
  error writer (they come before any game exists), backend messages (SDL, raylib) go through `PlatformRequest.Log`,
  replayed into the game's `ILog` at start; the crash report holds no recent log lines, because collecting them would
  mean wrapping the game's `ILog` service (what `Services.Get<ILog>()` returns would change).
  Correction to L2: a failed run already printed the exception with its stack on standard error (`error: {error}`); only
  host-creation errors print the message alone. The gap is the missing file a tester can send (evaluation §2.6 fixed).
- L5 (2026-11-12) Full suite: every project passes except `MicroEngine.Desktop.Tests` clipboard check, which also fails
  intermittently on `main` here (2 of 3 runs): the machine's clipboard, not this change. Snippets, `api-reference --check`,
  site tests 71/71 and `SITE_DEMOS=none npm run build` (check-dist) pass. Headless capture of the example: overlay,
  tile outlines (2,565 px in the static colour), one-way tops and the player's box drawn. Draw calls read 0 headless
  (its device does not count), as documented. Not verified: SDL/raylib desktop window, browser and mobile runs of the
  overlay; `dotnet-counters` against a live process (the meter is checked with an in-process `MeterListener`).
