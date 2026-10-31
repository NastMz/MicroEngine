# 0005 — Hybrid games: generated and file content side by side

Status: Accepted (2026-10-30). Amends [0004](0004-external-asset-files.md), which kept the three original
games fully code-first.

## Context

The owner asked for each game to become "the best possible version of itself", with an intended
portfolio of one fully procedural game (Meadow Run), two hybrids (Courtyard Key, Trail Duel) and one
external-asset game (Neon Rift). A survey of the original games found that their weakest elements were
not their procedural worlds but a few specific pieces: Courtyard Key's faceless enemies, Trail Duel's
flat battle stages and synthesized music. Hand-made CC0 packs were better at some of these; procedural
art was better at others.

## Decision

- A game may combine generated and file content. It chooses per element, by the result in play, and
  revisits the choice when playtesting disagrees. After two passes: Courtyard Key loads one enemy (the
  heart-trap) from a file and keeps its world, cast, other enemies and boss procedural; Trail Duel
  loads its creatures, trainers, battle effects and jingles and keeps its world, battle stages, music
  and sound effects procedural, repainted to match the imported sprites. Meadow Run stays fully
  procedural; Neon Rift is unchanged.
- **One visual language per game.** When imported art carries the identity (Trail Duel's creatures and
  trainers), the procedural parts adopt its language: palette, outline weight, shading steps. When the
  procedural world carries it (Courtyard Key), imported sprites are repainted to the world's palette.
- **The game's art direction wins over the pack's.** Imported sprites are harmonised in game code at
  load time when they would otherwise clash: Courtyard Key repaints its monster sheet with its own
  colour ramps and ink outline (`Visuals/MonsterArt.cs`) using the existing `PixelImage.Recolor`. The
  shipped files stay unedited copies, so provenance remains checkable against the pack.
- File content follows ADR 0004 unchanged: files under `games/<Name>/Content`, copied to the output and
  read through `ContentFolder`; a `CREDITS.md` lists the pack, author, URL, licence, and the original
  path of every file. Only CC0 packs were used, and from collections that mix human-made and
  AI-generated sets, only the sets marked as human-made.
- Hybrid needs no engine mode or registry. The atlas builder and the audio mixer already accept
  generated and loaded images and clips through the same calls.
- One general image transform was added to the engine: `PixelImage.Scale2x()` (the EPX rule). Trail
  Duel uses it to draw its trainers at battle size from their overworld frames without the blockiness
  of a plain 2x scale. It is genre-neutral and covered by an engine test.

## Consequences

- The working tree grows by about 1.4 MB (Trail Duel's jingles 1.2 MB; creature, trainer and effect
  sheets and Courtyard Key's heart-trap under 0.2 MB). A recorded soundtrack (16.4 MB) was committed
  and later removed on this branch, so it remains in the branch history. Only files the games use are
  in the tree.
- Two candidate packs were rejected because they would have lowered quality or cohesion (see
  `docs/evaluations/visual-excellence.md`). The "does it fit?" check is a design review, not a rule
  the engine can enforce.
- Tests of the hybrid games load the real files headlessly (managed PNG/WAV decoding), so a missing
  or renamed asset fails the build's test run.
