# RUN-1: a scripted run keeps its own saves

Objective: a run nobody plays (a script, deterministic time, a game stepped by a test) never reads or writes the
player's saves, with no code in the game; a folder can be named for any run. Issue NastMz/MicroEngine#205, part of
CONSOLIDATION-1 (#183). Branch `feat/run-1`. Delivery: one pull request, no push or release without the owner. Test
runner: `dotnet test MicroEngine.slnx -c Release`. Evaluation:
[`docs/evaluations/run-1.md`](../../docs/evaluations/run-1.md). Decision:
[ADR 0073](../../docs/decisions/0073-saves-of-a-run-nobody-plays.md).

## Specs

Quoted fragments are the issue's text (L1).

- S1 Own store: "A scripted or deterministic run gets a store of its own by default (empty, in memory or in a
  temporary folder), with a way to name a folder (`--saves <dir>`) for a run that wants to start from given saves
  and to look at what it wrote." Acceptance: such a run does not read or write the default location; two in a row
  start from the same state; `--saves <dir>` reads and writes there; no game writes code for it.
- S2 Interactive runs: "Whether the same switch serves an interactive run (a second profile, a clean start while
  developing)." Acceptance: decided, documented, and an interactive run without the option is unchanged.
- S3 Everything a run reads from the machine: settings, achievements and key bindings follow (they are slots of
  the store: confirmed), and what a scripted run does in the browser host is stated.
- S4 Samples: "What the demos and samples then delete: the `saveDirectory` parameters and `CreateSaves` overrides,
  if the host covers their tests too." The samples and their documentation lose them; the four demos (`games/**`)
  are not changed here and must keep working.
- S5 The stale file: "Where the stale file with empty control lists came from: not established.
  `Input.SaveBindings` writing empty lists in some state would be a defect of its own." Acceptance: reproduced in a
  test and fixed, or stated plainly as not reproduced.
- S6 Determinism: the demos' and the samples' scripted traces and captures still pass; one that changes because it
  depended on an earlier run's saves is reported.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 The rule, `--saves`, `MemorySaveStorage`, `InitializeForTest(saves)` | S1, S2, S3 | inline | [x] | see L3 |
| T2 Samples and their documentation without the parameter | S4 | inline | [x] | see L4 |
| T3 Saved bindings cannot empty a map | S5 | inline | [x] | see L5 |
| T4 API reference, closing checks | S6 | inline | [x] | see L6 |

## Log

- L1 (2026-11-20) The issue, verbatim:

  > ## What happens
  >
  > A scripted run of a game from the command line (`dotnet run -- --script tour.script --hidden --captures out`)
  > reads and writes the player's real save folder. Its captures and its state then depend on what earlier runs left
  > there.
  >
  > Seen on 2026-11-20 while integrating localization (#180): the Examples tour showed "CONFIRM" and "ACTION" in a
  > hint instead of key names. The cause was a stale
  > `%LOCALAPPDATA%\MicroEngine\MicroEngineExamples\input-bindings.json` in which every menu action had an empty
  > control list; the rebinding example loads it with `Input.LoadBindings`, and the keys were gone for the rest of
  > the tour. With a clean folder the same script shows the key names.
  >
  > ## Why it is the engine's
  >
  > - `Game.CreateSaves()` uses the platform's default storage unless the host composed another one. Nothing in a
  >   scripted run composes one.
  > - The documented way to keep a run away from the real saves is for each game to take a `saveDirectory` parameter
  >   and override `CreateSaves` (`docs/manual/persistence.md`, quoted from `samples/Examples/ExamplesGame.cs#saves`;
  >   `games/MeadowRun/MeadowGame.cs` does the same). Tests pass a temporary folder; the command line passes nothing.
  > - So every game repeats the same code, and the one path a developer uses to take captures and reproduce bugs is
  >   the one that is not isolated. A scripted run is meant to be the same every time (the engine already ignores the
  >   machine's language and the wall clock in one).
  >
  > ## To decide (method of #183)
  >
  > - A scripted or deterministic run gets a store of its own by default (empty, in memory or in a temporary
  >   folder), with a way to name a folder (`--saves <dir>`) for a run that wants to start from given saves and to
  >   look at what it wrote.
  > - Whether the same switch serves an interactive run (a second profile, a clean start while developing).
  > - What the demos and samples then delete: the `saveDirectory` parameters and `CreateSaves` overrides, if the host
  >   covers their tests too.
  > - Where the stale file with empty control lists came from: not established. `Input.SaveBindings` writing empty
  >   lists in some state would be a defect of its own.
  >
  > Part of CONSOLIDATION-1 #183 (structural cause: what a developer must assemble by hand).

- L2 (2026-11-20) Decisions (evaluation, ADR 0073). Level: Extend. The rule is in `Game.Initialize`, the one place
  every run starts, so `GameRunner`, `GameLoop`, `GameSession`, `GameHost` and `InitializeForTest` share it. "Nobody
  plays" is a script or deterministic time by the run or the platform's preset. In memory, not a temporary folder.
  Order: the run's medium, the composition's, the run's own store, the platform's default. The composition's medium
  wins over the run's own store because the demos' tests and any game with a medium of its own compose it on purpose
  (`tests/MeadowRun.Tests/SaveFormatTests.cs`, `tests/CourtyardKey.Tests/SaveFormatTests.cs`). The browser page keeps
  its storage in a scripted run: `web/tests/run.mjs` (`storage`, `blockedStorage`) plays a script to see a full and a
  blocked storage, and the browser suite cannot be run here.
- L3 (2026-11-22) T1, commit a3b1e9d0. `Game.AttachRun` decides the medium at `Initialize`; `RunOptions.Saves`,
  `LaunchRequest.SaveDirectory` (`--saves`), `LaunchArguments.SaveDirectory`, `MemorySaveStorage`,
  `InitializeForTest(platform, seed, saves)`; the browser host composes the page's storage for a launch with a
  script. Tests: `tests/MicroEngine.Tests/Hosting/RunSavesTests.cs` (10). They were written after the code and do
  not compile against the code before it; no red run was observed for this task.
- L4 (2026-11-22) T2, commit 75c3d869. `ExamplesGame` and Gem Garden's `MainGame` lose the parameter and the
  override; their tests use the run's own store, and the two relaunch tests share one `MemorySaveStorage`
  (`GemGardenTests`, `LocalizationExampleTests`). The tutorial's chapter on saving, the testing guide, the persistence
  page, `configuration.md` and `contracts.md` follow. The four demos were not touched and their tests pass. What
  they can delete: the `saveDirectory` constructor parameters and `CreateSaves` overrides of `MeadowGame`,
  `RiftGame`, `CourtyardKeyGame` and `TrailDuelGame`, the `*_SAVE_DIR` variables (`RIFT_SAVE_DIR`...), and in their
  tests the temporary folders, replaced by nothing or by `InitializeForTest(platform, saves: storage)`; the package
  checks (`packaging/verify.mjs`, `packaging/linux-distros.mjs`) would pass `--saves` instead of the variable.
- L5 (2026-11-22) T3, commit 4a83c1b4. Evaluation, section J. `InputBindingSet.FromJson` refuses a later format,
  `ImportBindings` keeps an action whose saved controls are all unknown, and the four methods take `actions`.
  `RebindExample` passes its two actions. Tests: `BindingsPersistenceTests` (4), written before the fix but not run
  red. Not reproduced: the first emptying of the file on this machine; it is inferred from the reader before
  71b97e67 and the folder's date.
- L6 (2026-11-22) T4. `dotnet build MicroEngine.slnx -c Release`: 0 warnings, 0 errors.
  `dotnet test MicroEngine.slnx -c Release --no-build`: 17 assemblies, 0 failed (MicroEngine.Tests 1345 passed,
  1 skipped; Samples.Tests 109 passed, 2 skipped; the four demos 67, 75, 73 and 132 passed; Desktop 833 passed,
  17 skipped), no aborted host. The examples' tour run twice from the command line: 45 captures, byte-identical
  between the runs; the files in `%LOCALAPPDATA%\MicroEngine\MicroEngineExamples` kept their dates of 2026-11-20;
  with `--saves DIR` the three slots were written there, the bindings holding `fire` and `jump` only.
  `16-rebind.png` and `37-localization-en.png` were looked at: key names, not action names. Not run: the browser
  suite (`web/tests`), the phone hosts, `sdk/tests`, the site build. No scripted trace or capture test of the demos
  or the samples changed.
