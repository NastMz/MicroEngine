# 0002 — Procedural art and synthesized audio

Status: Accepted (2026-10-29)

## Context

The previous games mixed CC0 sprite packs from several authors. Styles did not match each other,
provenance needed bookkeeping, and one source carried an unresolved similarity concern. A deliberate
visual identity per game is hard to reach by mixing packs.

## Decision

All game art is authored in code at load time (`PixelImage`, char maps with palettes, `Art` noise,
dithering and ramps, packed by `SpriteAtlasBuilder`). The font is a built-in proportional pixel face.
All sound effects use `Synth` (sfxr-style parameters) and all music uses the `Song` step sequencer.

## Consequences

- No binary assets, no licence ambiguity; each game controls its palette end to end.
- Art and gameplay change in the same reviewable diff; generation cost is a few milliseconds at load.
- Image/audio file loading is not part of the engine today. A backend can add it behind
  `IGraphicsDevice`/`IAudioDevice` when a game needs authored files.
