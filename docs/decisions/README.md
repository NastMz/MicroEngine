# Architecture decisions

Records for the rebuilt engine (2026-10-29 onward). Decisions of the previous implementation remain
available at tag `reference/pre-rebuild` under `docs/engineering/decisions/`.

| ID | Decision | Status |
| --- | --- | --- |
| [0001](0001-greenfield-rebuild.md) | Rebuild the engine and games from a clean slate | Accepted |
| [0002](0002-code-first-content.md) | Procedural art and synthesized audio instead of asset files | Accepted |
| [0003](0003-scenes-entities-runtime.md) | Scene stack, plain entities, fixed-step runner, raylib behind `IPlatform` | Accepted |
| [0004](0004-external-asset-files.md) | Load external image and audio files (amends 0002) | Accepted |
| [0005](0005-hybrid-content.md) | Hybrid games: generated and file content side by side (amends 0004) | Accepted |
| [0006](0006-achievements.md) | Engine-owned achievements, game-owned rules | Accepted |
| [0007](0007-browser-host.md) | Browser host: page-driven loop and pluggable save storage | Accepted |
| [0008](0008-web-packages.md) | Web packages: one host, gesture start, pinned toolchain | Accepted |
| [0009](0009-public-site.md) | Public site: Astro and Starlight, documentation read from the repository, pinned demo archive | Accepted |
