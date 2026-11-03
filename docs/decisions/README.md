# Architecture decisions

Records for the rebuilt engine (2026-10-29 onward). Decisions of the previous implementation are kept in the
maintainer's archive, not in the public history.

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
| [0010](0010-delivery-pipeline.md) | Content-addressed demo packages and a prebuilt Vercel deployment pipeline (amends 0009) | Accepted |
| [0011](0011-desktop-platform-validation.md) | Desktop platform validation: Windows, Linux and macOS test matrix, native Linux checks, per-system visual references | Accepted |
| [0012](0012-tiered-ci-and-platform-experiments.md) | Tiered CI driven by changed paths, and a separate platform workflow (amends 0010, 0011) | Accepted |
| [0013](0013-ci-cost-and-manual-releases.md) | Content evidence in daily CI, manual full validation, and demo releases (amends 0010, 0012) | Accepted |
| [0014](0014-public-repository-ci.md) | Project-graph check selection and fork-safe CI for a public repository (amends 0013) | Accepted |
| [0015](0015-mit-engine-reserved-demos.md) | MIT engine, all-rights-reserved demo games | Accepted |
| [0016](0016-platform-profiles.md) | Platform profiles, replaceable subsystems and suspension | Accepted |
| [0017](0017-sdl3-native-platform.md) | SDL 3 as the native platform; raylib for browsers (amends 0011, 0012) | Accepted |
| [0018](0018-native-demo-distribution.md) | Native demo downloads through the existing release pipeline (amends 0010, 0013) | Accepted |
| [0019](0019-sdk-packages.md) | SDK Alpha versioning and one package per backend | Accepted |
| [0020](0020-unified-input-and-touch-controls.md) | Unified digital and analog input; engine-owned touch controls on every platform (amends 0017) | Accepted |
| [0021](0021-sdk-toolchain.md) | The SDK toolchain: identity in the game's project, one `microengine` tool, demos as its consumers (amends 0018, 0019) | Accepted |
| [0022](0022-release-coordination.md) | Three releases with explicit contracts; an SDK released by one pull request and one approval (amends 0013, 0019) | Accepted |
| [0023](0023-frame-pacing-and-measurement.md) | Frames paced to the display, measured by the engine; checks that are never heard | Accepted |
| [0024](0024-graphics-configuration.md) | Graphics options chosen by the game and changed at run time; a capability-based renderer as GRAPHICS-1 | Accepted |
| [0025](0025-asset-pipeline.md) | Asset pipeline: managed decoders, FontStashSharp fonts, imported atlases, an engine asset cache (amends 0002, 0004) | Accepted |
| [0026](0026-kernel-modules-and-schedule.md) | A kernel with modules, a typed service registry and a phased frame schedule; `Game` is a module façade (amends 0003, 0016) | Accepted |
| [0027](0027-loop-owned-time.md) | The loop owns time; backends keep only a monotonic clock; time mode and sound are explicit options (amends 0003, 0023) | Accepted |
| [0028](0028-graphics-device-contract.md) | One graphics device contract for every tier: declared capabilities, a typed error, core validation, premultiplied alpha, a CPU reference device (amends 0003, 0017, 0024) | Accepted |
| [0029](0029-presentation-replaces-canvas-scaling.md) | `Presentation` replaces `CanvasScaling`: one choice per field, pixel art and high-resolution 2D as presets (amends 0024) | Accepted |
| [0032](0032-input-devices-players-layers.md) | Input as device events with ids, players and layers (amends 0020) | Accepted |
| [0033](0033-one-mixer-over-pcm-sinks.md) | One managed mixer over PCM sinks (`IAudioSink` v1); a feeder thread is the default SDL sink, an AudioWorklet queue the browser one | Accepted |
| [0034](0034-content-handles-loaders-sources.md) | Content handles with generations, a loader registry, a reserved import step and content sources beyond folders (amends 0004, 0025) | Accepted |
| [0037](0037-platform-host-contract.md) | A platform host contract with capabilities, explicit options and no time coupling (amends 0016, 0027) | Accepted |
| [0038](0038-one-composition-path.md) | One composition path for desktop, browser, phones and tests: `MicroEngine.Hosting`, a `Configure` entry point, no statics (amends 0007, 0016, 0021) | Accepted |
