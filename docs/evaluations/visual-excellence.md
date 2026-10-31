# Evaluation: procedural, hybrid and external content across the four games

The mission was to make each game "the best possible version of itself" while covering three content
approaches: Meadow Run fully procedural, Courtyard Key and Trail Duel hybrid, Neon Rift external. It ran
in two passes: a first pass, then a direction correction after hands-on playtesting by the owner. This
records what changed, why, what the engine already provided, and what it could not do.

**Summary.** The engine carried all three approaches with one small general addition
(`PixelImage.Scale2x`); every hard problem was art direction. The first pass improved production values
but kept Trail Duel's procedural creatures and characters, whose soft, toy-like style was the real
problem, and chose recorded music by track name, which played too calm. The second pass changed
direction instead of polishing: Trail Duel's cast now comes from one hand-drawn CC0 collection and the
procedural world was repainted in that cast's visual language; the synthesized soundtrack the owner
preferred came back once measurements confirmed why the recorded one felt soft. In Courtyard Key the
imported slime, bat and knight were reverted to their procedural originals and the Moss Warden was
rebuilt as a towering boss.

## Asset survey and choices

| Candidate (all CC0) | Verdict | Reason |
| --- | --- | --- |
| isaiah658 "50+ Monsters Pack 2D" and "Pixel Pack #2" (Zoonami) | Rejected | Flat four-colour-plus-black monsters and tiles |
| Ninja Adventure — FX sheets | Kept (Trail Duel) | Hand-animated flame, water, plant, thunder, rock, wind and magic effects map one-to-one onto the seven elements |
| Ninja Adventure — jingles | Kept (Trail Duel) | Four short fanfares; not part of the music complaint |
| Ninja Adventure — music | Adopted, then dropped | Played calm; measured 3-10 dB quieter than the synth tunes with a fraction of their bass energy (see below) |
| Ninja Adventure — monsters | Heart-trap kept; slime, bat, knight reverted | Playtesting found the procedural slime, bat and knight stronger; the heart-trap is a clear improvement over the old spitter |
| Ninja Adventure — villagers | Rejected | Squat chibi proportions next to Courtyard Key's hero |
| Openmon collection (Screen Smith), human-made sets | Adopted (Trail Duel creatures and trainers) | 64x64 battle sprites with strong three-quarter silhouettes and evolution lines, and matching 18x26 trainers, from one art family |
| Openmon collection, AI-generated sets and Tuxemon-derived sprites | Excluded | Provenance: only sets the author marks as made by human artists were used |
| Openmon battle backgrounds | Rejected | Soft painterly style at 480 px wide; Trail Duel's own stages fit better once tuned |
| ArMM1998 "Zelda-like tilesets" | Rejected | Good GBA-era tiles, but no dense forest or large buildings; mixing a third source would recreate an amalgam |

## What changed, per game

| Game | Problem found | Final result |
| --- | --- | --- |
| Trail Duel — creatures | Rounded, chubby, side-on procedural shapes read as toys | 14 hand-drawn 64x64 Openmon sprites with stronger silhouettes; the player's creature is the mirrored front view; six display names follow the new designs (ids, stats, types and evolutions unchanged) |
| Trail Duel — people | Big-headed 16x22 chibis; the player's cap read as a hard hat | 15 Openmon trainers (18x26, three-frame walks); battle portraits doubled with Scale2x and the outline thinned back to one pixel |
| Trail Duel — world | Pastel lime grass, checker-dithered sand paths, rail-like ledges, noisy roofs: a flat amalgam | Procedural tiles repainted in the sprites' language: natural ramps, outlined tufts, flat dirt paths with a rim, two-row encounter grass, earth-faced ledges, ponds with a waterline and shore (inner corners included), ink-outlined props, flat-stepped roofs and walls, four conifer shapes |
| Trail Duel — battles | Flat backdrops, 2x overworld sprite as trainer, particle-only attacks (first pass) | Painted stages for meadow, forest, hall and lab; crafted per-element impacts aimed at each sprite's centre; the grass palette now matches the creatures |
| Trail Duel — music | Recorded tracks felt calm and lullaby-like | Original synthesized soundtrack restored; recorded jingles kept |
| Courtyard Key — Moss Warden | Too small and round (40x40) | 64x60 stone guardian: boulder shoulders, knuckle-dragging arms, sapling crown, rune core, distinct tell, dazed and rage poses; hurtbox and behaviour unchanged |
| Courtyard Key — enemies | Mixed imports | Procedural slime, bat and knight back; imported heart-trap kept; joined stairwell and villager shading from the first pass kept |
| Meadow Run | Ember Castle halls were the one weak spot | Stone variants, framed windows, a moon through them; still 100% procedural |
| Neon Rift | — | Unchanged; tests pass |

### Measuring the music instead of guessing

The mix could not be listened to in this environment, so the recorded tracks were compared with the
synth tunes the owner preferred (rendered to WAV from the previous `MusicBank`): mean loudness and the
share of spectral energy below 200 Hz. The synth tunes sit at -18 to -21 dB with 0.34-0.44 of their
energy in the bass; the chosen recorded tracks sat at -21 to -29 dB with 0.04-0.25 (town 0.06, title
0.04, wild battle -28.9 dB). No track in the pack combined the synth tunes' loudness and bass drive, so
the synth soundtrack returned.

## Engine capabilities reused

| Capability | Use |
| --- | --- |
| `PixelImage` drawing, `Art.Ramp`/`Dither`/`ValueNoise`/`Hash` | Painted stages, panoramas, the repainted overworld, the Warden, stairwell and castle walls |
| `PixelImage.Load`, `Crop`, `FlipX`, `OpaqueBounds`, `ContentFolder` (ADR 0004) | Loading creature, trainer, effect and heart-trap sheets, pivots from opaque bounds, also in headless tests |
| `PixelImage.Recolor`, `Map`, `Silhouette`, `Outline`, `Scale2x` | Palette harmonisation, the fisher's hue turn, hit flashes, battle portraits |
| `SpriteAtlasBuilder.AddGrid`, `SpriteAtlas.Frames` | Effect strips and the effect player |
| `AudioMixer.AddSoundFile`, `Synth`/`Song` | Recorded jingles beside synthesized music and effects |
| `Autotile.Mask4`-style neighbour checks | Paths, water shores and their inner corners, stairwell pieces, window mouldings |
| Scripted captures (`--script --captures --hidden`), debug starts | Every before/after comparison; `TRAIL_DEBUG=fx` replays one move of each element |

## Engine change

`PixelImage.Scale2x()` (EPX), covered by `ContentLoadingTests.Scale2x_doubles_size_and_rounds_diagonal_steps`
and recorded in ADR 0005. No other engine change was needed in either pass.

## Limitations found

| Limitation | Game-side today | Possible engine change |
| --- | --- | --- |
| No one-shot sprite effect player | Trail Duel has a 55-line `SpriteFx`; Neon Rift has its own `Effect` actor | A small shared list once a third game needs it |
| Frame sizes of sheets are not discoverable | Measured from transparent gaps and checked visually | None needed |
| `Autotile.Mask4` has no diagonal bits | Trail Duel checks diagonals itself for pond inner corners | An 8-neighbour mask helper if another game needs concave corners |
| Visual fit and musical tone are judgements | Composite captures, playtest feedback, and loudness/bass measurements | Not engine concerns |

## How the games show the engine's range

- **Meadow Run**: nothing but code; every sprite, tile, backdrop, sound and song is generated at load.
- **Courtyard Key**: a procedural world, cast and boss with one crafted enemy repainted to belong.
- **Trail Duel**: crafted creatures and trainers carry the identity; the world, stages and music are
  painted in code to the same language.
- **Neon Rift**: almost entirely files, with code deriving poses the artist did not draw.

The same atlas, audio mixer, scene and test tooling serve all four without a "content mode".

## Verification status

See the final run recorded in `odd/tasks/visual-excellence.md` (build, test counts, captures). Not
verified: listening to any audio, and frame rate on real hardware.
