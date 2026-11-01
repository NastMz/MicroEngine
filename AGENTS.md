# MicroEngine contributor entry point

Read [README](README.md), [architecture](docs/architecture.md), [making a game](docs/making-a-game.md)
and [decisions](docs/decisions/README.md) before planning or editing. Repository context must be
sufficient without previous chats or agent memory.

## Current state

- `main` holds the rebuilt engine (`src/`, SDK Alpha: `MicroEngineVersion` in `Directory.Build.props`, [SDK status](docs/guide/sdk.md)),
  the `microengine` tool (`src/MicroEngine.Cli`, which also holds the browser, Android and iOS hosts), the `dotnet new`
  template (`templates/`) and four demo games (`games/`), playable in browsers and as native downloads built by
  `microengine package` and checked by [`packaging/`](packaging/README.md). A game's name, id, version and icon live
  only in its project file ([application identity](docs/guide/app-identity.md)); `sdk/tests/` checks the SDK from a
  project outside the repository. Nothing is on nuget.org until the owner runs `sdk-release.yml` with `publish`. The public history starts on 2026-10-30;
  the previous implementation and the first days of the rebuild live only in the maintainer's archive, so commit
  hashes and Actions run IDs quoted in older records point there. Never push branches from an old clone.
- The active rebuild record (specs, tasks, log) is [`odd/tasks/engine-rebuild.md`](odd/tasks/engine-rebuild.md); the latest
  feature records are [`odd/tasks/sdk-1.md`](odd/tasks/sdk-1.md), [`odd/tasks/input-1.md`](odd/tasks/input-1.md) and
  [`odd/tasks/product-1.md`](odd/tasks/product-1.md).
- CI ([docs/ci.md](docs/ci.md), ADR 0013): `ci.yml` runs the checks a change needs, once per content (evidence);
  `validate.yml` runs everything on demand; `platforms.yml` holds the monthly and manual macOS, Android and iOS
  experiments of [`platforms/`](platforms/README.md), which are not supported platforms. Demos reach the public site
  only through `release.yml`; merging never publishes them.
- `site/` is the public site (Astro + Starlight): it publishes the Markdown listed in `site/src/lib/repo-docs.mjs`
  straight from `docs/` and `games/`, so edit those files, never a copy. See [publishing the site](docs/deployment.md).
- Licensing ([ADR 0015](docs/decisions/0015-mit-engine-reserved-demos.md)): the engine, samples, tools, site code and
  docs are MIT (`LICENSE`); the four demo games, their tests and builds are all rights reserved (`games/LICENSE`),
  except the third-party files listed in each game's `CREDITS.md`. Never describe a demo as MIT; every demo page
  must carry its notice (the site build checks it). The repository is public ([PUBLIC-1](docs/evaluations/public-1.md)):
  `main` is protected by a ruleset (pull request and the **CI result** check), and pull requests from forks are
  untrusted code (ADR 0014).

## Working rules

- Keep the engine genre-neutral; game rules live in `games/<Name>`.
- Content is code-first by default (ADR 0002). Neon Rift (ADR 0004) and the Courtyard Key and Trail Duel hybrids
  (ADR 0005) ship CC0 files under `games/<Name>/Content`, each listed in the game's `CREDITS.md`; Meadow Run stays fully procedural.
- Test public behaviour; for visual or feel changes, run scripted captures and inspect them.
  Never report something as verified unless it was run or looked at.
- Significant architectural changes get an ADR in `docs/decisions/`.
- Technical artifacts in English. Conventional Commits, no AI attribution lines.
- Branch names: `feat/<topic>`, `fix/<topic>`, `docs/<topic>`. No force-push, PR or release without the owner asking.
