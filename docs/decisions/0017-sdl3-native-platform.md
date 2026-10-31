# ADR 0017: SDL 3 as the native platform; raylib for browsers

## Status

Accepted. Amends [ADR 0011](0011-desktop-platform-validation.md) (references per platform and backend) and
[ADR 0012](0012-tiered-ci-and-platform-experiments.md) (mobile hosts). Evidence: [PLATFORM-3](../evaluations/platform-3.md).

## Context

MicroEngine must run on Windows, Linux, macOS, Android, iOS and in browsers. raylib 6.0 has no iOS platform, needs an
accelerated OpenGL renderer on macOS, needs our own NDK build on Android and can abort there when its audio closes.

## Decision

- **SDL 3 is the default platform on Windows, Linux, macOS, Android and iOS** (`src/MicroEngine.Sdl`, through the
  `ppy.SDL3-CS` package, which ships SDL for all five). The game draws through SDL's 2D renderer into a texture of the
  virtual size, scaled by whole factors and letterboxed; SDL picks Direct3D, Metal, OpenGL, OpenGL ES or Vulkan, and a
  profile can fix one (`SdlPlatform.CreateProfile("vulkan")`). Audio uses SDL audio streams (SDL mixes them), with OGG
  decoded by NVorbis in managed code. Touch screens get `TouchPad` controls; the mobile lifecycle comes from an SDL
  event watch.
- **Android and iOS hosts run on SDL.** Android: an `SDLActivity` whose `Main` runs the game; content files travel as
  APK assets and are unpacked once per install. iOS: `SDL_RunApp` hands the app to SDL, which calls back into .NET.
- **raylib stays for browsers** (SDL3 has no browser package; raylib's wasm build is proven) and as the `raylib`
  desktop profile.
- **`sdl-software`** (SDL's CPU renderer) is an official desktop alternative for machines without a usable GPU.
- **References per platform and backend.** SDL's GPU renderers drew identical frames on Direct3D 11, OpenGL, Vulkan
  and Android's OpenGL ES (`web/tests/reference/sdl.json`); Mesa llvmpipe differs slightly and has its own
  (`sdl-linux-x64.json`). The raylib references are unchanged.

## Alternatives considered

- **raylib everywhere, with patches.** No iOS platform to patch; carrying a raylib fork for Android and macOS.
- **MonoGame.** A framework with its own loop and content pipeline: it would replace the engine, not serve it.
- **Our own renderer over Metal, Vulkan or WebGPU.** One renderer per API to write and maintain; SDL already has them.
- **SDL in the browser too.** Possible by building SDL3 with .NET's emscripten; not needed while raylib's wasm build
  works, and it would replace a validated path without a gain.

## Consequences

- One native backend for five systems; the Android NDK build and its toolchain pin are gone.
- Two backends to maintain (SDL native, raylib web), each with tests and references.
- macOS and iOS evidence depends on the platform workflow; until it has run they are implemented, not validated.
- Browser packages carry about 150 KB (Brotli) of SDL code they never call.
