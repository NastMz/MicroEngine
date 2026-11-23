# 0073 — A run nobody plays keeps its saves to itself

Status: Accepted (2026-11-20). The SDK is in Alpha: public API changes carry no compatibility commitment. Amends
[0063](0063-versioned-saves-and-the-games-store.md) (where `Game.Saves` keeps its slots) and
[0038](0038-one-composition-path.md) (what a launch decides for every host). Evidence: [RUN-1](../evaluations/run-1.md).
Task record: [`odd/tasks/run-1.md`](../../odd/tasks/run-1.md). Issue NastMz/MicroEngine#205.

## Context

A scripted run is meant to be the same every time: the launch already gives it deterministic time, seed 1, no sound
device and no machine language. Its saves were the exception. `Game.Saves` used the composition's medium, else the
player's folder, whatever the run was, so a capture run read and wrote the developer's real saves and depended on
what earlier runs had left there. The way out was code in every game (a `saveDirectory` parameter and an override
of `CreateSaves`), which tests used and the command line did not. A stale `input-bindings.json` in the real folder
changed the captures of the examples' tour on the reference machine.

## Decision

- **Where `Game.Saves` keeps its slots is decided when the run starts**, in this order: the medium the run names
  (`RunOptions.Saves`); the medium of the composition (`host.Storage`); a store of the run's own when nobody plays
  it; the platform's default.
- **Nobody plays a run that has a script, advances in deterministic time, has no window or ends by itself.**
  Deterministic time is the run's choice (`--time deterministic`) or the platform's preset; no window is the
  headless platform in any time mode (`RunPreset.Unattended`, which its preset always states, and `--headless` or
  `--platform headless` on a launch); an end of its own is `--frames N` (`RunOptions.MaxUpdates`): a run that stops
  after N updates is a check. `--hidden`, `--mute` and `--report` decide nothing, because a person may be playing
  such a run. `LaunchRequest.IsUnattended` is the rule for a launch; `Game.Initialize` applies the same to a run
  without a host. This is what a run knows before the game configures itself.
- **`host.SaveStore(gameId)` follows the launch** exactly as `Game.Saves` does, and in one launch both are the
  same saves (one store in memory per save id, kept by the composition).
- **The crash report of a run nobody plays is written beside its output**: the captures' folder with a script,
  else the working directory. The saves folder of `--saves` was not chosen: it may be a fixture that is read, and a
  report is an output. A played run keeps writing it in the game's folder under the user's data, where a player
  can be asked to look.
- **`--saves` is checked when the launch is read**: a blank value, something that is not a path and an existing
  file are argument errors (usage text, exit code 2).
- **The run's own store is in memory** (`MemorySaveStorage`, public, in `MicroEngine.Saves`): empty at start, gone
  at the end, no folder to clean or find, the same on every host. It takes the slot names the file storage takes
  and keeps no time of writing.
- **`--saves DIR`** (`LaunchRequest.SaveDirectory`) names a folder for the run's saves, on every desktop launcher,
  for a scripted run (start from given saves, look at what it wrote) and for a played one (a second set of saves).
  It is the most explicit choice and wins over the composition's medium.
- **`InitializeForTest(platform, seed, saves)`** takes the medium for a test that starts a game twice over the
  same saves.
- **A composition's medium wins over the run's own store.** A game or a test that sets `host.Storage` said where
  its saves go; only it knows whether that medium is safe for a scripted run, and its `Configure` can read
  `host.Request.HasScript`.
- **A store built by hand in `CreateSaves` is the game's own**: the run does not replace it. A game whose store
  already exists on the platform's default when such a run starts (it used `Saves` in its constructor) is told
  with an exception, as a composed medium already did.
- **In a browser page a scripted run keeps the page's storage.** The browser host composes it for a launch with a
  script. Such runs exist only in development packages, start from the browser profile they are opened in, and
  are how the browser checks see a full or blocked storage.
- **Saved bindings cannot empty a map.** `LoadBindings` does not apply a file of a later format (it is set aside);
  an action whose saved controls are all unknown keeps its own; and saving and loading take the actions a
  rebinding screen offers (`actions:`), so the other actions always follow the game's code.

## Consequences

- No game writes code to keep its tests and captures away from the player's saves. The examples, Gem Garden, the
  tutorial and the persistence page lose the `saveDirectory` parameter; the four demos keep theirs until they are
  modernized and work as before (their override falls back to the base, which follows the rule).
- A scripted run no longer starts from the developer's progress; `--saves DIR` does that on purpose.
- `Game.CreateSaves` is no longer the documented way to choose a folder: it is for a game that must keep its saves
  in one place whatever the run says.
- The demos' `*_SAVE_DIR` variables and the package checks that set them can become `--saves`.

## Not done

- The browser page has no option to ask for a store of the run's own, and its repeated-run check shares one
  profile between its two runs. Moving the page to the rule needs a page option for the storage checks and a run
  of the browser suite.
- A temporary folder per scripted run was rejected: it leaves folders behind or deletes what was to be looked at.
