# Neon Rift — run & gun on MicroEngine

Objective: a complete Contra-inspired run & gun built on MicroEngine with external licensed assets, plus an evaluation of the engine.
Branch: `feat/run-and-gun` (from `feat/engine-rebuild`). Delivery: work-unit commits, no push/PR. Runner: MSTest via `dotnet test`; visual checks via `--hidden --captures`.

## Specs

- S1 "Desarrollar un videojuego 2D de acción y plataformas inspirado en Contra (NES), utilizando MicroEngine como motor." — "El resultado debe ser un juego completo, funcional, visualmente coherente y entretenido, no una demostración técnica."
- S2 "Movimiento lateral, saltos y disparos en múltiples direcciones."
- S3 "Enemigos variados con comportamientos diferenciados."
- S4 "Escenarios desplazables con obstáculos y plataformas."
- S5 "Armas o mejoras que aporten variedad al combate."
- S6 "Enfrentamientos contra jefes."
- S7 "Sistema de vidas, daño, derrota y victoria."
- S8 "Animaciones, efectos sonoros, música e interfaz integrados."
- S9 "Una experiencia jugable de principio a fin."
- S10 "La referencia es Contra de NES, pero el juego debe tener identidad propia."
- S11 "El contenido audiovisual debe obtenerse principalmente de assets externos preexistentes." — "Los recursos deben tener licencias compatibles con su utilización y ofrecer una dirección artística coherente."
- S12 "Se permite evolucionar MicroEngine cuando sea necesario, manteniendo su orientación como motor reutilizable y evitando soluciones específicas del juego dentro de su núcleo."
- S13 "Un videojuego completo, ejecutable y jugable, acompañado de una evaluación de las capacidades de MicroEngine utilizadas, las limitaciones encontradas y las modificaciones que hayan sido necesarias."

## Tasks

| ID | Specs | Route | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 Asset selection, licence check, ADR 0004 | S11 | inline | done | this commit |
| T2 Engine: PNG/WAV decode, music streams, image ops, sheet slicing, parallax, content folder | S12 | inline | done | 040792d, 10 engine tests |
| T3 Game skeleton: content loading, hero, aiming, camera, stage 1 | S1,S2,S4,S8 | inline | done | e57d28a |
| T4 Enemies, weapons, pickups, lives, bosses, stages 2-3 | S3,S5,S6,S7 | inline | done | e57d28a |
| T5 Menus, HUD, flow title→ending, audio mix | S7,S8,S9 | inline | done (mix not listened to) | e57d28a, e53321c |
| T6 Tests, scripted captures, polish | S1,S9 | inline | done | e53321c, 25 game tests |
| T7 Engine evaluation, README, docs | S13 | inline | done | 3b54a6d |
| T8 Play-test feedback: balance, health per life, sprite facing | S1,S3,S7 | inline | done | 7daa1a3 |
| T9 Play-test feedback: pace and pressure to keep running | S1 | inline | done | this commit |

Route evidence: tasks depend on each other (engine → skeleton → content), so no parallel writer reason; all inline.

## Log

- L1 (2026-10-30) Original request: "MicroEngine — Nuevo juego: Run & Gun" (full text in session): Contra-inspired run & gun, external licensed assets chosen by Claude, engine may evolve while staying reusable, deliver the game plus an engine evaluation.
- L2 Asset selection (all CC0, verified on each OpenGameArt page and in the bundled licence files): Ansimuz "Warped" family for one coherent art direction — Warped City (hero, drone, turret, rooftop tileset, skyline), Warped City 2 (cop robot, egg turret, explosions, platform blocks), Super Grotto Escape (ruins), Sci-fi Lab (lab layers), Warped Vehicles (bosses), Warped Shooting FX (weapon effects), Ansimuz CC0 music (cyber_runner, up_and_right, jump_run_duck, boss_fight_2); Juhani Junkala CC0 "512 Sound Effects" and "Retro Game Music Pack" (title, ending). Rejected: Warped City bundled music by Pascal Belisle (attribution-only, not needed).
- L3 Decisions: virtual canvas 480×270 (hero sprites are ~50 px tall); ADR 0004 adds file loading to the core in managed code; compressed music streamed by the backend.
- L4 (2026-10-30) Engine unit: tests were written alongside the code rather than observed RED first (no prior behaviour to break); 60 engine tests pass.
- L5 Game built inline (tasks sequential). The autopilot playthrough test caught an impossible 6-tile lava pool (stepping stone misplaced), an unreachable Leviathan turret and a bot unable to climb blocks; fixed. Rules tests caught a real bug: one large score gain crossing two milestones granted one life.
- L6 Visual review from captures: muzzle flash too large, pod letter off-centre, controls columns misaligned with the proportional font, heavy outline on 5x titles, Leviathan underside glow read as blobs, foreground pillars too opaque, carriers too slow to shoot; all fixed and recaptured.
- L7 Verification: solution build clean; 204 tests pass (engine 60, Courtyard 24, Meadow 42, Neon Rift 25, Trail 53); every stage, boss, menu and the ending inspected in captures; music loads via raylib without decode errors. Not verified: audio mix by ear, 60 fps feel on hardware. Evaluation in docs/evaluations/neon-rift.md.
- L8 (2026-10-30) User play-test: "Bueno, hay que balancear un poquito la dificultad. El primer nivel te destruye." — "Hay que ajustar la animación de los enemigos cuando corren porque están corriendo hacia atrás." — "yo creo que lo dejaría a más de un hit la muerte" — "ahí revives y es imposible esquivar". Follow-up: "Revisa todos los sprites de enemigos a ver si hay más que esten girados incorrectamente". The music was approved ("La música está bien, está perfecta").
- L9 Changes: 3 health per life, 1.2 s invulnerability after a hit, 3 s after respawn; riflemen fire 2-round bursts every 2.4 s, egg pods one 3-way volley, turrets every 2 s, troopers released every 1.7-2.5 s; stage 1 rifleman moved off the egg pod. Facing audit: robot, drone, turret and egg sheets face left natively; troopers/riflemen and drones were mirrored wrongly, turret played its head-spin frames when firing; all fixed. Ships and bosses were already correct. 26 game tests pass; bot without god mode survives stage 1 far longer.
- L10 (2026-10-30) User: "sí está más balanceado, pero se siente más lento. El gameplay se siente más lento. Y ya no es tan run ... ya no te obliga ... a correr."
- L11 Changes: run speed 98 to 118 px/s, Pulse cooldown 0.16 to 0.13 s, Scatter 0.30 to 0.26 s, troopers 72 to 92 px/s, drones faster, camera shows more of the screen ahead; after 3.5 s without scrolling, troopers keep arriving from the right (every 2.6 s, down to 1.2 s). The bot test caught the longer jump overshooting a stage 2 stepping stone into lava: that pool is now 5 tiles. 27 game tests pass.
