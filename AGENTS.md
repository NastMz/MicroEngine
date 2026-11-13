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
  project outside the repository. The SDK version the documentation presents is in `sdk/version.json`
  (published on nuget.org, tag `sdk-v<version>`; [versions](docs/releasing.md#versions)); a release is published only
  with the owner's approval, through `sdk-release.yml` (Trusted Publishing, `nuget` environment). SDK tags `sdk-v*` and demo
  releases `v*` are separate series. The public history starts on 2026-10-30;
  the previous implementation and the first days of the rebuild live only in the maintainer's archive, so commit
  hashes and Actions run IDs quoted in older records point there. Never push branches from an old clone.
- The active rebuild record (specs, tasks, log) is [`odd/tasks/engine-rebuild.md`](odd/tasks/engine-rebuild.md); the latest
  feature records are [`odd/tasks/docs-1.md`](odd/tasks/docs-1.md), [`odd/tasks/sdk-1.md`](odd/tasks/sdk-1.md),
  [`odd/tasks/input-1.md`](odd/tasks/input-1.md), [`odd/tasks/product-1.md`](odd/tasks/product-1.md),
  [`odd/tasks/asset-1.md`](odd/tasks/asset-1.md) (the asset pipeline, ADR 0025),
  [`odd/tasks/demo-refresh.md`](odd/tasks/demo-refresh.md) (the four demos modernized, new art for the two hybrids) and
  [`odd/tasks/tile-1.md`](odd/tasks/tile-1.md) (maps from the Tiled editor, the optional `MicroEngine.Tiled` package, ADR 0052).
- CI ([docs/ci.md](docs/ci.md), ADR 0013): `ci.yml` runs the checks a change needs, once per content (evidence),
  the SDK check included; `validate.yml` runs everything on demand; `platforms.yml` holds the monthly and manual
  macOS, Android and iOS experiments of [`platforms/`](platforms/README.md), which are not supported platforms. Demos
  reach the public site only through `release.yml`; merging never publishes them. An SDK release is one pull request
  (`node sdk/version.mjs release <version>`) and the `nuget` environment's approval ([releasing](docs/releasing.md),
  ADR 0022); never hand-edit version references.
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
  (ADR 0005) ship CC0 or CC BY files under `games/<Name>/Content`, each listed (with its attribution) in the game's
  `CREDITS.md`; Meadow Run stays fully procedural.
- Test public behaviour; for visual or feel changes, run scripted captures and inspect them.
  Never report something as verified unless it was run or looked at.
- Significant architectural changes get an ADR in `docs/decisions/`.
- The site is product documentation for people making games. Decision records, evaluations, task records, CI and
  release procedures and project code names (PLATFORM-3, SDK-1...) are engineering material: they stay in the
  repository and are never published, linked or quoted on public pages (`site/src/lib/repo-docs.mjs` refuses such
  links, `site/scripts/check-dist.mjs` refuses such content). Public pages state capabilities and functional limits,
  not how they were tested.
- Public documentation describes the published SDK. Its code is quoted from samples (`samples/GemGarden`,
  `samples/Examples`, `samples/HelloEngine`), which reference the published packages and build against the source in the
  repository; a fenced block after `<!-- code: path#region -->` must equal that file region (the site build checks it;
  `node site/scripts/snippets.mjs --write` refreshes them). `docs/api` is generated from the XML comments by
  `node site/scripts/api-reference.mjs`; never edit it by hand.
- Documentation is written in the present tense, as if the SDK that contains it were published: never "not published
  yet", "next SDK" or "coming in". A pull request that changes the SDK lists the change under `## Next release` in
  `docs/guide/release-notes.md` (and breaking changes under `## Next release` in `docs/migrations.md`); while that
  section has content, production keeps its deployment, and the SDK release that ships it deploys the site
  ([documentation and releases](docs/releasing.md#documentation-and-releases)).
- Technical artifacts in English. Conventional Commits, no AI attribution lines.
- Branch names: `feat/<topic>`, `fix/<topic>`, `docs/<topic>`. No force-push, PR or release without the owner asking.
