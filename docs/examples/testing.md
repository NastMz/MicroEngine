# Testing games

MicroEngine games are tested headlessly: `HeadlessPlatform` replaces the window, audio and input, the fixed-step
loop advances exactly one update per call, and the same game code runs as on the desktop. On top of unit tests
for rules, every game built with the engine has playthrough tests that drive the real game through its input
actions, and input scripts that replay play sessions for captures. This page shows the three levels with real
code from `tests/` and `games/*/scripts`.

## The test projects

The engine and each game have their own test project, written with MSTest; they are ordinary .NET test projects
run by the standard .NET test tooling. At the WEB-1 evaluation they held 279 tests (engine 93, Meadow Run 44, Courtyard
Key 27, Trail Duel 85, Neon Rift 30; [web support](../evaluations/web-support.md#desktop)).

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

## Native desktop checks

Scripts force actions on `InputMap` and headless tests never open a window, so neither proves that the desktop
backend works on a given system. The native checks do, and on Linux they run under a virtual display (Xvfb) and a
virtual sound server (PulseAudio):

| Check | What it shows | Where it runs |
| --- | --- | --- |
| Scripted scenarios on the desktop build | raylib and OpenGL draw the 15 browser-test scenarios (138 captures), compared with a per-system visual reference | Linux automatically; macOS attempted, completeness only |
| Keyboard | Key events from the X server reach the game through raylib: a held key gives one press, stays down and gives one release; a tap shorter than a frame is still seen; two keys are independent | Linux |
| Audio device | The raylib audio device opens on the sound server instead of falling back to silence | Linux; macOS attempted |
| Audio output | A 440 Hz tone played through the engine is recorded from the virtual sink and recognised by level and frequency | Linux |

The visual reference keeps, for each capture, its size, a hash of its exact pixels and the mean colour of each cell of
a 32 × 18 grid. A run passes when every capture is there with its size and no cell moved by more than 3 out of 255:
another driver may round blending differently without failing, while a missing sprite, a wrong frame or a changed
HUD digit fails. Exact pixel equality is reported. Windows and Linux are not expected to produce identical pixels,
so each system gets its own reference. Gamepads are covered by none of these checks.

## Browser tests

The same scripts are replayed in Chrome and Firefox on desktop against development packages, and their captures
are compared byte for byte with the desktop build. Each scenario names a game, a script and its environment; from
the browser test scenarios:

```json
{ "game": "MeadowRun", "script": "smoke.script", "keyboard": true, "env": {} },
{ "game": "CourtyardKey", "script": "boss.script", "env": { "COURTYARD_START": "boss" } },
```

Browser packages and these browser tests are internal tooling; producing packages is not available to external
developers yet.

## Guidelines

- Test observable behaviour through public types and actions, not private state.
- Use a temporary save directory in every test that builds a game.
- For visual or feel changes, run a script with captures and look at the PNGs; a passing test is not proof that
  something looks right.
