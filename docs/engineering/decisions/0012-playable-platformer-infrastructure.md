# ADR-0012 — Optional 2D utilities and real-art platformer

Status: accepted2026-10-18 by root under technical delegation after checkpoint_design_review targeted PASS, before source.

## Context and decision
The owner requests a real-art Mario-inspired playable validation, not a package-specific Core or an example-owned general engine. Prior PO-1/AV-1 deferred collision/playback extraction until a demonstrated need; this new horizon supplies it. Add opt-in MicroEngine.TwoD referencing Loop only: immutable StaticWorld axis-separated AABB sweep, FrameAnimation deterministic region playback and ImageSet keyed lazy image leases. Core/Hosting/Loop stay unchanged. A single level uses ordinary object composition, not a new scene manager. Camera offset and draw order compose existing public APIs, not a renderer in the game.

Image decoding belongs to a concrete adapter: public DesktopImageDecoder.Decode(byte[] png) -> PixelImage, native CPU decode/free with positive dimension/pixel budget and no native handles escaping. No new package. Consumers can substitute any decoder returning PixelImage. DesktopWindowOptions keeps its existing constructor/default256image/32leases; a new five-argument constructor makes adapter budgets explicit (dimension1..8192, resources1..256). This resolves actual Kenney902x902sheet rejection without imposing a universal size. Point sampling remains this adapter profile, not a style rule; arbitrary positive destination sizes already work. Future filtering is an optional capability extension, not a forced Core enum now. Vector sources/high-resolution files do not imply native SVG or universal support.

## Alternatives
Keep utilities in the game: rejected by owner and a second concrete consumer need. Full scene/physics/asset framework or third-party physics dependency: premature. Raw/preconverted proprietary image blobs: avoids decoding but duplicates assets and obscures provenance; reject. Managed PNG package: reasonable replaceable future adapter, unnecessary dependency for existing Raylib Desktop. Remove every budget: unsafe; retain configurable bounded limits and explicit failure. Change old256default: unnecessary compatibility break; retain it.

## Classification
General reusable extension: static collision, image lease cache and frame playback. Existing capability: rendering/lifecycle/input/regions/labels. Adapter: PNG decode and configurable GPU limits. Game: level tiles/coins/hazards, gravity/speed/jump buffer, camera-follow policy, coin-gated finish/death/reset and Kenney metadata mapping. One level needs no scene infrastructure. No genre module in Core; no ECS, broad physics, audio, export, parallax, editor, enemy AI or arbitrary art pipeline.

## Consequences and review
TwoD has no native/platform/package dependency. World does not prescribe gravity, ground, tile size or timestep; accepts caller displacement. Playback accepts arbitrary frame regions/duration; image lifetime remains provider-owned, set owns leases and releases reverse acquisition order even after partial failure. Add public deterministic tests, native actual-asset upload/course/replay evidence, old226regressions and13native harnesses. Windows validation only. Owner visual/control assessment remains human. Latest instruction forbids push/main integration. See [KPF-1](../specs/kenney-platformer.md).
