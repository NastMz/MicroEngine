# Testing your game

MicroEngine games are tested headlessly: `HeadlessPlatform` replaces the window, audio and input, the fixed-step
loop advances exactly one update per call, and the same game code runs as on the desktop. On top of unit tests
for rules, every game built with the engine has playthrough tests that drive the real game through its input
actions, and input scripts that replay play sessions for captures. This page shows the three levels with real
code from `tests/` and `games/*/scripts`.

## A test project for your game

A game's tests are an ordinary .NET test project that references the game. With MSTest:

```bash
dotnet new mstest -n GemGarden.Tests
dotnet add GemGarden.Tests reference GemGarden/GemGarden.csproj
dotnet test GemGarden.Tests
```

A test builds the game with a temporary save folder, initialises it on `HeadlessPlatform` (no window, no sound, one
update per call), presses actions with `InputMap.SetForced` and checks what the game exposes. These tests play the
tutorial's game:

<!-- code: tests/Samples.Tests/GemGardenTests.cs -->
```csharp title="GemGardenTests.cs"
using GemGarden;
using GemGarden.Scenes;
using MicroEngine.Core;

namespace GemGarden.Tests;

/// <summary>Gem Garden played headlessly: the game is built, initialised on HeadlessPlatform and stepped one update at a time.</summary>
[TestClass]
public sealed class GemGardenTests
{
    private static (MainGame Game, string Saves) Start()
    {
        var saves = Directory.CreateTempSubdirectory("gemgarden-").FullName;
        var game = new MainGame(saves);
        game.InitializeForTest(new HeadlessPlatform());
        return (game, saves);
    }

    private static void Hold(Game game, string action, int steps)
    {
        game.Input.SetForced(action, true);
        Step(game, steps);
        game.Input.SetForced(action, false);
        Step(game, 1);
    }

    private static void Step(Game game, int steps)
    {
        for (int i = 0; i < steps; i++) game.StepForTest();
    }

    /// <summary>From the title, PLAY (the first option) starts a round once the fade is over.</summary>
    private static PlayScene Play(MainGame game)
    {
        Assert.IsInstanceOfType<TitleScene>(game.Scenes.Current);
        Hold(game, "confirm", 2);
        Step(game, 60);
        return (PlayScene)game.Scenes.Current!;
    }

    [TestMethod]
    public void WalkingRightPicksUpTheFirstGem()
    {
        var (game, saves) = Start();
        var play = Play(game);
        Assert.AreEqual(0, play.Gems);
        Assert.AreEqual(24, play.TotalGems);
        Hold(game, "right", 70);
        Assert.AreEqual(1, play.Gems, "the gem five tiles to the right of the start");
        Directory.Delete(saves, true);
    }

    [TestMethod]
    public void PauseFreezesTheRoundAndResumes()
    {
        var (game, saves) = Start();
        var play = Play(game);
        Hold(game, "pause", 2);
        Assert.IsInstanceOfType<PauseScene>(game.Scenes.Current);
        float frozen = play.TimeLeft;
        Step(game, 120);
        Assert.AreEqual(frozen, play.TimeLeft, "no time passes under the pause menu");
        Hold(game, "confirm", 2);   // RESUME
        Assert.AreSame(play, game.Scenes.Current);
        Step(game, 60);
        Assert.IsLessThan(frozen, play.TimeLeft);
        Directory.Delete(saves, true);
    }

    [TestMethod]
    public void TheRoundEndsWhenTimeRunsOutAndTheBestScoreIsSaved()
    {
        var (game, saves) = Start();
        Play(game);
        Hold(game, "right", 70);
        Step(game, (int)(PlayScene.RoundSeconds * 60) + 60);
        Assert.IsInstanceOfType<ResultsScene>(game.Scenes.Current);
        Assert.AreEqual(1, game.BestScore);
        Assert.IsTrue(File.Exists(Path.Combine(saves, "progress.json")), "SaveStore writes <slot>.json in the save folder");

        var relaunched = new MainGame(saves);
        relaunched.InitializeForTest(new HeadlessPlatform());
        Assert.AreEqual(1, relaunched.BestScore, "the next session loads the best score");
        Assert.IsFalse(relaunched.RecordScore(1), "an equal score is not a new best");
        Assert.IsTrue(relaunched.RecordScore(2));
        Directory.Delete(saves, true);
    }

    [TestMethod]
    public void QuitFromTheTitleEndsTheGame()
    {
        var (game, saves) = Start();
        Hold(game, "down", 2);
        Hold(game, "confirm", 2);
        Assert.IsTrue(game.QuitRequested);
        Directory.Delete(saves, true);
    }
}
```

Three things make such tests reliable:

- **Fixed steps.** `StepForTest()` advances exactly one update of `1 / UpdateRate` seconds, so "70 steps holding right"
  always moves the hero the same distance.
- **Actions, not keys.** `SetForced("right", true)` presses the action the game reads, whatever device would press
  it.
- **Isolated saves.** The game takes a save directory (`new MainGame(saves)`), so tests never touch the player's
  saves and never depend on each other.

The rest of this page goes deeper: the engine's own contract tests, step-by-step tests of a full game, and input
scripts that replay play sessions and capture screenshots.

## Level 1: engine contracts

Engine tests exercise public types directly. `HeadlessInput` is an `IInputSource` whose keys a test sets. From
[`tests/MicroEngine.Tests/InputAndRunnerTests.cs`](../../tests/MicroEngine.Tests/InputAndRunnerTests.cs):

```csharp
[TestMethod]
public void A_tap_shorter_than_an_update_is_not_lost()
{
    var (map, keys) = Create();
    keys.Set(Key.Z, true);
    map.Sample(keys);
    keys.Set(Key.Z, false);
    map.Sample(keys); // released before the next fixed update ran
    Assert.IsTrue(map.Pressed("jump"));
    map.EndUpdate();
    map.Sample(keys);
    Assert.IsFalse(map.Pressed("jump"));
    Assert.IsFalse(map.Down("jump"));
}
```

The runner itself is tested with a script and a headless platform, which runs one update per frame:

```csharp
[TestMethod]
public void Headless_run_is_one_update_per_frame_and_honours_script_quit()
{
    var game = new CountingGame();
    var script = InputScript.Parse("10 press confirm\n20 press confirm\n30 capture shot.png\n40 quit");
    var platform = new HeadlessPlatform();
    var result = GameRunner.Run(game, platform, new RunOptions { Script = script });
    Assert.IsNull(result.Error);
    Assert.AreEqual(40, result.Updates);
    // ...
    Assert.AreEqual(1, platform.Screenshots.Count);
}
```

## Level 2: a game, step by step

`Game.InitializeForTest(platform)` runs `Load` against a headless platform without starting a loop, and
`StepForTest()` samples input and advances one fixed update. Games pass a temporary save directory so tests never
touch real saves. Helpers force actions through the game's `InputMap`. From
[`tests/MeadowRun.Tests/PlaythroughTests.cs`](../../tests/MeadowRun.Tests/PlaythroughTests.cs):

```csharp
private static void Press(MeadowGame game, string action, int frames = 2)
{
    game.Input.SetForced(action, true);
    for (int i = 0; i < frames; i++) game.StepForTest();
    game.Input.SetForced(action, false);
    game.StepForTest();
}

[TestMethod]
public void NewGameThroughFirstLevelSavesProgress()
{
    string dir = TempDir();
    var game = new MeadowGame(dir);
    var platform = new HeadlessPlatform();
    game.InitializeForTest(platform);
    Assert.IsInstanceOfType<TitleScene>(game.Scenes.Current);

    for (int i = 0; i < 40; i++) game.StepForTest();
    Press(game, "confirm");
    StepUntil(game, () => game.Scenes.Current is MapScene && !game.Scenes.InTransition, 200, "map");
    // ...
}
```

Persistence is tested by relaunching against the same folder:

```csharp
for (int i = 0; i < 70; i++) game.StepForTest();
var relaunched = new MeadowGame(dir);
relaunched.InitializeForTest(new HeadlessPlatform());
Assert.AreEqual(7, relaunched.Achievements.Progress(MeadowAchievements.CoinPurse), "progress is flushed while playing");
```

Courtyard Key presses physical keys on the headless platform instead, so its bots go through the real bindings.
From [`tests/CourtyardKey.Tests/Harness.cs`](../../tests/CourtyardKey.Tests/Harness.cs):

```csharp
public Harness(string? start = null)
{
    Game = new CourtyardKeyGame(SaveDir, start);
    Platform = HeadlessPlatform.For(Game.Settings);
    Game.InitializeForTest(Platform);
}
// ...
public void Press(Key key, int hold = 2)
{
    Platform.Keys.Set(key, true);
    Step(hold);
    Platform.Keys.Set(key, false);
    Step(2);
}
```

Meadow Run and Neon Rift also ship autopilots that play whole levels or stages under full rules in their tests.

## Level 3: input scripts and captures

An input script is plain text keyed by update number. Commands: `press ACTION` (down for 2 updates),
`hold ACTION N`, `down ACTION`, `up ACTION`, `capture FILE.png`, `quit`; `@+N` means N updates after the
previous command and `#` starts a comment. The format is documented on `InputScript` in
[`src/MicroEngine/Core/GameRunner.cs`](../../src/MicroEngine/Core/GameRunner.cs).

From [`games/MeadowRun/scripts/smoke.script`](../../games/MeadowRun/scripts/smoke.script):

```text
# Title -> map -> level 1, walking and jumping.
90 capture title.png
100 press confirm
200 capture map.png
210 press confirm
260 capture card.png
420 capture level_start.png
430 hold right 200
470 press jump
520 down jump
560 up jump
600 capture level_run.png
# ...
760 quit
```

The game is started with `--script smoke.script --hidden --mute --captures captures`: the desktop launcher plays
the script in a hidden window without sound and writes each `capture` to `captures/`, where a person looks at them.

With a script the launcher uses a fixed step and seed 1, so a script produces the same frames on every run.
Developer environment variables start later in a game. Neon Rift, for example, is started with `RIFT_START=stage`,
`RIFT_STAGE=2`, `RIFT_AUTO=1` and `RIFT_GOD=1` set and its `autotour.script` playing, so its autopilot tours the
second stage while the script takes captures.

The variables of each game are listed in [Configuration](../reference/configuration.md#environment-variables).

## Guidelines

- Test observable behaviour through public types and actions, not private state.
- Use a temporary save directory in every test that builds a game.
- For visual or feel changes, run a script with captures and look at the PNGs; a passing test is not proof that
  something looks right.
