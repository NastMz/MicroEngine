# Build the first real-art platformer increment

Baseline2415072; branch feat/kenney-platformer. Local commits only: latest owner prohibits publication/main integration. Serial .NET Release and Windows native proof; stacked coherent work units, forecast1200 authored lines (heuristic, not a test limit). Memory mirror pending unavailable runtime identity.

## Specs
S1: "Vamos a continuar con **plataformas**, manteniendo nuestro objetivo original de validar MicroEngine mediante juegos progresivamente más exigentes: primero Mario, después Zelda y posteriormente Pokémon."
S2: "El objetivo no es reproducir Mario fielmente, sino disponer de un juego que incluya movimiento, salto, plataformas, obstáculos, interacciones básicas, progresión por un nivel y una condición de finalización o reinicio."
S3: "Evalúa los contratos gráficos actuales y asegúrate de que las decisiones del siguiente horizonte no introduzcan restricciones artificiales sobre resoluciones, dimensiones de sprites, filtrado, escalado o estilo artístico." No speculative advanced renderer.
S4: "Quiero que el mini plataformas utilice sprites y tiles reales en lugar de limitarse a figuras geométricas." Verify Kenney New Platformer Pack availability/license/compatibility and record asset provenance. Only general public engine APIs, no Kenney-specific engine rules.
S5: "No quiero que el ejemplo termine implementando por su cuenta sistemas generales que deberían pertenecer a MicroEngine, como la gestión de recursos, el renderizado, las animaciones, las colisiones o el manejo de escenas." Classify general capability, reusable extension, composition or game rule; significant ADR reviewed before approval.
S6: "No publiques al remoto ni integres cambios en main sin autorización." Preserve unrelated work, local traceable commits, specs/backlog/roadmap/evidence synchronized, independent review and automated/native checks; stop at genuine subjective playable increment, not routine technical approvals.

## Tasks
T1 S1-S6 inline completed9ce0452 asset verification, bounded architecture specification/ADR and independent design review.
T2 S3-S5 inline completed341a423/7843a54 reusable capabilities and RED/GREEN correction.
T3 S1-S6 inline completed6b750f7/431d441 real-art playable consumer, native proof, independent verification and local delivery.

## Log
L1 Owner original request verbatim:
Vamos a continuar con **plataformas**, manteniendo nuestro objetivo original de validar MicroEngine mediante juegos progresivamente más exigentes: primero Mario, después Zelda y posteriormente Pokémon.

## Objetivo del siguiente horizonte

Desarrollar un pequeño juego de plataformas inspirado en Mario, funcional y jugable, que sirva para validar las capacidades generales de MicroEngine.

El objetivo no es reproducir Mario fielmente, sino disponer de un juego que incluya movimiento, salto, plataformas, obstáculos, interacciones básicas, progresión por un nivel y una condición de finalización o reinicio.

Utiliza este escenario para identificar qué capacidades generales necesita el motor y cuáles son reglas particulares del videojuego.

## Consideración arquitectónica: independencia del estilo gráfico

Quiero incorporar un requisito transversal importante.

MicroEngine debe poder soportar distintos estilos de gráficos 2D, incluyendo:

- Pixel art.
- Sprites e ilustraciones de alta resolución, como los utilizados en juegos modernos.
- Spritesheets y animaciones.
- Primitivas geométricas generadas mediante código.
- Composición de capas, fondos y, posteriormente, efectos como parallax.

No significa que debas implementar todas esas capacidades inmediatamente.

Evalúa los contratos gráficos actuales y asegúrate de que las decisiones del siguiente horizonte no introduzcan restricciones artificiales sobre resoluciones, dimensiones de sprites, filtrado, escalado o estilo artístico.

Si identificas una limitación real, documenta y resuelve lo necesario de manera incremental, respetando la arquitectura general del motor.

No desarrolles un renderizador avanzado de forma especulativa.

## Assets para el primer juego

Quiero que el mini plataformas utilice sprites y tiles reales en lugar de limitarse a figuras geométricas.

Utiliza como primera opción el paquete gratuito New Platformer Pack de Kenney:

https://kenney.nl/assets/new-platformer-pack

Verifica su disponibilidad, licencia y compatibilidad.

Puedes seleccionar otro paquete de recursos gratuitos, preferiblemente CC0, si encuentras uno técnicamente más adecuado.

Registra la procedencia y licencia de los recursos incorporados. No utilices assets protegidos de Mario o de otros videojuegos comerciales.

Los assets deben consumirse mediante las APIs generales de MicroEngine. No introduzcas código del motor especializado en un paquete gráfico determinado.

## Validación arquitectónica

El juego debe ser un consumidor independiente del motor, utilizando únicamente sus APIs públicas.

No quiero que el ejemplo termine implementando por su cuenta sistemas generales que deberían pertenecer a MicroEngine, como la gestión de recursos, el renderizado, las animaciones, las colisiones o el manejo de escenas.

Sin embargo, tampoco quiero trasladar al núcleo reglas específicas de plataformas o de Mario.

Si aparece una necesidad nueva, determina si corresponde a:

- Una capacidad general de MicroEngine.
- Una extensión reutilizable.
- Una composición de capacidades existentes.
- Una regla particular del videojuego.

Justifica las decisiones arquitectónicas significativas.

## Desarrollo autónomo

Tienes autoridad para investigar alternativas, elaborar especificaciones, aprobar ADRs técnicos mediante nuestro proceso de revisión, preparar tareas, implementar, probar, revisar y continuar con el siguiente trabajo.

Mantén el backlog, roadmap, contratos y documentación actualizados.

Ejecuta pruebas automatizadas, validaciones nativas y revisiones independientes según corresponda.

Realiza commits trazables y preserva el trabajo ajeno.

No detengas el desarrollo para solicitar aprobaciones técnicas ordinarias ni para que yo repita pruebas automatizables.

No publiques al remoto ni integres cambios en main sin autorización.

## Cuándo necesito intervenir

Solicita mi participación cuando exista un incremento jugable que realmente se beneficie de mi evaluación subjetiva: respuesta de controles, sensación del movimiento, claridad visual, animaciones o experiencia general.

En esos casos, entrégame instrucciones concretas para ejecutar el juego y dime qué aspectos debo evaluar.

Una vez reciba mi feedback, podrás continuar con las correcciones y los siguientes incrementos autónomamente.

Detente también ante decisiones que cambien sustancialmente la visión del producto o bloqueos que no puedas resolver dentro de la autoridad delegada.

## Criterio principal de éxito

No basta con que el juego funcione.

Quiero que MicroEngine demuestre que puede proporcionar infraestructura general reutilizable, mientras que el juego implementa únicamente sus reglas y contenido particulares.

No persigas completar toda la arquitectura 2D antes de tener un juego funcional. Construye las capacidades necesarias progresivamente, sin comprometer la extensibilidad futura.

**Continúa autónomamente con el siguiente horizonte del roadmap hasta alcanzar un incremento jugable que requiera evaluación humana.**

L2 Design reviewer required fixed course/order and retry-safe cache cleanup; one correction/targetedPASS resolves both. Root approves ADR0012/KPF1 before source. Source-free docs/assets verification only; no meaningful RED for this unit. Official Kenney URL/CC0+embeddedlicense verified; archiveSHA553b907f3f0e505ab65f56f245ccaff3123c8fe3f3a0dfce9373b996bfc18cc2. Existing226tests remain baseline; RDDglobaloff unchanged.

L3 TwoD9runnableRED->9GREEN, Desktop2RED->38fullGREEN, game3RED->3GREEN (later missingcoin/buffer regression checks added without fabricatedRED). Initial testproject accidentally used Xunit annotations with MSTest; compiler error corrected before runnableRED, no dependency change. One extreme-displacement test corrected from a finitely clamped result to genuinely unrepresentable unblocked coordinates; contract unchanged. Public course completes517steps and identical reset. Native PNG/atlas/camera/course proofPASS; screenshot convenience TakeScreenshot prepended cwd to an absolute path, so switched to checked ExportImage of existing readback, thenPASS. Original asset bytes preserved with narrow asset.gitattributes to prevent XML/license line-ending conversion. Full feature independent review pending; main/origin untouched, no remote operations.

L4 Own game6/6,TwoD9/9,Desktop38/38; newnative PNG/camera/course517steps/reset/identicalreplay+CLI7casesPASS/build0warnings. Nine original source asset hashesPASS and native screenshot inspected, no generated art or commercialMario assets. Full independent baseline+horizon verification requested at frozen game commit; no physical key or subjective/fullH1 claim.

L5 Independent wholefeature verification initialpass243cases (Hosting31+Loop159+Desktop38+TwoD9+game6),14nativeharnesses/allbuildsclean/nineassethashesPASS. One confirmed contract blocker: StaticWorld constructor accepted mutually overlapping solids despite specifiednonoverlap. New public regression observedRED1failed; constructor pair-validation added without changing spec or coverage, touching staysvalid. Targeted recheck only requested; no new full sweep. NFR006 styleindependence and latest local-onlyrestriction documented in manifesto/requirements/AGENTS/autonomy; no frozen game/backend changes.

L6 Parent post-correction native+productionCLI spotcheckPASS. Final Git-blob provenance check found initial License/XML canonical line-ending normalization differed from original working bytes despite all nine working-file hashes matching. Narrow asset attributes were already -text; explicit renormalize of only these two staged originals restores exact original archive bytes. All nine stagedblob hashes nowPASS; no C#/PNG/gameplay changes. This packaging-normalization correction has no functional delta and is recorded rather than silently changing provenance hashes. Main/origin remain2415072, unrelated.gitignore/.codegraph preserved.

KPF-1 technical closure2026-10-18: independent kenney_platformer_verify PASS S1-S6, after one source correction7843a54 for overlapping-static-solid rejection (new regression RED1->GREEN10TwoD, touching retained). Initial full Release suites Hosting31/Loop159/Desktop38/TwoD9/Platformer6 =243PASS, then targeted correctedTwoD10 and independent public edge/course probesPASS =>244effective verified cases. All14nativeharnessesPASS, including13baseline and new KPF realPNG/902atlas/default256vs2048budget/pixel/alpha/camera/517stepcourse/coins/death/gating/reset/replay plus production7CLIcases0/1/2. Fresh builds0warnings/errors. Parent corrected native+7CLI spotcheckPASS. No full-suite rerun after bounded correction claimed; unchanged suites retain initial proof, correctedmodule/route targeted. Nineoriginal workingfilehashesPASS and finalcommittedassetblobhashesPASS after431d441 restored onlyLicense canonicalnewline bytes; XML already exact. Source snapshot corrected233files paritytrue/SHA9aa9db9b0729ff1ff038d5a9b50d5218de5c3749499e84f07b5295d879192fe5 in independent system-temp scratchC:/tmp/MicroEngine-KPF-recheck-892db0258d134b029bef83b45b7a4a0b. No .git/.codegraph/build reuse. Old examples/Hosting/Loop/config/dependencies unchanged. Immutable upstreamLicense whitespace preserved by provenance; diffcheck exception limited to that file, authored code/docs clean. No physical-key or subjective/fullH1 acceptance inferred. Root accepts objective KPF-1; playable control/jump/visual/camera/progression assessment remains owner-owned. Currentmain/origin2415072 unchanged; latest instruction localcommitsonly, no push/main. Pre-existing.gitignore/.codegraph preserved. No independent ready task at this subjective gate; next gameplay refinement derives from owner experience. Memory mirror pending unavailable authoritative runtime identity.
