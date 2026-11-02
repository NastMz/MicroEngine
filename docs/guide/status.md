# What MicroEngine offers today

**MicroEngine SDK 0.4.0-alpha.2.** A working 2D engine for .NET 10, open source under the MIT License, installed from
nuget.org ([Getting MicroEngine](getting-microengine.md)). Games built with it run on Windows, Linux, macOS, Android,
iOS and in web browsers. It is in Alpha: public APIs may still change between 0.x versions ([Versions](sdk.md)).

## What you get

| Area | What it does |
| --- | --- |
| Engine core | Game loop with a fixed step, scene stack with overlays and transitions, coroutines and tweens, input actions (digital and analog) with per-game touch controls, pixel-art and HD graphics (smooth scaling and texture filtering in the next SDK version), sprite atlases, tile worlds, cameras and parallax, particles and light, synthesized and file-based audio, UI widgets, saves, achievements; next SDK version: an asset cache for images, atlases from packing tools and TTF/OTF fonts ([Assets](../manual/assets.md)) |
| Platforms | SDL 3 on Windows, Linux, macOS, Android and iOS; raylib on desktops and in browsers (WebAssembly); a headless platform for tests; your own platforms as profiles ([Platforms](platforms.md)) |
| Tooling | The `microengine` tool: create a game from a template, run it, build it, and package it for every target ([The microengine tool](cli.md)) |
| Distribution | Packages for players on each platform, with the game's own name, id, version and icon ([Packaging](packaging.md), [Application identity](app-identity.md)) |
| Testing | Headless runs, scripted input and screenshot captures for automated tests of your game ([Testing](../examples/testing.md)) |
| Examples | Hello Engine (the smallest complete game) and four complete demo games ([demos](/demos/)) |

## What it does not do

| Not included | Notes |
| --- | --- |
| A visual editor, level editor or asset tool | MicroEngine is code-first: content is code (`PixelImage.FromMap`, `TileLayer.Parse` over text rows) or files made with other tools (images, packing tools' atlases, fonts) loaded at run time. A visual editor is a long-term vision on the [roadmap](/roadmap/) |
| 3D, an ECS, a physics engine, networking or a scripting language | Not part of the engine's design ([Architecture](../architecture.md)); 3D is a long-term vision |
| Cloud saves | Saves stay on the device (files) or in the browser (`localStorage`) |
| Android App Bundles (`.aab`) for Google Play | `microengine package android` builds APKs |

The complete list, with requirements per platform: [Requirements and limitations](../reference/limitations.md).

## Planned work

The [roadmap](/roadmap/) lists what comes next, without dates. A visual editor and 3D appear there only as a
long-term vision, not as available or committed features.

## Stability of the API

There is no 1.0 and no backward-compatibility promise yet. The types in [Contracts](../reference/contracts.md) are
the core ones a game uses; when one changes, the [migration notes](../migrations.md) say what changed and how to
update.
