# MicroEngine autonomous rebuild

Objective: rebuild MicroEngine and its three reference games into a solid, polished product.
Branch: `feat/engine-rebuild` (reference point: tag `reference/pre-rebuild`). Delivery: work-unit commits, no push/PR by inference. Runner: MSTest via `dotnet test`.

## Specs

- S1 "Un motor 2D coherente, extensible y mantenible." — "MicroEngine debe seguir siendo un motor reutilizable y no convertirse simplemente en tres juegos con implementaciones particulares imposibles de compartir."
- S2 "Tres experiencias jugables diferenciadas." (plataformas inspirado en Mario, aventura inspirado en Zelda, RPG inspirado en Pokémon)
- S3 "Una identidad visual cuidada para cada juego." — "Cada juego debe sentirse como una experiencia diseñada deliberadamente, no como una demostración técnica de primitivas gráficas."
- S4 "Controles, interfaces, sonido y retroalimentación adecuados a las mecánicas implementadas."
- S5 "Pruebas y mecanismos de validación proporcionales al sistema."
- S6 "Documentación suficiente para desarrollar nuevos juegos sobre el motor."
- S7 "Ejecuta los juegos, inspecciona visualmente los resultados mediante capturas o herramientas equivalentes disponibles, examina su comportamiento y corrige las deficiencias que encuentres." — "No afirmes que has comprobado algo que no hayas podido ejecutar o inspeccionar realmente."
- S8 "Trabaja en una rama independiente, conserva intacto el punto de referencia original y deja trazabilidad de los cambios importantes."

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 Engine core + raylib backend + headless/script capture | S1,S5,S7 | inline | done | 1de4c1c |
| T2 Meadow Run (platformer) | S2,S3,S4,S7 | delegated (parallel unit) | done | 9c6f14d, 42 tests |
| T3 Courtyard Key (action adventure) | S2,S3,S4,S7 | delegated (parallel unit) | done | 9e231da, 24 tests |
| T4 Trail Duel (creature RPG) | S2,S3,S4,S7 | delegated (parallel unit) | done | 08bd6a8, 51 tests |
| T5 Engine tests | S5 | inline | done | 20d7e9f, 45 tests |
| T6 Docs: architecture, game guide, ADR, AGENTS | S6,S8 | inline | done | 78580ea |
| T7 Cross-game review, engine promotion of shared code, final captures | S1,S3,S7 | inline | done | 5bc50ed |
| T8 Shared LightMap; games migrated to engine helpers | S1,S5,S7 | inline | done | fa9a3da, 46f0521, this commit |

Route evidence: T2-T4 are independent units with disjoint surfaces (`games/<Name>`, `tests/<Name>.Tests`), each several thousand lines, launched together (Writer trigger: parallelism).

## Log

- L1 (2026-10-29) Original request: "MicroEngine — Reconstrucción autónoma integral" (full text in session); autonomy over architecture, art, UX; work on an independent branch, keep the original reference intact.
- L2 Baseline captured: three single-screen games on a thin tracer engine. Decision: greenfield engine with procedural, license-free art and synthesized audio; previous tree retired from the branch, preserved at `reference/pre-rebuild`.
- L3 (2026-10-30) Three games delivered by parallel writers and reviewed from captures. Parent fixes: Trail Duel battle caption during attacks, title Quit/footer/layout; Courtyard Key title footer and boss bar plate. Promoted engine helpers requested by the games: `DrawPivoted`, `FillEllipse`, `AudioMixer.TryPlay`, `PixelImage.FlipY/Rotate90/Silhouette`, copyright glyph.
- L4 Verification: `dotnet build MicroEngine.slnx -c Release` clean; 162 tests pass (engine 45, Meadow 42, Courtyard 24, Trail 51); hidden-window smoke runs of all games and the sample pass. Not verified by a human: audio mix (all automated runs muted) and live 60 fps feel.
- L5 Open follow-ups: migrate game-local helpers to the new engine APIs; a generic lighting module (Courtyard/Meadow both darken scenes); EntityList deferral outside Update.
- L6 (2026-10-30) User: "Next improvements" (migrate games to the new engine helpers and create the shared lighting module). Added `MicroEngine.Graphics.LightMap` (banded multiply darkness + additive halos, custom glow image) from Courtyard Key's lighting; Courtyard Key and Meadow Run now use it. Games use engine `DrawPivoted`, `Rotate90`, `Silhouette`, `TryPlay`, `FillEllipse`; local duplicates removed. Trail Duel battle caption now stays pinned through the attack and hit (auto-advance had been closing it); `TextPanel.Say` always waits for confirmation.
- L7 Verification: solution build clean; 167 tests pass (engine 50, Courtyard 24, Meadow 42, Trail 51); captures inspected for Courtyard dungeon/woods, Meadow caves/castle, Trail title and battle; smoke runs of all games pass.
