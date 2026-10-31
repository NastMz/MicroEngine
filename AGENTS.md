# MicroEngine contributor entry point

Read [README](README.md), [architecture](docs/architecture.md), [making a game](docs/making-a-game.md)
and [decisions](docs/decisions/README.md) before planning or editing. Repository context must be
sufficient without previous chats or agent memory.

## Current state

- Branch `feat/engine-rebuild` holds the rebuilt engine (`src/`) and three games (`games/`).
  The previous implementation is preserved at tag `reference/pre-rebuild`; do not restore it over active work.
- The active rebuild record (specs, tasks, log) is [`odd/tasks/engine-rebuild.md`](odd/tasks/engine-rebuild.md).
- CI runs only the checks a change needs (`.github/scripts/changes.mjs`, ADR 0012); `platforms.yml` holds the weekly
  macOS, Android and iOS experiments of [`platforms/`](platforms/README.md), which are not supported platforms.
- `site/` is the public site (Astro + Starlight): it publishes the Markdown listed in `site/src/lib/repo-docs.mjs`
  straight from `docs/` and `games/`, so edit those files, never a copy. See [publishing the site](docs/deployment.md).
- The repository is private and MicroEngine is not open source: public pages must not link into the repository,
  claim a licence, or require building from source (the site build enforces this). Licensing is pending the
  owner's decision ([proposal](docs/legal/distribution-proposal.md)).

## Working rules

- Keep the engine genre-neutral; game rules live in `games/<Name>`.
- Content is code-first by default (ADR 0002). Neon Rift (ADR 0004) and the Courtyard Key and Trail Duel hybrids
  (ADR 0005) ship CC0 files under `games/<Name>/Content`, each listed in the game's `CREDITS.md`; Meadow Run stays fully procedural.
- Test public behaviour; for visual or feel changes, run scripted captures and inspect them.
  Never report something as verified unless it was run or looked at.
- Significant architectural changes get an ADR in `docs/decisions/`.
- Technical artifacts in English. Conventional Commits, no AI attribution lines.
- Branch names: `feat/<topic>`, `fix/<topic>`, `docs/<topic>`. No force-push, PR or release without the owner asking.
