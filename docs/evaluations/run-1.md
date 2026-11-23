# RUN-1: where a run nobody plays keeps the game's saves

Evaluation of 2026-11-20, on `main` at 2da45f20, for NastMz/MicroEngine#205. It follows the method of
[CONSOLIDATION-1](consolidation-1.md), section 1. Decision record:
[ADR 0073](../decisions/0073-saves-of-a-run-nobody-plays.md). Task record:
[`odd/tasks/run-1.md`](../../odd/tasks/run-1.md).

## A. The need

Every game is run in two ways: by a player, and by a script, a test or a check that nobody plays. The second kind
is how a developer takes captures, reproduces a bug and proves a change, and it has to be the same every time. The
engine already makes it so for time (one fixed step per frame), randomness (seed 1), sound (no device) and the
machine's language. It did not for the one thing a game keeps between runs: its saves.

## B. What happened before

- `Game.Saves` was a store on the composition's medium (`host.Storage`), else the platform's default: the player's
  folder. Nothing about a scripted run changed that. `dotnet run -- --script tour.script --hidden --captures out`
  read and wrote `%LOCALAPPDATA%\MicroEngine\<game>`.
- The documented way out was code in every game: a `saveDirectory` constructor parameter and an override of
  `CreateSaves`. The persistence page quoted it from the examples, the tutorial taught it in its chapter on saving,
  the four demos and Gem Garden repeat it. Tests passed a temporary folder; the command line passed nothing, so the
  path used for captures was the one left open. The demos added an environment variable each (`RIFT_SAVE_DIR`...)
  for their package checks.
- A game stepped by a test without that parameter (`new MyGame()`, `InitializeForTest`) wrote the player's folder
  too. Six test files in the repository carry their own in-memory storage because the SDK has none.
- Seen on this machine: the examples' tour showed "CONFIRM" where a key name belongs, because
  `input-bindings.json` in the real folder held every menu action with an empty list of controls (section J).

What else a run reads from the machine that is not the game's content was checked: progress, settings
(`GameSaves`), achievements (`AchievementTracker.SaveSlot`) and key bindings (`Input.SaveBindings(Saves.Storage)`) are
all slots of the game's one store, so one decision covers them. Crash reports are written beside the saves folder
only when a run fails and are never read. The language and the clock were already taken out of a scripted run.

## C. Alternatives

No library applies: this is the engine's own launch model. What other engines do was the reference. Godot and Unity
leave `user://` and `persistentDataPath` shared between the editor's test runs and play, and their test guides tell
each project to redirect them by hand, which is what the SDK did. MonoGame has no notion of a scripted run. The
options were therefore the engine's own:

1. **Keep** the per-game parameter, and add `--saves DIR`. The command line gets a way out, but every game still
   writes the override and a run is isolated only when somebody remembers.
2. **A temporary folder per scripted run.** Isolated and inspectable, but it leaves a folder behind every run (or
   deletes what the developer wanted to look at), the path has to be printed and found, and it does not exist in a
   browser page.
3. **A store in memory per run nobody plays**, and `--saves DIR` for a run that starts from given saves or whose
   saves are wanted afterwards. Nothing to clean, the same on every host, and the explicit case is one option.

## D. Target experience

A game:

```csharp
public sealed class MyGame : Game
{
    protected override void Load() => _progress = Saves.Load("progress", Schema).Value ?? new Progress();
}
```

A capture run, isolated with no option; and one that starts from saves prepared for it:

```
dotnet run -- --script tour.script --hidden --captures out
dotnet run -- --script boss.script --hidden --captures out --saves fixtures/before-boss
```

A second set of saves while developing: `dotnet run -- --saves .saves/clean`.

A test that plays, and one that relaunches over the same saves:

```csharp
var game = new MyGame();
game.InitializeForTest(new HeadlessPlatform());          // its own store, empty

var saves = new MemorySaveStorage();
first.InitializeForTest(new HeadlessPlatform(), saves: saves);
second.InitializeForTest(new HeadlessPlatform(), saves: saves);
```

No constructor parameter, no override, no environment variable.

## E. Responsibility limit

The engine decides where the saves of a run go; the game decides what it saves. A game that builds its store by
hand (`CreateSaves`) keeps it whatever the run says: that is the game's explicit choice. A composition that names a
medium (`host.Storage`, a cloud service) decides for its scripted runs too, because only it knows what that medium
is; the documentation shows the one line that leaves such runs alone.

## F. Integration

- The rule lives where every run starts, `Game.Initialize`, so `GameRunner`, `GameLoop`, `GameSession`, `GameHost`
  and `InitializeForTest` share it, on desktop, phones and tests. "Nobody plays it" is what the run already knows
  before the game configures itself: it has a script, or its time mode is deterministic by the run's choice or the
  platform's preset (the headless platform's).
- `RunOptions.Saves` is the run's medium; `--saves DIR` (`LaunchRequest.SaveDirectory`) fills it with a
  `FileSaveStorage`, for scripted and played runs alike.
- Saves are not part of the state hash and need not be: a scripted run now starts from an empty store, so what it
  loads is a function of the script.
- Browser: a scripted page run exists only in development packages and is the way the browser checks exercise the
  page's storage when it is full or blocked; what such a run starts from is the browser profile. The browser host
  composes the page's storage for it, so nothing changes there. It could move to the same rule with an option of
  the page to ask for its storage; that needs the browser suite, which was not run here.
- NativeAOT and trimming: `MemorySaveStorage` is a dictionary of strings; no reflection.

## G. Trade-offs

- A scripted run no longer shows the developer's own progress. That is the point; `--saves DIR` is the way to
  start from something.
- "Deterministic time means its own saves" also covers a person playing with `--time deterministic` to step a bug.
  Such a session is a reproduction, not play; `--saves DIR` keeps its saves.
- Slots in memory have no time of writing, so a "load game" list in a scripted run shows none. A clock time in a
  capture would differ every run.
- A game that used `Saves` in its constructor without a store of its own is now told, in a test or a scripted run:
  its store would have been the player's.

## H. Decision

**Extend.** The launch model gains one rule and one option; the store, its media and the composition keep their
shape. The per-game parameter goes from the samples and the documentation.

## I. Validation

Tests of the rule through the public interface (`tests/MicroEngine.Tests/Hosting/RunSavesTests.cs`): a scripted
run, a deterministic run and a stepped game keep a store of their own and the default folder is never created; two
scripted runs start from the same state; `--saves DIR` is read and written, scripted or played; a run somebody
plays is on the platform's default; the composition's medium and a hand-built store are respected. The samples'
tests run without a folder of their own, and the four demos' tests pass unchanged.

## J. The file with empty control lists

`%LOCALAPPDATA%\MicroEngine\MicroEngineExamples\input-bindings.json` on this machine (read, not changed) is a
version 2 file in which `jump` and `fire` hold the rebinding example's defaults and every menu action
(`confirm`, `up`, `pause`, `fullscreen`...) has `"controls": []`.

What was established by running the code:

- The examples' tour saves the bindings on every run: its second `confirm` in the rebinding example presses
  "RESET TO DEFAULTS". A run on a temporary folder writes a healthy file, so the tour alone does not produce the
  stale one.
- `SaveBindings` writes every action of the map, and `LoadBindings` replaces every action the file names. So one
  load that empties the menu actions is written back by the next tour and never heals. This is why `jump` and
  `fire` look right in the stale file (the reset rebinds them) and nothing else does.
- A reader applies a file of a format it does not know as "every action, no control". The current reader did not
  look at the version number, and a non-empty list in which it recognised nothing left the action with no control.
  Both are reproduced in tests (`BindingsPersistenceTests`).

What is inferred, not observed: the first emptying. The reader before commit 71b97e67 (2026-11-19 17:25, the
format's version 2) has the same flaw visibly in its source: it reads `keys` and `buttons`, ignores `version`, and
so reads a version 2 file as actions without controls. The save folder was created on 2026-11-19 at 17:34. A build
of a branch without that commit, run after a build with it, would produce exactly this file. No such build was run
here to prove it.

No sequence of presses in the rebinding example (start a rebind, cancel, leave, quit while listening) writes an
empty list for an action that had controls: `Listen` saves only after a press was assigned, and `Exit` unbinds the
two example actions without saving. An action unbound on purpose (`Unbind("diagnostics")`) is saved with an empty
list, which is correct.

Fixed: a file of a later format is not applied (it is set aside, like unreadable data); an action whose saved
controls are all unknown to this version keeps the controls it has; and `SaveBindings`, `LoadBindings`,
`ExportBindings` and `ImportBindings` take the actions a rebinding screen offers, so the engine's and the menus'
actions always follow the game's code. An empty saved list still unbinds the action: that is what was saved.
