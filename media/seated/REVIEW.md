# Review and delivery note

Film: `renders/seated.mp4`, 1920 x 1080, 30 fps, 50.0 s, H.264 + AAC stereo.

## Creative calls

- Storyline A, "Seated", was built; B ("Fixed step") and C ("Parts list") are in `STORYLINES.md`.
- The look is the site's own "Socket" system. Every line of copy is the project's: the site's opening line, the
  label of the Hello Engine code panel, and the mark's construction notes ("the left socket stays empty").
- Everything that looks like the engine is the engine: the code is `samples/HelloEngine/Program.cs`, the footage
  comes from scripted runs of the demos, and the score and effects are rendered by `MicroEngine.Audio`.
- The board is a diagram of the architecture, not a recording of a feature. No version, platform, count or upstream
  library is named.
- Footage avoids Trail Duel's village tileset (CC BY 4.0), so the film needs no attribution card; the battle shot
  uses CC0 creatures on a stage drawn in code.

## Measured

- `hyperframes check`: passes. Kept on purpose: `nested_media_start_basis_ambiguous` (the footage is timed inside
  its scene), `text_occluded` at 41.7 s (graphite rising over the claim is the transition).
- Audio of the rendered file: -15.8 LUFS integrated, true peak -1.0 dBTP, no clipping. Onsets sit on the beat grid;
  the steepest moment of the file is at 44.06 s, the frame after the module seats in the mark (44.00 s).
- Section levels (mean): hook -28 dB, sprite -22 dB, worlds -17 dB, board -17 dB, claim -16.5 dB, mark -15 dB.

## Looked at

Snapshots at the QA frames of `SPEC.md`, and decoded frames of the rendered file at every transition.

## Not checked

**Nobody has heard the sound.** It was composed as a `Song` and measured, never listened to. Listen before
publishing: balance of lead against arpeggio, and whether the risers are too bright, are the likely notes.

## Weakest shots

- 0-4 s: the hook is quiet and slow next to the rest; a faster map (0.2 s a line) would tighten it.
- 22-26 s: the kernel line holds over an empty board for two bars.
- The swap (32-33 s) tells its replacement apart only by a hatch pattern.

## Rebuild

`node media/tools/films.mjs build seated` (see `media/README.md`).
