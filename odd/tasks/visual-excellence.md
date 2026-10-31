# Mission: Visual Excellence

Objective: raise the audiovisual quality of Meadow Run (procedural), Courtyard Key (hybrid) and Trail Duel (hybrid, major renewal) without changing gameplay; Neon Rift stays the external-asset reference.
Branch: `feat/visual-excellence` (from `feat/run-and-gun`). Delivery: work-unit commits, no push/PR. Runner: MSTest via `dotnet test MicroEngine.slnx -c Release`; visual checks via `--hidden --mute --captures` with `COURTYARD_START`, `TRAIL_DEBUG`, `MEADOW_START`.

## Specs

- S1 "Meadow Run remains a beautiful, fully procedural game with its existing identity preserved." ("do not redesign it or sacrifice its character merely to introduce new technology")
- S2 "Courtyard Key looks more polished and cohesive, combining procedural and external resources where they genuinely complement each other." Weak elements named by the owner: "certain NPCs, enemies, stairs, and other details". "We do not want beautiful backgrounds with characters that look imported from an unrelated game."
- S3 "Trail Duel achieves a substantial improvement in artistic quality and visual consistency." "Characters, creatures, environments, battles, animations, interfaces, visual effects, and overall atmosphere should feel like parts of one cohesive, polished RPG." "Preserve the established gameplay, progression, and identity of Trail Duel."
- S4 "All three games remain complete, functional, and enjoyable."
- S5 "Existing game mechanics and established behavior are preserved." ("Visual changes must not unintentionally alter collisions, controls, difficulty, progression, or existing game behavior.")
- S6 "MicroEngine continues to support procedural, external, and hybrid content without unnecessary coupling." ("Do not introduce speculative infrastructure simply because it might be useful someday.")
- S7 "The other games, including Neon Rift, remain unaffected by regressions."
- S8 "Any engine improvements and significant architectural decisions are documented."
- S9 "Use properly licensed resources. Verify the licenses and provenance of external assets, document their sources, and integrate only resources compatible with the project. Prefer CC0 resources where suitable."
- S10 "Provide a final assessment of what improved, what capabilities were reused, what limitations were discovered, and how the resulting games demonstrate MicroEngine's versatility." "do not consider the mission complete merely because assets have been replaced or tests pass."

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 Trail Duel battle stages: painted backdrops per area, natural platforms, trainer back sprites | S3,S5 | inline | done | a62e1dd; captures L4 |
| T2 Trail Duel move/status/capture effects from Ninja Adventure FX, typed per element | S3,S5,S9 | inline | done | a62e1dd; `TRAIL_DEBUG=fx` captures |
| T3 Trail Duel soundtrack and jingles from Ninja Adventure | S3,S9 | inline | done | cf5b470; unmuted raylib decode run |
| T4 Trail Duel interface/overworld consistency pass (title, ending, forest motes) | S3 | inline | done | 3989b76 |
| T5 Courtyard Key: Ninja Adventure monsters for regular enemies, redrawn stairs, refined weak villagers | S2,S5,S9 | inline | done | d664780 |
| T6 Meadow Run: review every world, refine only clear weak spots | S1 | inline | done | df00bc8 (castle only) |
| T7 Credits, ADR 0005, READMEs, screenshots, evaluation; full test run | S7,S8,S9,S10 | inline | done | docs commit; 213/213 tests |

Route evidence: one parent owns art direction across games; units are sequential and share the asset pack decisions, so no Writer trigger.

## Log

- L1 (2026-10-30) User request: "MicroEngine — Mission: Visual Excellence" (full text in the session transcript; S1-S10 quote its success criteria and principles verbatim). Portfolio: Meadow Run fully procedural, Courtyard Key hybrid, Trail Duel hybrid with substantial renewal, Neon Rift external reference, unchanged.
- L2 Asset survey (all checked 2026-10-30):
  - isaiah658 "50+ Monsters Pack 2D" and "Pixel Pack #2" (OpenGameArt, CC0): rejected. Flat four-colour-plus-black monsters and tiles are below Trail Duel's current shaded creatures and would clash with its overworld.
  - "Ninja Adventure Asset Pack" by Pixel-boy and AAA (pixel-boy.itch.io/ninja-adventure-asset-pack, CC0 1.0, LICENSE.txt in the zip): adopted selectively. Its FX sheets (flame, water, plant, thunder, rock, slashes, magic) match Trail Duel's seven types; its 16x16 monsters read far better than Courtyard Key's faceless slime and dark bat and share the world's outline/shading language. Its villagers are squat chibis that are not better than Courtyard Key's own and would mismatch the hero, so people stay procedural.
- L3 Baseline captures (scratchpad) show: Trail Duel battle backdrops are the weakest screens (flat tree row, generic checker hall, overworld sprite reused as trainer back); Courtyard Key ruin stairs are two flat dark rectangles; Meadow Run reads well.
- L4 Trail Duel: painted stages (meadow, forest, hall, lab), mound platforms, Scale2x trainer portraits (engine `PixelImage.Scale2x`, RED then GREEN in `ContentLoadingTests`), 16 Ninja Adventure FX sheets with per-element impacts, composed soundtrack (13 OGG, 16.4 MB) and 4 jingles replacing synth music; title and ending use the meadow panorama. Indoor (lab) battles now use a lab stage instead of the meadow: visual only. Creatures kept procedural (gallery review: already coherent).
- L5 Courtyard Key: slime/bat/heart-trap/gold knight from Ninja Adventure, repainted with game ramps and ink (test asserts no pack outline colour survives); first knight choice (iron) unreadable under dungeon lighting, replaced by KnightGold. Joined stairwell tiles; villager volume pass. Hitboxes unchanged; 26/26 tests including playthroughs.
- L6 Meadow Run: worlds 1-3 judged strong and left alone; Ember Castle back wall got stone variants, window jambs/sills and a moon/stars sky. 42/42 tests.
- L7 Full run: build 0 warnings; 213 tests pass (61 engine, 42 MR, 26 CK, 57 TD, 27 NR). Neon Rift capture unchanged. Not verified: listening to the mix, frame rate on real hardware.
- L8 (2026-10-30) User request: "MicroEngine — Visual Direction Adjustment" (full text in the session transcript). Verbatim success criteria: Trail Duel "creatures feel more appealing and less chubby/toy-like", "trainers and NPCs feel less childish and better integrated", "the overworld feels more coherent and less flat/odd", "the soundtrack better supports adventure"; Courtyard Key "Moss Warden has stronger presence", "enemy choices are refined with better judgment"; "Meadow Run remains strong", "Neon Rift remains unaffected", "gameplay remains intact".
- L9 Adjustment pass, commits 597535e..303fe31: CK Warden rebuilt (64x60, poses), procedural slime/bat/knight restored, heart-trap kept. TD music: synth soundtrack restored after loudness/bass measurement of all 40 recorded tracks against rendered synth tunes; jingles kept. TD creatures and trainers recast from Screen Smith's Openmon collection (human-made CC0 sets only; AI-generated and Tuxemon-derived sets excluded); six display renames, ids/stats/types/evolutions unchanged. TD overworld repainted (palette, grass, paths, tall grass, ledges, water + inner corners, outlined props, flat roofs/walls, four conifers). Final: build 0 warnings; 213 tests pass (61 engine, 42 MR, 26 CK, 57 TD, 27 NR). Not verified: listening, fps on hardware.
