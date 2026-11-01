# MicroEngine

A code-first 2D game engine for C#/.NET 10 that orchestrates subsystems and integrates existing technologies (SDL 3,
raylib, .NET, WebAssembly) behind one contract, with four complete pixel-art games built on it. **SDK Alpha, 0.x:**
it works on six platforms, and its APIs may still change between Alpha versions ([SDK status](docs/guide/sdk.md)).

Play the demos in the browser; demo [releases](https://github.com/NastMz/MicroEngine/releases) also carry native
downloads for each system where a checked package exists ([Downloads](docs/guide/downloads.md)).

| Game | Genre | Run |
| --- | --- | --- |
| [Meadow Run](games/MeadowRun/README.md) | Side-scrolling platformer | `dotnet run --project games/MeadowRun -c Release` |
| [Courtyard Key](games/CourtyardKey/README.md) | Top-down action adventure | `dotnet run --project games/CourtyardKey -c Release` |
| [Trail Duel](games/TrailDuel/README.md) | Creature-collecting RPG | `dotnet run --project games/TrailDuel -c Release` |
| [Neon Rift](games/NeonRift/README.md) | Run & gun | `dotnet run --project games/NeonRift -c Release` |

The four games cover the range of content approaches. Meadow Run is fully procedural: every sprite,
tile, sound effect and song is generated in code at startup. Courtyard Key and Trail Duel are hybrids
([ADR 0005](docs/decisions/0005-hybrid-content.md)): Courtyard Key keeps a procedural world, cast and
boss and imports one crafted enemy; Trail Duel casts its creatures and trainers from hand-drawn CC0
sprites, adds crafted battle effects, and paints its world, stages and music in code to match.
Neon Rift is built from CC0 sprite sheets, tilesets, WAV effects and OGG music loaded through the
engine's file loaders ([ADR 0004](docs/decisions/0004-external-asset-files.md)). Controls are shared: arrows/WASD to move, Z/Space to confirm, X to cancel, C for the
secondary action, Esc/P to pause, F11 for fullscreen; gamepads work too.

## Engine at a glance

- Fixed-step runner, scene stack with overlays and transitions, coroutines and tweens
- Action-based input (keyboard + gamepad), scripted input playback for tests and captures
- Procedural pixel art (`PixelImage`, palettes, noise, dithering), sprite atlases, animation, particles,
  camera with dead zone/bounds/shake, parallax layers, built-in proportional pixel font
- Managed PNG and WAV loading, grid sprite-sheet slicing, streamed OGG/MP3 music, a content folder
- Tile layers, swept tile collision with one-way platforms, autotiling, plain entities
- Retro sound synthesizer and step-sequencer music, mixer with music fades
- Dialogue boxes, menus, JSON saves, headless platform for deterministic tests
- Achievements: per-game catalogs, immediate and progressive goals, saved progress, unlock banners and a
  list screen (every game ships five)
- SDL 3 platform for Windows, Linux, macOS, Android and iOS, and raylib for browsers, behind a small `IPlatform` boundary with named, replaceable platform profiles

## Documentation

- [Introduction](docs/guide/introduction.md) — what MicroEngine is, who it is for, the four games, design principles
- [Getting MicroEngine](docs/guide/getting-microengine.md) — use the engine in a game
- [SDK status](docs/guide/sdk.md) — what Alpha means, the SDK packages and how a release is published
- [The microengine tool](docs/guide/cli.md) and [packaging](docs/guide/packaging.md) — create, run and distribute a game
- [Downloads](docs/guide/downloads.md) — install the demos on Windows, Linux, macOS and Android
- [Engine development setup](docs/guide/getting-started.md) — requirements, build, test, run a demo
- [Engine manual](docs/manual/lifecycle.md) — lifecycle, scenes, rendering, input, audio, assets, UI, persistence, achievements, backends
- [Contracts](docs/reference/contracts.md) — the public types games and backends depend on, with signatures
- [Architecture](docs/architecture.md) — layers, frame, modules, design rationale
- [Making a game](docs/making-a-game.md) — step-by-step guide with code
- [Decisions](docs/decisions/README.md) — architecture decision records
- [Run & gun evaluation](docs/evaluations/neon-rift.md) — what building Neon Rift showed about the engine
- [Web builds](web/README.md) — the four games as browser packages: setup, build, run, embed
- [Platforms](docs/guide/platforms.md) — the six targets and exactly how each was validated
- [Native packages](packaging/README.md) — how the demo downloads are built, checked and released
- [Public site](docs/deployment.md) — the landing, demo pages and documentation site in `site/`: pipeline, Vercel setup
- [Web support evaluation](docs/evaluations/web-support.md) — what was verified in Chrome and Firefox, sizes, limits
- [WebAssembly feasibility](docs/evaluations/web-feasibility.md) — the WEB-0 experiment that preceded it
- [Visual excellence evaluation](docs/evaluations/visual-excellence.md) — procedural, hybrid and external content across the four games
- [Contributing](CONTRIBUTING.md) — build, test and capture commands

## Build and test

```bash
dotnet build MicroEngine.slnx -c Release
dotnet test MicroEngine.slnx -c Release
```

Requires the .NET 10 SDK on Windows, Linux or macOS. Windows 11 is played by hand; Linux and macOS are checked
automatically, Android on an emulator and iOS in the simulator ([Platforms](docs/guide/platforms.md)).

## History

MicroEngine was rebuilt from a clean slate in October 2026 ([ADR 0001](docs/decisions/0001-greenfield-rebuild.md)).
The previous implementation and the first days of the rebuild are kept in the maintainer's archive, not in the
public history, which starts on 2026-10-30. Commit hashes and Actions run IDs quoted in evaluations and task records
written before 2026-10-31 refer to that archived history.

## License

MicroEngine is open source under the [MIT License](LICENSE). The four demo games are not: they are published to
play and to learn from, with all rights reserved.

| Part | Paths | Licence | You may |
| --- | --- | --- | --- |
| Engine and backend | `src/` | [MIT](LICENSE) | use, modify, fork, redistribute and sell, keeping the copyright and licence notice |
| Engine tests, CI, web host, phone hosts, packaging, site code, docs | `tests/MicroEngine*.Tests/`, `.github/`, `web/`, `platforms/`, `packaging/`, `site/`, `docs/` | [MIT](LICENSE) | the same |
| Samples | `samples/` (HelloEngine) | [MIT](LICENSE) | copy them into your own games |
| Demo games | `games/<Name>/`, `tests/<Name>.Tests/`, their builds, browser packages and downloads | [all rights reserved](games/LICENSE) | read, build and run them to learn; reuse ideas and techniques in original games. Not redistribute, sell or republish them under another name |
| Third-party art and audio of the demos | the files listed in each game's `CREDITS.md` | their own licences (CC0 1.0) | what their licence allows |
| Third-party libraries | SDL 3, ppy.SDL3-CS, NVorbis, raylib, Raylib-cs, .NET, Emscripten, site packages and fonts | see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) | what their licences allow |

**Games you make are yours.** Commercial games built with MicroEngine need no royalty or permission. Ship the
engine's assemblies inside your game and include MicroEngine's MIT notice (the [LICENSE](LICENSE) text) and the
third-party notices with it, for example in a licences file or credits screen. Forks are welcome under the same
terms: keep the original copyright notice.

## Contributing

Contributions are welcome under the MIT License: see [CONTRIBUTING.md](CONTRIBUTING.md). Security problems:
[SECURITY.md](SECURITY.md). Everyone taking part follows the [Code of Conduct](CODE_OF_CONDUCT.md).
