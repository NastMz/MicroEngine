# MicroEngine achievements

Objective: a reusable engine achievement system (registry, progress, unlock, persistence, toasts, list screen) used by all four games.
Branch: `feat/achievements` (from `fix/prize-money`). Delivery: one commit as requested, no push/PR. Runner: MSTest via `dotnet test MicroEngine.slnx -c Release`.

## Specs

- S1 Engine responsibility — "MicroEngine administra el registro, progreso, desbloqueo, persistencia y consulta de los logros. Cada juego decide qué acciones justifican otorgarlos."
- S2 No gameplay coupling — "el motor no debe conocer conceptos como Pokémon, enemigos, monedas, niveles o misiones."
- S3 Simple API — "desbloquear o actualizar un logro debe requerir una operación clara desde el juego"; both immediate and progressive achievements; "Cada logro podrá disponer de un identificador estable, nombre, descripción, estado y, opcionalmente, icono y progreso cuantificable."
- S4 No duplicates — "El motor debe garantizar que un logro desbloqueado no vuelva a desbloquearse ni produzca notificaciones duplicadas."
- S5 Persistence — "El progreso debe guardarse y recuperarse correctamente entre ejecuciones. Debe tolerar catálogos que evolucionen: agregar un logro nuevo no debe invalidar los anteriores. Los datos corruptos o ausentes deben tratarse de forma controlada." Respect the existing architecture and future WebAssembly without speculative abstractions.
- S6 Notifications — "Al conseguir un logro deberá mostrarse una notificación temporal con su nombre y descripción. Debe integrarse con el renderizado existente y evitar interferir con el control del juego."
- S7 Menu — "Cada demo debe ofrecer una pantalla donde consultar sus logros, incluyendo los desbloqueados, pendientes y el progreso cuando corresponda." Following each demo's navigation conventions, with existing UI capabilities.
- S8 Isolation — "cada demo debe disponer de su propio catálogo y progreso persistido. No deben compartirse desbloqueos accidentalmente entre juegos."
- S9 Integration — "Incorpora un conjunto pequeño, pero representativo, de logros propios en cada una de las cuatro demos existentes." Tied to real gameplay events; no rule or balance changes.
- S10 Tests — "pruebas automatizadas para registro, progreso, desbloqueo, persistencia, aislamiento y casos límite."
- S11 Docs — "Actualiza la documentación pública del motor con ejemplos de uso."
- S12 Constraints — no new dependencies, no generic event bus, no cloud/accounts/rankings, no duplicated system logic per demo, no unrelated refactors, keep existing games compatible; reusable by a fifth game without engine changes. "Realiza un commit con los cambios implementados."

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 Engine: `Achievement`, `AchievementTracker`, `Game.UseAchievements`, toasts, `AchievementsScene` + engine tests | S1-S8,S10 | inline | done | 17 engine tests |
| T2 Meadow Run achievements + title entry + tests | S9,S7 | inline | done | playthrough + 2 new tests, captures |
| T3 Courtyard Key achievements + title entry + tests | S9,S7 | inline | done | dungeon run asserts all 5, title test |
| T4 Trail Duel achievements + title entry + tests | S9,S7 | inline | done | catch/evolve/title tests, playthrough asserts |
| T5 Neon Rift achievements + save store + title entry + tests | S9,S7,S8 | inline | done | autopilot stage 1 and 3 through PlayScene |
| T6 Docs (README, architecture, making-a-game, ADR 0006), captures, commit | S11,S12 | inline | done | this commit |

Route evidence: each game integration is a few dozen lines that depends on T1's API; no Writer trigger reason (no parallel units of several minutes), so all work is inline.

## Log

- L1 2026-10-30 user request (verbatim pasted brief "MicroEngine — Sistema de logros (Achievements)"; specs above quote it).
- L2 Design: tracker lives in `MicroEngine.Core` beside `SaveStore` and persists through it (slot `achievements` in the game's own save folder, which already isolates games by id); views (`AchievementToasts`, `AchievementsScene`) live in `MicroEngine.UI` and use `UiTheme`/`Panel`/`BitmapFont`. Games report events with direct calls (`Unlock`, `AddProgress`, `SetProgress`), no event bus.
- L3 Verification: `dotnet test MicroEngine.slnx -c Release` → 251 passed (engine 78, Neon Rift 30, Courtyard Key 27, Meadow Run 44, Trail Duel 72). RED was not observed first: the tracker was written before its tests.
- L4 Captures inspected (scratch folders, isolated saves): list screens in all four games, banners in Meadow Run, Neon Rift and Trail Duel, title menus. Fixes from review: banners moved below the HUD in Meadow Run, Courtyard Key and Neon Rift (`AchievementToasts.Top`); Courtyard Key title panel narrowed to clear the footer. A first capture run without rebuilding wrote `%LocalAppData%/MicroEngine/NeonRift/achievements.json`; removed, and `RIFT_SAVE_DIR`/`TRAIL_SAVE_DIR` added so capture runs never touch real saves.
- L5 2026-10-30 closing review of dd608f2 (owner request: persistence on abrupt exit, corrupt files, write errors, compatibility, coupling). Defects fixed: a corrupt file was overwritten by the next save (now set aside as `<slot>.corrupt.json` by `SaveStore.Load`), and an existing but unreadable file was overwritten with a partial state (the tracker now refuses to write that session). Retry after a failed write was already correct; now covered by a test. 253 tests pass.
