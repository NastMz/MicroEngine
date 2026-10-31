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

Follow the [Code of Conduct](CODE_OF_CONDUCT.md).
