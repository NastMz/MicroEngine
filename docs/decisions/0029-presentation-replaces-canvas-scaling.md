# ADR 0029: `Presentation` replaces `CanvasScaling`; pixel art is a preset

## Status

Accepted (2026-11-03). Amends [ADR 0024](0024-graphics-configuration.md) (graphics options chosen by the game: how the
canvas reaches the window becomes one record of independent choices). Builds on
[ADR 0028](0028-graphics-device-contract.md) (`SamplerDescription`). Evidence: ARCH-1 dossier §3 (S14, S15), §7;
annex [A2](../evaluations/arch-1/A2-render.md) R6 and §4 items 1, 3, 4, 5, 6 and 8.

## Context

`GameSettings.Scaling` (`CanvasScaling.Pixel` or `Smooth`) decided five things at once: the canvas's present filter, the
rounding of its scale (whole factors with an 80 % rule, or fractional), the textures' default filter, the initial window
size (whole factors or not) and the capture scale (3 or 1). A game could not ask for combinations the switch did not
encode, such as whole-factor scaling with a smooth present filter, a smooth HD canvas with nearest textures and 1×
captures, or pixel art captured at its own size, without finding hidden rules in the backends. The rules were also
spread over `FrameLayout`, `WindowSizing`, `PlatformRequest`, `GameSettings` and each backend.

## Decision

- **`Presentation`** (`MicroEngine.Graphics`, in `MicroEngine.Graphics.dll`), a record with one field per choice:
  `Rounding` (`Fractional`, `Integer`, `IntegerUnlessWasteful` with `WastefulThreshold`, 0.8 by default), `Aspect`
  (`Keep`, `Stretch`), `Upscale` (the canvas's sampler on its way to the window), `DefaultSampler` (the explicit default
  of textures created without a filter), `CaptureScale` and `WholeFactorWindows`. Invalid values throw when set.
- **Presets**: `Presentation.PixelArt` (whole factors unless wasteful, nearest everywhere, captures at 3×, whole-factor
  windows) and `Presentation.HighResolution2D` (fractional, linear everywhere, captures at 1×). They reproduce exactly
  what `Pixel` and `Smooth` did; games change any field with `with`.
- **`GameSettings.Presentation`** (null by default) wins over `Scaling`, which stays as the shorthand naming a preset;
  `EffectivePresentation` is the one in effect. `DefaultTextureFilter` comes from `DefaultSampler` unless
  `TextureFilter` is set; `PlatformRequest.CaptureScale` defaults to `CaptureScale`; `WindowSizing.Initial` uses
  `WholeFactorWindows`.
- **One layout rule** (`Presentation.Fit`) that every backend uses to place the canvas in the window or its safe area
  (portrait screens at the top), and that SDL and raylib use for their present filter (`Upscale`).

## Consequences

- No behaviour changes for existing games: the presets equal the old switch (checked by the existing scaling, filter
  and window-size tests, which still pass, and by tests comparing the presets with the old layout).
- `Aspect.Expand` (a canvas that grows with the window) is not offered: the canvas is still fixed at start. Views (a
  world view plus a UI view at screen resolution) and run-time canvas changes come with the 2D layer over the device.
- The on-screen controls keep their own layout rule (`VirtualControls.Frame`), fed with the rounding the presentation
  implies.
- Font pages still choose their filter from `FontOptions` (A2 §4 item 2 is not changed here: it would alter the text of
  existing HD games).

## Alternatives

- **Per-sprite snap flags.** Rejected: Godot's experience; the choice belongs to the presentation, not to each draw.
- **Keep the five-way coupling and document it.** Rejected: documenting a hidden rule does not let a game choose.
- **Remove `Scaling`.** Rejected for now: it would break every game for no gain; it is a one-line preset name.
