# Trail Duel visual art-direction pass

Objective: elevate Trail Duel's pixel art (overworld cohesion, characters, walk cycle, creatures, battle animation) without touching rules, collision, audio or engine behaviour.
Branch: `feat/engine-rebuild`. Delivery: work-unit commits, no push/PR. Runner: MSTest via `dotnet test`; visual checks via `--hidden --captures` and `TRAIL_DEBUG` starts.

## Specs

- S1 "Buildings and environmental objects feel naturally integrated into their surroundings rather than pasted onto flat terrain."
- S2 "Overworld scenes have improved depth, shading, variation, and environmental cohesion."
- S3 "Characters have more natural proportions, recognizable hairstyles, and readable directional sprites."
- S4 "Walking animations look like actual walking rather than hopping."
- S5 "Creatures have stronger silhouettes, richer shading, and more distinctive designs."
- S6 "Battle animations and creature presentation feel more expressive."
- S7 "Trail Duel retains its own recognizable monster-tamer identity."
- S8 "Gameplay, audio, performance, and existing engine behavior remain unaffected."
- S9 "Improvements are applied consistently across the game's existing content."
- Constraints: "Do not introduce unnecessary abstractions or specialized systems into the engine." "Keep the overworld bright, colorful, and readable." "Avoid excessive bloom, blur, gradients, particles, or effects that compromise the pixel-art aesthetic."

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 Characters: new proportions, hair, 3-direction walk cycle without hop | S3,S4,S9 | inline | done | see L3 |
| T2 Overworld: building integration, ground decals, tree variety, shadows (all maps) | S1,S2,S9 | inline | done | see L3 |
| T3 Creatures: canvas occlusion/sel-out shading, redesigned species | S5,S7,S9 | inline | done | see L3 |
| T4 Battle: grounding shadows, breathing idle, anticipation, hit reactions | S6,S8 | inline | done | see L3 |
| T5 Validation: build, tests, captures of every area, README screenshots | S8,S9 | inline | done | see L3 |

Route evidence: single art direction pass in one game folder; no Writer trigger (units share palette and visual rules, decided by the parent).

## Log

- L1 (2026-10-30) User request: "Trail Duel — Visual Art Direction & Animation Polish" — comprehensive visual polish pass on sprite quality, character animations, creature design and environmental cohesion; Sprout Town as the environmental reference; representative work first, then apply across all content; validate with captures, builds and tests; Definition of Done items quoted as S1-S9 above. (Full request text is in the session transcript; specs quote its Definition of Done verbatim.)
- L2 Baseline captured (town, gallery, forest, battle). Findings: buildings are flat rectangles with no ground contact; ground is uniform tile noise; character walk raises head+torso 1px on stride frames (reads as a hop); side hair leaves a bare forehead band; creatures are ellipse-heavy with uniform outlines.
- L3 Implemented and checked by captures (town, route, forest, brook, lab, shrine, hall battle, wild battle frame sequence, `TRAIL_DEBUG=cast` and `zoom` sheets): new 16x22 pixel-map characters with planted-foot walk frames and no head bob; buildings with overhanging roofs, eave shadow, foundations, doorsteps, shrubs and tufts; per-map ground layer with tone patches, specks, tall-grass fringe and union shadow mask; tree/pine variants; rounded pond corners; creature canvas with occlusion, sel-out, polygons, flat facets and banded shading; redesigned Emberpup, Blazehound, Tidalotl, Grovehare, Breezel, Pebblit, Gustowl, Voltusk, Bouldox (cute species kept round); battle breathing frames, contact shadows, charge glow, crouch-lunge with hit-stop, flash and knockback, faint lurch. `dotnet test tests/TrailDuel.Tests`: 51/51 passed; solution builds with 0 errors. Engine untouched.
