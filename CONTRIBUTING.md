# Contributing to MicroEngine

Thanks for helping make MicroEngine better. Bug reports, ideas, documentation fixes and code are all welcome. This page
explains how a contribution travels from an idea to `main`, and how to build and test the engine on your machine.

## Ways to help

- **Report a bug** or **ask for a feature** with an [issue](https://github.com/NastMz/MicroEngine/issues/new/choose).
  The templates ask for what we need to reproduce or understand it.
- **Improve the documentation.** The pages of the [website](https://micro-engine.vercel.app/docs/) are the Markdown
  files in `docs/` and `games/`; edit those, never a copy under `site/`.
- **Send a pull request.** For anything larger than a fix, open an issue first so we can agree on the approach before
  you spend time on it.
- **Security problems** go through [SECURITY.md](SECURITY.md), never a public issue.

## Before you start

- **Licence of your contribution.** What you contribute is licensed under the [MIT License](LICENSE), the same as the
  project (inbound = outbound, as GitHub's Terms of Service, section D.6, describe). There is no contributor licence
  agreement to sign. By opening a pull request you confirm that you wrote the change or have the right to submit it
  under the MIT License, and that it copies no code or assets under incompatible terms.
- **Demo games.** `games/` and the games' test projects are not MIT ([games/LICENSE](games/LICENSE)). Fixes to them are
  welcome; your change is still yours under the MIT License, and the games as a whole stay all rights reserved. New
  third-party assets need a licence that allows public redistribution (CC0 preferred) and an entry in the game's
  `CREDITS.md`.
- **Read the design first.** [Architecture](docs/architecture.md) explains the layers, the frame and the modules;
  [decisions](docs/decisions/README.md) records why the engine is the way it is.

## Build and test

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download). From the repository root:

```bash
dotnet build MicroEngine.slnx -c Release
dotnet test MicroEngine.slnx -c Release
```

The build treats warnings as errors. To play a demo from source:

```bash
dotnet run --project games/MeadowRun -c Release
```

[Engine development setup](docs/guide/getting-started.md) covers the requirements of each platform in more detail.

## Repository layout

| Path | Contents |
| --- | --- |
| `src/MicroEngine` | The `Game` class, scenes and the headless platform, beside the engine's modules (`MicroEngine.Kernel`, `.Graphics`, `.Audio`, `.Input`, `.UI`...), one project each |
| `src/MicroEngine.Sdl`, `src/MicroEngine.Raylib`, `src/MicroEngine.Desktop` | The SDL 3 and raylib platforms and the desktop launcher |
| `src/MicroEngine.Cli` | The `microengine` tool, with the browser, Android and iOS hosts it builds with |
| `templates/` | The `dotnet new microengine-game` template |
| `samples/` | Hello Engine, the Gem Garden tutorial, the focused examples and other samples the documentation quotes |
| `games/<Name>` | The four demo games: code, optional `Content/` with its `CREDITS.md`, input scripts and README |
| `tests/` | MSTest projects for the engine, the tool, the samples and each game |
| `docs/` | The documentation, the architecture, decision records and evaluations |
| `site/` | The website (Astro + Starlight), built from `docs/` and `games/` |
| `web/`, `platforms/`, `packaging/` | The browser host, the phone and macOS experiments, and the checks of native packages |

## Making a change

- **Branches and commits.** Branch from `main` as `feat/<topic>`, `fix/<topic>` or `docs/<topic>`. Commits follow
  [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `docs:`, `test:`, `ci:`). Keep a
  change focused; its tests and documentation travel in the same pull request.
- **Keep the engine genre-neutral.** Game rules live in `games/<Name>`. Promote code to the engine only when it is
  genre-neutral or two games need it.
- **Test observable behaviour.** Engine contracts go in `tests/MicroEngine.Tests`; game rules and scripted headless
  playthroughs in each game's test project.
- **Look at visual changes.** Run a script with `--hidden --captures <dir>` and inspect the PNGs before you report a
  visual change as done.
- **Document what you change.** A change to the SDK updates its pages in `docs/` in the present tense and lists itself
  under `## Next release` in [docs/guide/release-notes.md](docs/guide/release-notes.md); a breaking change also goes
  under `## Next release` in [docs/migrations.md](docs/migrations.md). Code quoted on the site comes from `samples/`:
  run `node site/scripts/snippets.mjs --write` after changing a quoted region, and
  `node site/scripts/api-reference.mjs` after changing XML comments (never edit `docs/api` by hand).
- **Record significant design changes** as a new decision record in `docs/decisions/`.
- **English everywhere:** code, comments, UI text and documentation.

## Pull requests and review

Maintainers review every pull request and decide what is merged. `main` is protected: a change reaches it only
through a pull request whose **CI result** check passes ([CI](docs/ci.md)). Pull requests from forks run without
secrets and need a maintainer's approval before their checks start. Merging never publishes anything: SDK versions and
demo releases are published separately, by the maintainers ([Releasing](docs/releasing.md)).

## Scripted runs and captures

```bash
dotnet run --project games/MeadowRun -c Release -- --script games/MeadowRun/scripts/<file>.script --hidden --mute --captures captures
```

Launcher flags: `--script FILE`, `--captures DIR`, `--hidden`, `--headless`, `--frames N`, `--seed N`, `--scale N`,
`--mute`. The script format is documented on `InputScript` in `src/MicroEngine/Core/GameRunner.cs`.

## Native desktop checks (Linux)

`dotnet test` skips the native tests (category `Native`) when there is no display or sound server. To run them, and the
scripted scenarios against the Linux visual reference, as CI does (needs `xvfb`, `libxtst6`, `pulseaudio` and
`pulseaudio-utils`):

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

## Repository history

MicroEngine was rebuilt from a clean slate in October 2026 ([decision 0001](docs/decisions/0001-greenfield-rebuild.md)).
The public history starts on 2026-10-30; the previous implementation and the first days of the rebuild are kept in the
maintainer's archive. Commit hashes and Actions run IDs quoted in evaluations and task records written before
2026-10-31 refer to that archived history.

## Code of Conduct

Everyone taking part follows the [Code of Conduct](CODE_OF_CONDUCT.md).
