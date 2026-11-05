# ADR 0038: One composition path for desktop, browser, phones and tests

## Status

Accepted (2026-11-03). Amends [ADR 0016](0016-platform-profiles.md) (profiles and request changes are now composed once
by the game and reach every host), [ADR 0021](0021-sdk-toolchain.md) (`MicroEngineGameFactory` is replaced by a
`Configure` entry point the tool finds in the built game) and [ADR 0007](0007-browser-host.md) (the browser host is a
thin caller of the shared launcher model). Builds on [ADR 0026](0026-kernel-modules-and-schedule.md),
[ADR 0027](0027-loop-owned-time.md) and [ADR 0037](0037-platform-host-contract.md). Evidence: ARCH-1 §6.3 (the
`MicroEngine.Hosting` row), §9 (stage 1), annex [A1](../evaluations/arch-1/A1-core.md) F6 (three composition roots,
statics, `MicroEngineGameFactory`), and the browser determinism report
([`experiments/arch-1/browser-determinism`](../../experiments/arch-1/browser-determinism/README.md), root cause R1).

Amended 2026-11-05 (ARCH-1 stage 5, below): phone hosts pass a test's `--env` pairs to the game as
`HostEnvironment.Variables` and no longer write them into the process environment. The browser host is SDL 3
([ADR 0047](0047-sdl-browser-platform.md)), not raylib.

## Context

A game was composed three different ways. The desktop launcher ran the developer's `Program.cs` (catalog, request
changes, `MICROENGINE_PLATFORM`, the macOS bundle written into the static `ContentFolder.DefaultRoot`). The browser
launcher set the statics `SaveStore.DefaultStorage` and `BrowserTouch.Vibrator`, built `new RaylibPlatform(settings, new
DesktopOptions())` directly, and received page events through more statics (`BrowserTouch`, `BrowserLifecycle`). The
phone runner wrote `ContentFolder.DefaultRoot` and `XDG_DATA_HOME` into the process and repeated the launcher's rules by
hand. Web and phone builds created the game from an MSBuild property holding a C# expression
(`MicroEngineGameFactory`), so whatever `Program.cs` composed (a platform of its own, a replaced audio device) never
reached them, and two games in one process (an editor, parallel tests) shared the statics. The duplicated rules had
already drifted: browser scripted runs were real-time (R1), so their captures depended on the wall clock and differed
from run to run (123 of 138 identical) and from desktop (113 of 138).

## Decision

- **A `MicroEngine.Hosting` assembly** that references only the kernel and the platform contracts (the architecture test
  checks it) and ships inside the `MicroEngine` package like them. It holds the launcher model every host shares:
  - `LaunchRequest`: what one launch asks for, from any host (a desktop command line through `LaunchRequest.Parse`, a
    page's script, a phone's launch extras, a test). Its rules are written once: `EffectiveTime` (a script means
    deterministic time unless `--time` says otherwise) and `ResolveSeed` (a script means seed 1). Options naming a
    module's values (GPU, window mode, audio output) are kept as their command-line words; the engine maps them.
  - `ProfileSelection`: how a host chooses a platform from its catalog (a named platform, `--headless`, the developer
    variable `MICROENGINE_PLATFORM`, the game's preferred backend, fallbacks), over `IHostProfile`.
  - `HostEnvironment`: the host's kind (`HostKind`: desktop, browser, Android, iOS, test), its developer variables, its
    output and base directory, handed to the composition instead of being read from the process.
  - `HostBuilder`: the composition of one host, with no process-wide state: `Content` (an `IContentSource`; null means
    the `Content` folder next to the program), `Storage` (the save medium for a game id; null means the platform's
    default), `ConfigureEngine` (changes applied to every engine the host composes) and `Services` (the parts the
    engine's modules keep).
- **The game's `Configure` entry point.** A game declares one `public static void Configure(HostBuilder host)`; it
  chooses the game (`host.UseGame(() => new MainGame())`) and may add platforms (`host.Platforms().Add(...)`), change
  the platform request (`ConfigureRequest`), compose engine services, content and storage. The desktop `Program.cs`
  hands it to the launcher (`DesktopLauncher.Run(MainGame.Configure, args)`); the browser, Android and iOS hosts that
  `microengine package` generates call the same method. The tool finds it by reading the built game's metadata (one
  public static `Configure(HostBuilder)` on a public type; the assembly is never loaded), so no C# expression lives in
  the project file. `MicroEngineGameFactory` is deprecated: with no entry point, the tool wraps it in `UseGame` and warns;
  it has no default any more.
- **`GameHost`** (the engine's side, in the `MicroEngine` assembly): composes a host (its defaults first, then the
  game's `Configure`), creates the game, decides the run (`CreateRunOptions`, `CreateRequest`: deterministic time asks
  for no display wait and no sound device unless the launch asks for one; a real-time run with a start screen starts
  gamepads after the first frame), chooses and starts the platform with fallbacks, and applies the composition to the
  engine. `GameLauncher` (desktop), `BrowserLauncher` (page sessions, `GameSession(Func<LaunchRequest, GameHost>)`),
  the phone runner and tests are thin callers. The old entry points (`DesktopLauncher.Run(game, args[, catalog,
  configure])`, `GameLauncher.Run(game, args, catalog, configure)`, `GameSession(createGame, createPlatform)`) remain and
  go through the same path.
- **No statics.** `ContentFolder.DefaultRoot` is a read-only rule (the `Content` folder next to the program); hosts
  compose their folder (`HostBuilder.Content`: the macOS bundle's resources on desktop, the unpacked assets on Android,
  the bundle on iOS), applied per engine. `SaveStore.DefaultStorage` is a method stating the platform's default medium:
  `MicroEngine.Platform.BrowserStorage` (the page's localStorage) in browsers, files elsewhere; the phone folders are
  part of `SaveStore.DefaultDirectory` (Android: the app's files folder; iOS: `Library/Application Support`), so the
  phone hosts no longer write `XDG_DATA_HOME`. Page events go to a `BrowserPage` the web host owns, and the raylib
  browser profile (`RaylibPlatform.BrowserProfile(page)`) reads that page only.
- **Browser scripted runs are deterministic** by the same rule as every host, which removes R1. raylib stays the browser
  host until stage 5 (directive of 2026-11-02); this change makes no SDL browser launcher.

## Consequences

- A game composed once runs the same way everywhere: a platform or request change made in `Configure` reaches browser and
  phone builds; two hosts in one process keep their own content and storage.
- Browser scripted captures are byte-identical from run to run, so the browser suite gains a repeat check
  (`web/tests/run.mjs repeat`); browser captures are compared with desktop captures where cross-platform identity holds
  (R4 in the determinism report).
- Breaking for code that used the statics or the browser page classes (`ContentFolder.DefaultRoot` setter,
  `SaveStore.DefaultStorage` setter, `BrowserSaveStorage`, `BrowserTouch`, `BrowserLifecycle`,
  `BrowserLauncher.CreateSession(Func<Game>)`); the migration notes list each replacement. Phone save folders are
  unchanged.
- Developer variables a phone test passes (`--env`) are still written to the phone process's environment, because game
  code reads them there, as it does on desktop and in pages; the composition reads them through `HostEnvironment`.
- Content sources other than folders are refused with their reason until the content stage (2c) supports them.
- The launcher model parses `--state-trace FILE` (`LaunchRequest.StateTracePath`): `GameHost` gives the run a
  `StateTrace` with section hashes (`RunOptions.StateTrace`, from the per-step state hash) and writes its text when
  the run ends, whatever the outcome.

## Alternatives

- **Keep per-target launchers and share only a factory.** Rejected: the rules (time, seed, request options) had already
  drifted (R1) and the factory cannot carry the composition.
- **An assembly attribute or a C# expression naming the entry point.** Rejected: both repeat in a second place what the
  code already states; reading the built assembly's metadata needs nothing from the developer and loads nothing.
- **A process-wide registry of defaults with `Reset` for tests.** Rejected: it keeps the interference between two games
  in one process that F6 names.

## Amendment (2026-11-05, ARCH-1 stage 5): developer variables on phones; the browser host

- **Phone `--env` pairs are host variables** (#89). The Consequences above said the phone hosts wrote a test's
  developer variables (`--env NAME=VALUE`) into the process environment, because game code read them there. That
  stopped once the four demos read their launch options in `Configure` through `host.Environment.Variable(name)`
  (#83-#86). The Android and iOS hosts now build their `HostEnvironment` with `Variables` that look at the launch's
  pairs first and then the process environment. They never call `SetEnvironmentVariable`. `MICROENGINE_PROBE` is read
  through the same environment. The launch logic is in `src/MicroEngine.Cli/Hosts/mobile/MobileLaunch.cs`, which both
  phone hosts copy and `MicroEngine.Cli.Tests` links. Its test fails if the pairs do not reach `Variables`. Game code
  that read such a variable with `Environment.GetEnvironmentVariable` on a phone reads it through the host environment
  (migration note, next SDK version). Desktop and browser hosts are unchanged.
- **The browser host is SDL 3** (#80, #81; [ADR 0047](0047-sdl-browser-platform.md)). The Decision above has raylib
  as the browser host until stage 5 and a raylib browser profile reading the page. Stage 5 moved the shared launcher
  model's browser caller to `MicroEngine.Sdl` (`BrowserLauncher`, `BrowserPage`, `SdlPlatform.BrowserProfile`), with
  the same members. `MicroEngine.Raylib` has no browser profile any more. The composition path this ADR describes did
  not change.
