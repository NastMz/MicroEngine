# ADR 0011: Desktop platform validation on Windows, Linux and macOS

## Status

Accepted.

## Context

MicroEngine was validated on Windows only, and CI ran the .NET tests on Windows only. The tests are headless: none
opens a raylib window, the scripts force actions on `InputMap`, and audio is silent. Run by hand on Linux x64, the
solution built, every test passed, and the 15 scripted scenarios drew their 138 captures deterministically under a
virtual display. The same run found a real Linux defect: with no `~/.local/share` folder, .NET reports an empty
local application data path and saves were written to the working directory.

## Decision

- **Save folders follow each system's convention.** `SaveStore.DefaultDirectory` is always absolute: local
  application data on Windows (unchanged), the same folder on macOS without requiring it to exist, and the XDG data
  folder on Linux (`$XDG_DATA_HOME` when absolute, else `~/.local/share`).
- **One test matrix.** The engine and game tests run on Windows, Linux and macOS. Tests that only apply to one system
  use `[OSCondition]` and are reported as skipped with their reason. macOS does not block until it has a record.
- **Native checks are separate tests.** `tests/MicroEngine.Raylib.Tests` (category `Native`) drives the real backend:
  keyboard events from the X server through XTEST, and audio recorded from a virtual PulseAudio sink. Without their
  display or sound server they are skipped, except in the native CI job (`MICROENGINE_NATIVE_TESTS=1`), where that
  is a failure.
- **Per-system visual references, compared with a tolerance.** The scripted captures are summarised (size, exact
  pixel hash, 32 × 18 mean-colour grid) in a reference file per system. A run fails when a capture is missing, has
  another size, or a cell moved by more than 3/255; exact equality is reported, not required. Windows and Linux are
  never compared pixel for pixel.
- **Support needs a person.** Automated checks on virtual devices do not make a system supported: Linux stays "checked
  automatically, not yet supported" until it has been played on real hardware.

## Alternatives considered

- **Exact pixel hashes only.** Simplest, but a Mesa or driver update that rounds blending differently would fail
  every capture with no visible change.
- **Committing the 138 reference PNGs.** About 7 MB per system and per visual change; the fingerprints are 340 KB and
  catch the regressions that matter.
- **Injecting keys with xdotool.** Works, but adds a tool to install; XTEST through `libXtst` is already on any X
  system and keeps the test self-contained.
- **Native rendering on Windows runners.** Not attempted: hosted Windows runners have no GPU and their OpenGL support
  for raylib is unverified. Windows rendering stays covered by the recorded browser comparison and manual play.

## Consequences

Linux regressions in rendering, keyboard handling, audio output and save folders are caught on every engine change.
An intended visual change requires regenerating the Linux reference in the same environment. macOS results start to
accumulate without blocking. Gamepads, Wayland, real GPUs and fullscreen remain manual checks.
