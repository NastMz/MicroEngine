# Contributing to MicroEngine

## Setup

Install the .NET 10 SDK. From the repository root:

```bash
dotnet build MicroEngine.slnx -c Release
dotnet test MicroEngine.slnx -c Release
```

## Layout

| Path | Contents |
| --- | --- |
| `src/MicroEngine` | Backend-independent core (no native dependencies) |
| `src/MicroEngine.Raylib` | Desktop backend and `DesktopLauncher` |
| `games/<Name>` | A complete game: code, procedural art and audio, optional `Content/` files with `CREDITS.md`, input scripts, README |
| `samples/HelloEngine` | Minimal example game |
| `tests/<Name>.Tests` | MSTest projects (engine and each game) |
| `docs/` | Architecture, game guide, decision records |

## Rules of thumb

- Keep genre rules in games. Promote code to the engine only when it is genre-neutral or used by two games.
- Test observable behaviour: engine contracts in `tests/MicroEngine.Tests`, game rules and scripted
  headless playthroughs in each game's test project.
- Visual changes need a look: run a script with `--hidden --captures <dir>` and inspect the PNGs.
- Record significant architectural changes as a new ADR in `docs/decisions/`.
- Code, comments, UI text and docs are in English. Commits follow Conventional Commits.

## Scripted runs and captures

```bash
dotnet run --project games/MeadowRun -c Release -- --script games/MeadowRun/scripts/<file>.script --hidden --mute --captures captures
```

Launcher flags: `--script FILE`, `--captures DIR`, `--hidden`, `--headless`, `--frames N`, `--seed N`,
`--scale N`, `--mute`. The script format is documented on `InputScript` in `src/MicroEngine/Core/GameRunner.cs`.

## Native desktop checks (Linux)

`dotnet test` skips the native tests (`tests/MicroEngine.Raylib.Tests`, category `Native`) when there is no display
or sound server. To run them, and the scripted scenarios against the Linux visual reference, as CI does (needs
`xvfb`, `libxtst6`, `pulseaudio` and `pulseaudio-utils`):

```bash
pulseaudio --start --exit-idle-time=-1 && pactl unload-module module-suspend-on-idle
pactl load-module module-null-sink sink_name=microengine && pactl set-default-sink microengine
export MICROENGINE_NATIVE_TESTS=1 MICROENGINE_AUDIO_MONITOR=microengine.monitor
dotnet build MicroEngine.slnx -c Release
xvfb-run -a -s "-screen 0 1920x1080x24" dotnet test tests/MicroEngine.Raylib.Tests -c Release --no-build --filter TestCategory=Native
xvfb-run -a -s "-screen 0 1920x1080x24" node web/tests/desktop.mjs --reference web/tests/reference/linux-x64.json
```

After an intended visual change, regenerate the reference with `--write-reference web/tests/reference/linux-x64.json`
(in the same environment) and look at the captures before committing it.

Follow the [Code of Conduct](CODE_OF_CONDUCT.md).
