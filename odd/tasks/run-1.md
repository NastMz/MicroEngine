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
- L7 (2026-11-22) Coordinator's corrections, verbatim: "No failing run was observed for any new test. Observe one
  now for each group"; "The name clash is a defect for a developer following the manual [...] Change the sample's
  medium to something a developer would really write"; "Say in the manual's persistence page and in limitations
  what a scripted run does in the browser (it uses the page's storage), since that is the one place the rule does
  not hold."
- L8 (2026-11-22) Red runs. Against the source of `origin/main` the two new test files do not compile (they use
  `MemorySaveStorage`, `SaveDirectory` and the `actions` parameter: CS0246, CS1061, CS1501). So the behaviour was
  taken out with the new API left in (in the working tree only, restored afterwards): `Game.AttachRun` giving no
  store of its own, `InputBindingSet.FromJson` and `ImportBindings` accepting a later format, `ImportBindings`
  replacing an action whose controls are all unknown. `RunSavesTests` and `BindingsPersistenceTests`: 7 failed,
  8 passed of 15. The tests were first reordered so that each fails on the behaviour it is named for:
  - `A_scripted_run_keeps_its_saves_in_a_store_of_its_own`: "Se esperaba que la condición fuera false" (the
    default folder exists: the run wrote there).
  - `Two_scripted_runs_in_a_row_start_from_the_same_state`: "Se esperaba que los valores fueran iguales" (the
    second run found the first one's save).
  - `A_run_in_deterministic_time_keeps_its_saves_in_a_store_of_its_own` and
    `A_game_stepped_by_a_test_has_a_store_of_its_own_or_the_one_the_test_hands_it`: "Expected value to be of type
    MemorySaveStorage".
  - `A_game_that_used_its_saves_before_such_a_run_is_told`: "Expected exception of exact type
    InvalidOperationException but no exception was thrown".
  - `A_file_of_a_later_format_is_not_applied_and_is_set_aside`: "the action keeps its controls. Número diferente
    de elementos" (the map was emptied).
  - `An_action_whose_saved_controls_are_all_unknown_keeps_the_controls_it_has`: "nothing it knows: the action is
    left as it was. Número diferente de elementos".
  The eight that passed test the option, the composition's medium, a played run, the memory storage itself, the
  `actions` parameter and an action saved unbound, none of which the removed lines decide.
- L9 (2026-11-22) The examples' storage of their own is `SingleFileSaveStorage` (every slot in one file, written
  whole beside it and moved over it), in place of a hand-written memory storage that now had the SDK's name.
  `ExtendingTests.AGameSavesThroughACustomStorage` covers it; the samples' tests use the SDK's `MemorySaveStorage`
  without qualifying. The persistence page and the limitations page state that a scripted run in a browser page
  uses the page's storage.
- L10 (2026-11-23) Correction to L6. "kept their dates of 2026-11-20" was true when it was observed (2026-11-22
  19:36, right after the two tour runs of this branch) and stopped being true eleven minutes later: the three files
  of `%LOCALAPPDATA%\MicroEngine\MicroEngineExamples` are dated 2026-11-22 19:47:19 to 19:47:24. What wrote them was
  not this branch. A set of tour captures in the temporary folder `tmp.hvjg7UH72v` was written in the same seconds
  (19:47:18 to 19:47:25); it has 47 captures, ending in `41-music-time.png` and `42-spatial-sound.png`, examples
  that exist only in the audio writer's worktree (`MicroEngine-worktrees/audio-3`), whose branch does not contain
  a3b1e9d0. A tour run from a tree without the rule reads and rewrites the real folder, and writes the whole map
  (1042 bytes), which is what is there. This branch's tour runs wrote 45 captures to `tmp.DJbiyp0CLs` at 19:36 and
  its bindings file has two actions. Not established: the process itself (inferred from the files' times and
  names, nobody was asked).
  The red-run experiments of L8 did not write that folder: they ran `MicroEngine.Tests` only, with save ids of
  their own, and removed their folders.
  One write to the player's data folder is this branch's and is stated plainly: on 2026-11-23 15:12 the red run of
  `The_crash_report_of_a_run_nobody_plays_is_beside_its_output_not_in_the_players_folder` (the behaviour not yet
  written) created `%LOCALAPPDATA%\MicroEngine\com.example.identitytest\` with one `crash-*.txt`: the crash
  folder is named by the application id of the test project, not by the save id the test made unique. It was left
  in place (nothing in that folder is deleted from here). The test now counts the reports of that folder before
  and after, so it fails if one is added.
- L11 (2026-11-23) Coordinator's correction batch, the points verbatim in short: "`--headless --time realtime
  --frames 3`, and `RunOptions.TimeMode = RealTime` on `HeadlessPlatform`, use the platform default";
  "`host.SaveStore(gameId)` [...] returns the platform default in a `--script` launch and ignores `--saves`";
  "a scripted run that crashes writes its report under the real `%LOCALAPPDATA%\MicroEngine\<id>` folder";
  "`--hidden --frames N --report` without a script is a real-time run on the real folder"; "`--saves \"\"`, `\"   \"`
  or a path with a NUL character: unhandled `ArgumentException`"; "A `--saves` path that is an existing FILE
  currently exits 0"; "State in the persistence page the full list of what decides \"nobody plays\", in one table".
  Decisions: a headless platform is unattended in any time mode (`RunPreset.Unattended`, always stated by the
  headless platform's preset); `--frames N` (`RunOptions.MaxUpdates`) makes a run unattended and `--report` does
  not (it only prints two lines of a run somebody may be playing); `LaunchRequest.IsUnattended` is the launch's
  rule and `HostBuilderExtensions.RunStorage` the one place a launch's medium is decided, for `Game.Saves` and
  `host.SaveStore` alike; the crash report of an unattended run goes to the captures' folder with a script, else the
  working directory (not the `--saves` folder: it may be a fixture, and a report is an output); `--saves` is
  checked in `LaunchRequest.Parse`. `sdk/tests/external.mjs` passes `--saves` to the two packaged runs.
  Red run, with `IsUnattended` present and false and nothing else written (6 failed of 23 in `RunSavesTests` and
  `GameSavesTests`):
  - `A_headless_run_keeps_its_saves_to_itself_whatever_its_time_mode`: "Expected value to be of type
    MemorySaveStorage".
  - `A_run_that_ends_by_itself_keeps_its_saves_to_itself`: "Se esperaba que la condición fuera false" (the default
    folder existed).
  - `A_store_asked_of_the_composition_follows_the_launch`: "Se esperaba que los valores variaran" (its folder was
    the player's).
  - `The_crash_report_of_a_run_nobody_plays_is_beside_its_output_not_in_the_players_folder`: an exception of the
    test method, not an assertion (there was no captures folder to list: the report had gone to the player's).
  - `A_saves_option_that_names_no_folder_is_refused_when_the_launch_is_read`: "Expected exception of exact type
    FormatException but no exception was thrown".
  - `The_launch_says_whether_somebody_plays`: "Se esperaba que la condición fuera true".
  After: `dotnet build MicroEngine.slnx -c Release` 0 warnings, 0 errors. `dotnet test MicroEngine.slnx -c Release
  --no-build`: every assembly passed except one test of `MicroEngine.Tests`,
  `A_strength_or_brightness_without_limit_is_clamped_and_the_effect_ends` (10 s, a post-processing test this batch
  does not touch), while other programs were running on the machine; alone it passes (49 ms), and `MicroEngine.Tests`
  run again whole: 1351 passed, 1 skipped, 0 failed. Samples.Tests 109 passed; the demos 67, 75, 73 and 132.
  From the command line (`Examples.exe`): `--saves ""`, `--saves "   "` and `--saves <a file>` print the reason and
  the usage text and exit with 2; `--headless --time realtime --frames 3` and `--platform sdl-software --hidden
  --frames 30 --report` end normally and the files of the two Examples folders under `%LOCALAPPDATA%\MicroEngine`
  keep their dates. Not run: `sdk/tests/external.mjs` (it packs and installs the SDK), the browser suite, the phone
  hosts, the site build.
